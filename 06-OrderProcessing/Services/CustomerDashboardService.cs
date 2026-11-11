using _06_OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Services;

public class CustomerDashboardService
{
    private readonly HashSet<int> _failPaymentsForCustomerIds;

    public CustomerDashboardService(HashSet<int>? failPaymentsForCustomerIds = null)
    {
        _failPaymentsForCustomerIds = failPaymentsForCustomerIds ?? new HashSet<int>();
    }

    public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken ct = default)
    {
        var profileTask = LoadProfileAsync(customerId, ct);
        var ordersTask = LoadOrdersAsync(customerId, ct);
        var paymentsTask = LoadPaymentsAsync(customerId, ct);
        var emailsTask = LoadEmailsAsync(customerId, ct);

        try
        {
            await Task.WhenAll(profileTask, ordersTask, paymentsTask, emailsTask);
        }
        catch (Exception)
        {
            var exceptions = new List<Exception>();

            if (profileTask.IsFaulted && profileTask.Exception != null)
                exceptions.AddRange(profileTask.Exception.InnerExceptions);
            if (ordersTask.IsFaulted && ordersTask.Exception != null)
                exceptions.AddRange(ordersTask.Exception.InnerExceptions);
            if (paymentsTask.IsFaulted && paymentsTask.Exception != null)
                exceptions.AddRange(paymentsTask.Exception.InnerExceptions);
            if (emailsTask.IsFaulted && emailsTask.Exception != null)
                exceptions.AddRange(emailsTask.Exception.InnerExceptions);

            throw new AggregateException(exceptions);
        }

        return new CustomerDashboard
        {
            Customer = profileTask.Result,
            Orders = ordersTask.Result,
            Payments = paymentsTask.Result,
            Emails = emailsTask.Result
        };
    }

    private async Task<Customer> LoadProfileAsync(int customerId, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        if (customerId == 99)
        {
            throw new KeyNotFoundException($"Customer #{customerId} not found");
        }
        return new Customer
        {
            Id = customerId,
            Name = customerId == 3 ? "Omar Hassan" : $"Customer {customerId}",
            Email = $"customer{customerId}@example.com",
            IsActive = true
        };
    }

    private async Task<List<Order>> LoadOrdersAsync(int customerId, CancellationToken ct)
    {
        await Task.Delay(150, ct);
        if (customerId == 99) return new List<Order>();

        if (customerId == 3)
        {
            return new List<Order>
            {
                new Order { Id = 3, Status = OrderStatus.Completed },
                new Order { Id = 13, Status = OrderStatus.Failed }
            };
        }
        return new List<Order>();
    }

    private async Task<List<PaymentTransaction>> LoadPaymentsAsync(int customerId, CancellationToken ct)
    {
        await Task.Delay(120, ct);

        if (_failPaymentsForCustomerIds.Contains(customerId))
        {
            throw new TimeoutException("Payments store timeout");
        }

        if (customerId == 3)
        {
            return new List<PaymentTransaction>
            {
                new PaymentTransaction { OrderId = 3, Status = PaymentStatus.Captured, Amount = 150.0m },
                new PaymentTransaction { OrderId = 13, Status = PaymentStatus.Refunded, Amount = 75.5m }
            };
        }
        return new List<PaymentTransaction>();
    }

    private async Task<List<EmailMessage>> LoadEmailsAsync(int customerId, CancellationToken ct)
    {
        await Task.Delay(80, ct);
        if (customerId == 3)
        {
            return new List<EmailMessage>
            {
                new EmailMessage { OrderId = 3, Subject = "Order Confirmed", To = "customer3@example.com" }
            };
        }
        return new List<EmailMessage>();
    }
}

