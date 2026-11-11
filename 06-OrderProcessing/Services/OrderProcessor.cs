using _06_OrderProcessing.Models;
using _06_OrderProcessing.Repositories.AsyncRepositories;
using _06_OrderProcessing.Shared;
using System.Diagnostics;

namespace _06_OrderProcessing.Services;

// Runs the full checkout for one order: customer, stock, pricing, payment, save, email.
// If a step fails, the order is stopped, compensated if needed, and still saved
// with its final status, so orders.json always ends with all 20 orders.
public class OrderProcessor
{
    private readonly CustomerService _customers;
    private readonly InventoryService _inventory;
    private readonly PricingService _pricing;
    private readonly IPaymentGateway _payment;
    private readonly OrderRepository _orders;
    private readonly IEmailService _email;
    private readonly ProductRepository _products = new ProductRepository();
    private readonly JsonFileStore _store = new JsonFileStore();

    public OrderProcessor(CustomerService customers, InventoryService inventory, PricingService pricing,
                          IPaymentGateway payment, OrderRepository orders, IEmailService email)
    {
        _customers = customers;
        _inventory = inventory;
        _pricing = pricing;
        _payment = payment;
        _orders = orders;
        _email = email;
    }

    // Main method: calls the steps in order and stops at the first failure.
    // Returns true only if the order ends with Status = Completed.
    public async Task<bool> ProcessOrderAsync(Order order, CancellationToken ct)
    {
        // Tracks whether stock is currently reserved, so a cancellation knows what to undo
        bool reserved = false;

        try
        {
            var customer = await GetCustomerAsync(order, ct);                 // step 1
            if (customer == null) return false;

            if (!await ReserveStockAsync(order, ct)) return false;            // step 2
            reserved = true;

            if (!await CalculateTotalAsync(order, ct)) return false;          // step 3
            if (!await ProcessPaymentAsync(order, ct)) return false;          // step 4
            if (!await CompleteAndSaveAsync(order, ct)) return false;         // step 5

            await SendEmailAsync(order, customer, ct);                        // step 6

            return order.Status == OrderStatus.Completed;                     // step 7
        }
        catch (OperationCanceledException)
        {
            // The whole run was cancelled (Ctrl+C or --cancel-after)
            await CancelAsync(order, reserved);
            return false;
        }
    }

    // Step 1: load the customer.
    private async Task<Customer?> GetCustomerAsync(Order order, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var customer = await _customers.GetCustomerAsync(order.CustomerId, ct);

            if (!customer.IsActive)
            {
                Log.Error($"Order #{order.Id}", $"Step 1/6 Customer ... INACTIVE in {sw.ElapsedMilliseconds} ms");
                await FinishEarlyAsync(order, OrderStatus.Rejected, "Customer is inactive");
                return null;
            }

            Log.Success($"Order #{order.Id}", $"Step 1/6 Customer ... OK in {sw.ElapsedMilliseconds} ms");
            return customer;
        }
        catch (CustomerNotFoundException ex)
        {
            Log.Error($"Order #{order.Id}", $"Step 1/6 Customer ... NOT FOUND in {sw.ElapsedMilliseconds} ms");
            await FinishEarlyAsync(order, OrderStatus.Rejected, ex.Message);
            return null;
        }
    }

    // Step 2: reserve stock for all items (all or nothing).
    private async Task<bool> ReserveStockAsync(Order order, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        bool ok = await _inventory.ReserveStockAsync(order, ct);

        if (ok)
        {
            Log.Success($"Order #{order.Id}", $"Step 2/6 Reserve stock ... OK in {sw.ElapsedMilliseconds} ms");
            return true;
        }

        string name = await FindShortProductNameAsync(order, ct);
        Log.Error($"Order #{order.Id}", $"Step 2/6 Reserve stock ... INSUFFICIENT in {sw.ElapsedMilliseconds} ms");
        await FinishEarlyAsync(order, OrderStatus.Rejected, $"Insufficient stock for '{name}'");
        return false;
    }

    // Finds the name of the first product that does not have enough stock.
    private async Task<string> FindShortProductNameAsync(Order order, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product != null && product.StockQuantity < item.Quantity) return product.Name;
        }

        return "unknown";
    }

    // Step 3: price the order (SubTotal, Discount, Shipping, Total).
    private async Task<bool> CalculateTotalAsync(Order order, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await _pricing.CalculateTotalAsync(order, ct);
            Log.Success($"Order #{order.Id}", $"Step 3/6 Pricing ... OK ({order.TotalAmount}) in {sw.ElapsedMilliseconds} ms");
            return true;
        }
        catch (InvalidCouponException ex)
        {
            Log.Error($"Order #{order.Id}", $"Step 3/6 Pricing ... INVALID COUPON in {sw.ElapsedMilliseconds} ms");
            await _inventory.ReleaseStockAsync(order, CancellationToken.None);
            await FinishEarlyAsync(order, OrderStatus.Rejected, ex.Message);
            return false;
        }
    }

    // Step 4: take the payment (3 second timeout, or stop if the whole run is cancelled).
    private async Task<bool> ProcessPaymentAsync(Order order, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(3000);

        PaymentResult result;

        try
        {
            result = await _payment.ProcessPaymentAsync(order.Id, order.TotalAmount, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Log.Error($"Order #{order.Id}", $"Step 4/6 Payment ... TIMEOUT after {sw.ElapsedMilliseconds} ms, released");
            await _inventory.ReleaseStockAsync(order, CancellationToken.None);
            await FinishEarlyAsync(order, OrderStatus.PaymentFailed, "Payment gateway timeout (3s)");
            return false;
        }

        if (!result.Success)
        {
            Log.Error($"Order #{order.Id}", $"Step 4/6 Payment ... DECLINED in {sw.ElapsedMilliseconds} ms, released");
            await _inventory.ReleaseStockAsync(order, CancellationToken.None);
            await FinishEarlyAsync(order, OrderStatus.PaymentFailed, result.Message ?? "Card declined by issuer");
            return false;
        }

        order.TransactionId = result.TransactionId;
        Log.Success($"Order #{order.Id}", $"Step 4/6 Payment ... OK ({result.TransactionId}) in {sw.ElapsedMilliseconds} ms");
        return true;
    }

    // Step 5: mark the order Completed and save it.
    private async Task<bool> CompleteAndSaveAsync(Order order, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        order.Status = OrderStatus.Completed;
        order.FailureReason = null;

        try
        {
            await _orders.SaveAsync(order, ct);
            Log.Success($"Order #{order.Id}", $"Step 5/6 Save ... OK in {sw.ElapsedMilliseconds} ms");
            return true;
        }
        catch (DatabaseWriteException)
        {
            string txn = order.TransactionId!;

            Log.Error($"Order #{order.Id}", $"Step 5/6 Save ... FAILED in {sw.ElapsedMilliseconds} ms, refunding");
            await _payment.RefundAsync(txn, CancellationToken.None);
            await _inventory.ReleaseStockAsync(order, CancellationToken.None);
            await FinishEarlyAsync(order, OrderStatus.Failed, $"Database save failed after payment; payment {txn} refunded");
            return false;
        }
    }

    // Step 6: send the confirmation email with retry (Task 9.5).
    // A failed email never changes the order status.
    private async Task SendEmailAsync(Order order, Customer customer, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            string? lastError = await SendConfirmationWithRetryAsync(order, customer, ct);

            if (lastError == null)
            {
                Log.Success($"Order #{order.Id}", $"Step 6/6 Email ... OK in {sw.ElapsedMilliseconds} ms");
                return;
            }

            await QueueForRetryAsync(order, customer, lastError);
            Log.Info($"Order #{order.Id}", $"Email for Order #{order.Id} queued for retry later");
        }
        catch (OperationCanceledException)
        {
            Log.Info($"Order #{order.Id}", $"Step 6/6 Email ... CANCELLED in {sw.ElapsedMilliseconds} ms");
        }
    }

    // Tries to send the email up to 4 times (1 attempt + 3 retries).
    // Returns null on success, or the last error message if all 4 attempts failed.
    private async Task<string?> SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct)
    {
        int[] delays = { 500, 1000, 2000 };
        string? lastError = null;

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                await _email.SendConfirmationAsync(order, customer, ct);
                return null;
            }
            catch (SmtpUnavailableException ex)
            {
                lastError = ex.Message;
                Log.Error($"Order #{order.Id}", $"Step 6/6 Email ... attempt {attempt} failed: {ex.Message}");
            }

            if (attempt <= 3)
                await Task.Delay(delays[attempt - 1], ct);
        }

        return lastError;
    }

    // Saves the failed email to email_retry.json (Attempts = 4 and LastError).
    private async Task QueueForRetryAsync(Order order, Customer customer, string lastError)
    {
        var failed = new EmailMessage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} confirmation",
            Body = $"Your order #{order.Id} was completed.",
            Attempts = 4,
            LastError = lastError
        };

        await _store.UpdateAsync<EmailMessage>(DataFiles.EmailRetry, list => list.Add(failed));
    }

    // Sets the final status and reason, then saves the order.
    private async Task FinishEarlyAsync(Order order, OrderStatus status, string reason)
    {
        order.Status = status;
        order.FailureReason = reason;
        await _orders.SaveAsync(order, CancellationToken.None);
    }

    // The run was cancelled: refund a captured payment, release reserved stock,
    // then save the order as Pending with the reason "Cancelled".
    private async Task CancelAsync(Order order, bool reserved)
    {
        if (order.TransactionId != null)
            await _payment.RefundAsync(order.TransactionId, CancellationToken.None);

        if (reserved)
            await _inventory.ReleaseStockAsync(order, CancellationToken.None);

        order.TransactionId = null;
        await FinishEarlyAsync(order, OrderStatus.Pending, "Cancelled");
    }
}