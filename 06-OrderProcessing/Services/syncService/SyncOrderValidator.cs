using _06_OrderProcessing.Models;
using _06_OrderProcessing.Repositories.syncRepositories;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Services.syncService;

public class SyncOrderValidator
{
    private readonly SyncCustomerRepository _customers = new();
    private readonly SyncProductRepository _products = new();
    private readonly SyncCouponRepository _coupons = new();

    public Order ValidateOrder(Order order)
    {
        var customer = _customers.GetById(order.CustomerId);
        if (customer is null) return Reject(order, $"Customer #{order.CustomerId} not found");
        if (!customer.IsActive) return Reject(order, "Customer is inactive");

        foreach (var item in order.Items)
            if (_products.GetById(item.ProductId) is null)
                return Reject(order, $"Product #{item.ProductId} not found");

        foreach (var item in order.Items)
        {
            var product = _products.GetById(item.ProductId)!;
            if (product.StockQuantity < item.Quantity)
                return Reject(order, $"Insufficient stock for '{product.Name}'");
        }

        if (order.CouponCode is not null)
        {
            var coupon = _coupons.GetByCode(order.CouponCode);
            if (coupon is null) return Reject(order, $"Coupon '{order.CouponCode}' does not exist");
            if (coupon.ExpiryDate < DateTime.Today) return Reject(order, $"Coupon '{order.CouponCode}' has expired");
        }

        order.Status = OrderStatus.Validated;
        order.FailureReason = null;
        return order;
    }

    private Order Reject(Order order, string reason)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;
        Log.Error("SyncOrderValidator", $"Order #{order.Id} rejected: {reason}");
        return order;
    }
}
