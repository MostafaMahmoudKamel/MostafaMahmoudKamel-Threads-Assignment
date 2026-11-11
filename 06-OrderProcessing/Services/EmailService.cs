using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System.Collections.Concurrent;

namespace _06_OrderProcessing.Services;

public class EmailService : IEmailService
{
    private readonly JsonFileStore _store = new JsonFileStore();
    private readonly Dictionary<int, int> _failAttempts;
    private readonly ConcurrentDictionary<int, int> _attemptCounts = new();

    // المفتاح = رقم الأوردر، القيمة = كام محاولة تفشل
    public EmailService(Dictionary<int, int>? failAttempts = null)
    {
        _failAttempts = failAttempts ?? new Dictionary<int, int>();
    }

    public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct)
    {
        int attempt = _attemptCounts.AddOrUpdate(order.Id, 1, (_, old) => old + 1);

        Log.Info("EmailService", $"Order #{order.Id}: rendering template (attempt {attempt})...");
        await Task.Delay(200, ct);

        Log.Info("EmailService", $"Order #{order.Id}: connecting to SMTP server...");
        await Task.Delay(300, ct);

        Log.Info("EmailService", $"Order #{order.Id}: sending email to {customer.Email}...");
        await Task.Delay(500, ct);

        if (ShouldFail(order.Id, attempt))
        {
            throw new SmtpUnavailableException("SMTP server unavailable");
        }

        await SaveMessageAsync(order, customer, attempt, ct);
        Log.Success("EmailService", $"Order #{order.Id}: confirmation sent to {customer.Email}");
    }

    // Order should fail
    private bool ShouldFail(int orderId, int attempt)
    {
        return _failAttempts.TryGetValue(orderId, out int failCount) && attempt <= failCount;
    }

    private async Task SaveMessageAsync(Order order, Customer customer, int attempt, CancellationToken ct)
    {
        var message = new EmailMessage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} confirmed",
            Body = $"Hello {customer.Name}, your order #{order.Id} was completed. Total: {order.TotalAmount}.",
            SentAt = DateTime.Now,
            Attempts = attempt,
            LastError = null
        };

        await _store.UpdateAsync<EmailMessage>(DataFiles.Emails, list => list.Add(message), ct);
    }
}