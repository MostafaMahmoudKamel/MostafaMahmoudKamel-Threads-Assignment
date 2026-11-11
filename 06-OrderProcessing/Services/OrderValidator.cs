using _06_OrderProcessing.Models;
using _06_OrderProcessing.Repositories.AsyncRepositories;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Services;

public class OrderValidator
{
    private readonly CustomerRepository _customerRepository = new CustomerRepository();
    private readonly ProductRepository _productRepository = new ProductRepository();
    private readonly OrderRepository _orderRepository = new OrderRepository();
    private readonly CouponRepository _couponRepository = new CouponRepository();

    public async Task<Order> ValidateOrderAsync(Order order,CancellationToken ct)
    {

        //check 1:customer exists
        var customer=await _customerRepository.GetByIdAsync(order.CustomerId, ct);
        if (customer == null)
            return await RejectAsync(order, $"Customer #{order.CustomerId} not found", ct);

        //Check 2:customer not active
        if (!customer.IsActive)
              return await RejectAsync(order, "Customer is inactive", ct);

        //product exists
        // Check 3: Every product exists
        foreach (var item in order.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            if (product is null)
                return await RejectAsync(order, $"Product #{item.ProductId} not found", ct);
        }

        //check 4:
        foreach (var item in order.Items)
        {
            //var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            if (product!.StockQuantity <item.Quantity)
                return await RejectAsync(order, $"Insufficient stock for '{product.Name}'", ct);
        }

        // Check 5 + 6: Coupon
        if (order.CouponCode is not null)
        {
            var coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, ct);

            // Check 5: Coupon exists
            if (coupon is null)
                return await RejectAsync(order, $"Coupon '{order.CouponCode}' does not exist", ct);

            // Check 6: Coupon not expired (الرفض لما ExpiryDate < Today)
            if (coupon.ExpiryDate < DateTime.Today)
                return await RejectAsync(order, $"Coupon '{order.CouponCode}' has expired", ct);
        }
        order.Status = OrderStatus.Validated;
        order.FailureReason = null;

        Log.Success("OrderValidator", $"Order #{order.Id} validated");
        return order;



    }

    private async Task<Order> RejectAsync(Order order, string reason, CancellationToken ct)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;
        //await _orderRepository.SaveAsync(order, ct);

        Log.Error("OrderValidator", $"Order #{order.Id} rejected: {reason}");
        return order;
    }
}
