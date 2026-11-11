using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System.Diagnostics;

namespace _06_OrderProcessing.Services;

public class SyncOrderProcessor
{
    private readonly JsonFileStore _store = new();

    public bool ProcessOrder(Order order)
    {
        try
        {
            // 1. التحقق من العميل
            var customers = _store.ReadAll<Customer>(DataFiles.Customers);
            var customer = customers.FirstOrDefault(c => c.Id == order.CustomerId);

            if (customer == null || !customer.IsActive)
            {
                FinishEarly(order, OrderStatus.Rejected, customer == null ? $"Customer #{order.CustomerId} not found" : "Customer is inactive");
                return false;
            }

            // 2. محاكاة حجز المخزون بالـ Thread.Sleep
            Thread.Sleep(200); // Open connection
            Thread.Sleep(100); // Begin transaction
            var products = _store.ReadAll<Product>(DataFiles.Products);

            foreach (var item in order.Items)
            {
                var prod = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod == null || prod.StockQuantity < item.Quantity)
                {
                    FinishEarly(order, OrderStatus.Rejected, $"Insufficient stock for '{prod?.Name ?? "unknown"}'");
                    return false;
                }
            }

            // خصم الكمية
            foreach (var item in order.Items)
            {
                var prod = products.First(p => p.Id == item.ProductId);
                prod.StockQuantity -= item.Quantity;
            }
            Thread.Sleep(400); // Update
            _store.WriteAll(DataFiles.Products, products);
            Thread.Sleep(200); // Commit

            // 3. إنهاء الأوردر بنجاح
            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            _store.Update<Order>(DataFiles.Orders, list =>
            {
                var idx = list.FindIndex(o => o.Id == order.Id);
                if (idx >= 0) list[idx] = order;
                else list.Add(order);
            });

            return true;
        }
        catch (Exception ex)
        {
            FinishEarly(order, OrderStatus.Failed, ex.Message);
            return false;
        }
    }

    private void FinishEarly(Order order, OrderStatus status, string reason)
    {
        order.Status = status;
        order.FailureReason = reason;
        _store.Update<Order>(DataFiles.Orders, list =>
        {
            var idx = list.FindIndex(o => o.Id == order.Id);
            if (idx >= 0) list[idx] = order;
            else list.Add(order);
        });
    }
}