namespace _06_OrderProcessing.Services;

using _06_OrderProcessing.Models;

public interface IEmailService
{
    Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct);
}