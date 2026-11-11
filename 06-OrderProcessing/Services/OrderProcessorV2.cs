using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Services;

using _06_OrderProcessing.Models;
using _06_OrderProcessing.Repositories.AsyncRepositories;


public class OrderProcessorV2
{
    private readonly ProductRepository _productRepository = new ProductRepository();
    private readonly OrderRepository _orderRepository = new OrderRepository();
    private readonly OrderPricer _pricer;
     private readonly InvoiceGenerator _invoiceGenerator = new InvoiceGenerator();  // 8.5  invokeGenerator

    public OrderProcessorV2(OrderPricer pricer)
    {
        _pricer = pricer;
    }

    public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken ct)
    {
        // 1) 
        string? failedProduct = await ReserveAllAsync(order, ct);

        if (failedProduct != null)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = $"Insufficient stock for '{failedProduct}'";
            await _orderRepository.SaveAsync(order, ct);
            return false;
        }

        await _pricer.PriceOrderAsync(order, ct);

        //3) invoke
         await _invoiceGenerator.GenerateAsync(order,ct);

        order.Status = OrderStatus.Completed;
        await _orderRepository.SaveAsync(order, ct);
        return true;
    }

    //ReserveAll
    private async Task<string?> ReserveAllAsync(Order order, CancellationToken ct)
    {
        var reserved = new List<OrderItem>();

        foreach (var item in order.Items)
        {
            bool ok = await _productRepository.ReserveStockAsync(item.ProductId, item.Quantity, ct);

            if (!ok)
            {
                await ReleaseItemsAsync(reserved, ct);
                var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
                return product!.Name;
            }

            reserved.Add(item);
        }

        return null;
    }

    
    private async Task ReleaseItemsAsync(List<OrderItem> items, CancellationToken ct)
    {
        foreach (var item in items)
        {
            await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
        }
    }

    
    public async Task ReleaseOrderStockAsync(Order order, CancellationToken ct)
    {
        await ReleaseItemsAsync(order.Items, ct);
    }
}

