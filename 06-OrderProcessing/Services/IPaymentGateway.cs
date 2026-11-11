namespace _06_OrderProcessing.Services;

using global::_06_OrderProcessing.Models;


public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct);
    Task RefundAsync(string transactionId,CancellationToken ct);
}

