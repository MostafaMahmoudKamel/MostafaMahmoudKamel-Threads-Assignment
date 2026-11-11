namespace _06_OrderProcessing.Services;

using global::_06_OrderProcessing.Models;
using global::_06_OrderProcessing.Shared;

public class PaymentGateway : IPaymentGateway
{
    private readonly JsonFileStore _store = new JsonFileStore();
    private readonly HashSet<int> _timeoutOrderIds;
    private readonly HashSet<int> _declineOrderIds;

    public PaymentGateway(IEnumerable<int>? timeoutOrderIds = null, IEnumerable<int>? declineOrderIds = null)
    {
        _timeoutOrderIds = new HashSet<int>(timeoutOrderIds ?? Array.Empty<int>());
        _declineOrderIds = new HashSet<int>(declineOrderIds ?? Array.Empty<int>());
    }

    public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
    {
        Log.Info("PaymentGateway", $"Order #{orderId}: authorizing {amount}...");
        await Task.Delay(800, ct);   

        Log.Info("PaymentGateway", $"Order #{orderId}: fraud check...");
        await Task.Delay(500, ct);   

        if (_declineOrderIds.Contains(orderId))
        {
            return await DeclineAsync(orderId, amount, ct);
        }

        // payment TimeOut 
        int captureDelay = _timeoutOrderIds.Contains(orderId) ? 5000 : 700;
        Log.Info("PaymentGateway", $"Order #{orderId}: capturing...");
        await Task.Delay(captureDelay, ct);

        return await CaptureAsync(orderId, amount, ct);
    }

    //payment declined
    private async Task<PaymentResult> DeclineAsync(int orderId, decimal amount, CancellationToken ct)
    {
        var transaction = NewTransaction(orderId, amount, PaymentStatus.Declined);
        await _store.UpdateAsync<PaymentTransaction>(DataFiles.Payments, list => list.Add(transaction), ct);

        Log.Error("PaymentGateway", $"Order #{orderId}: payment declined");
        return new PaymentResult
        {
            Success = false,
            TransactionId = transaction.TransactionId,
            Status = PaymentStatus.Declined,
            Message = "Card declined by issuer"
        };
    }

    private async Task<PaymentResult> CaptureAsync(int orderId, decimal amount, CancellationToken ct)
    {
        var transaction = NewTransaction(orderId, amount, PaymentStatus.Captured);
        await _store.UpdateAsync<PaymentTransaction>(DataFiles.Payments, list => list.Add(transaction), ct);

        Log.Success("PaymentGateway", $"Order #{orderId}: payment captured ({transaction.TransactionId})");
        return new PaymentResult
        {
            Success = true,
            TransactionId = transaction.TransactionId,
            Status = PaymentStatus.Captured,
            Message = "Payment captured"
        };
    }

    private PaymentTransaction NewTransaction(int orderId, decimal amount, PaymentStatus status)
    {
        return new PaymentTransaction
        {
            OrderId = orderId,
            Amount = amount,
            Status = status,
            TransactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper(),
            CreatedAt = DateTime.Now
        };
    }

    public async Task RefundAsync(string transactionId, CancellationToken ct)
    {
        Log.Info("PaymentGateway", $"Refund {transactionId}: handshake...");
        await Task.Delay(150, ct);

        Log.Info("PaymentGateway", $"Refund {transactionId}: sending request...");
        await Task.Delay(300, ct);

        Log.Info("PaymentGateway", $"Refund {transactionId}: waiting for response...");
        await Task.Delay(Random.Shared.Next(500, 1501), ct);

        await _store.UpdateAsync<PaymentTransaction>(DataFiles.Payments, list =>
        {
            var record = list.FirstOrDefault(p => p.TransactionId == transactionId);
            if (record != null) record.Status = PaymentStatus.Refunded;
        }, ct);

        Log.Success("PaymentGateway", $"{transactionId} refunded");
    }
}