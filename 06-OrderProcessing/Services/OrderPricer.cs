using _06_OrderProcessing.Models;
using _06_OrderProcessing.Repositories.AsyncRepositories;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Services;



public class OrderPricer
{
    private readonly ProductRepository _productRepository = new ProductRepository();
    private readonly CustomerRepository _customerRepository = new CustomerRepository();
    private readonly CouponRepository _couponRepository = new CouponRepository();
    private readonly HashSet<int> _failShippingIds;

    public OrderPricer(IEnumerable<int> failShippingForOrderIds)
    {
        _failShippingIds = new HashSet<int>(failShippingForOrderIds);
    }

    // Task 8.1 — Pricing rules (exact)

    public async Task PriceOrderAsync(Order order, Customer customer, Coupon? coupon, CancellationToken ct)
    {
        await UpdatingUnitPriceAsync(order, ct);

        decimal subTotal = CalcSubTotal(order);
        decimal percent = CalcDiscountPercent(customer, coupon);
        decimal discount = subTotal * percent / 100;
        decimal shipping = await CalcShippingAsync(order, subTotal, ct);

        order.SubTotal = Round(subTotal);//1) subtotal
        order.Discount = Round(discount);//4) discount
        order.ShippingCost = Round(shipping);//5) shipping cost

        order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);//6) toalAmount 
    }

    //1) update unit test
    public async Task UpdatingUnitPriceAsync(Order order, CancellationToken ct)
    {
        //update unitPrice for each product in order from json file
        foreach (var item in order.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            item.UnitPrice = product!.Price;
        }
    }

    //2)subtotal = summition  (unitPrice * quantity) 
    private decimal CalcSubTotal(Order order)
    {
        decimal subTotal = 0;

        foreach (var item in order.Items)
        {
            subTotal += item.UnitPrice * item.Quantity;
        }

        return subTotal;
    }

    //3) get discount percent 
    private decimal GetLoyaltyPercent(LoyaltyTier tier)
    {
        if (tier == LoyaltyTier.Gold) return 10;//gold gets 10% discount
        if (tier == LoyaltyTier.Silver) return 5;//silver gets 5% discount
        return 0;
    }

    //4) get total discount 
    private decimal CalcDiscountPercent(Customer customer, Coupon? coupon)
    {
        decimal loyalty = GetLoyaltyPercent(customer.LoyaltyTier);
        decimal couponPercent = 0;

        if (coupon != null)
        {
            couponPercent = coupon.DiscountPercent;
        }

        decimal total = loyalty + couponPercent;//total discount percent

        if (total > 30) return 30;//max discount percent is 30%
        return total;
    }

    //5) الشحن Shipping cost
    private async Task<decimal> CalcShippingAsync(Order order, decimal subTotal, CancellationToken ct)
    {
        
        if (subTotal >= 1000) return 0;//shipping= 0 ; if(total >= 1000)

        int totalQty = 0;
        decimal totalWeight = 0;

        foreach (var item in order.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            totalQty += item.Quantity;
            totalWeight += product!.WeightKg * item.Quantity;
        }

        return 5.00m + 1.50m * totalQty + 2.00m * totalWeight;
    }

    //round money to 2 decimal places
    private decimal Round(decimal x)
    {
        return Math.Round(x, 2, MidpointRounding.AwayFromZero);
    }


    //Task 8.2 — PricingService and concurrent awaits

    // 1) DB query
    public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
    {
        await UpdatingUnitPriceAsync(order, ct);
        return Round(CalcSubTotal(order));
    }

    // 2) Get discount async 
    public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
    {
        await Task.Delay(150, ct);                           // DNS/handshake
        await Task.Delay(300, ct);                           // Send request
        await Task.Delay(Random.Shared.Next(500, 1501), ct); // Wait for response

        var customer = await _customerRepository.GetByIdAsync(order.CustomerId, ct);
        Coupon? coupon = null;

        if (order.CouponCode != null)
        {
            coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, ct);
        }

        decimal percent = CalcDiscountPercent(customer!, coupon);
        return Round(order.SubTotal * percent / 100);
    }

    // 3) Shipping API
    public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
    {
        await Task.Delay(150, ct);                           // DNS/handshake

        if (_failShippingIds.Contains(order.Id))
        {
            throw new ShippingProviderUnavailableException("Shipping provider unavailable");
        }

        await Task.Delay(300, ct);                           // Send request
        await Task.Delay(Random.Shared.Next(500, 1501), ct); // Wait for response

        if (order.SubTotal >= 1000) return 0;

        int totalQty = 0;
        decimal totalWeight = 0;

        foreach (var item in order.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            totalQty += item.Quantity;
            totalWeight += product!.WeightKg * item.Quantity;
        }

        return Round(5.00m + 1.50m * totalQty + 2.00m * totalWeight);
    }

    // Concurrent: discount + shipping at the same time
    public async Task PriceOrderAsync(Order order, CancellationToken ct)
    {
        order.SubTotal = await CalculateSubTotalAsync(order, ct);

        Task<decimal> discountTask = GetDiscountAsync(order, ct);
        Task<decimal> shippingTask = CalculateShippingCostAsync(order, ct);

        await Task.WhenAll(discountTask, shippingTask);

        order.Discount = discountTask.Result;
        order.ShippingCost = shippingTask.Result;
        order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
    }

    // Sequential: await each call right after starting it
    public async Task PriceOrderSequentialAsync(Order order, CancellationToken ct)
    {
        order.SubTotal = await CalculateSubTotalAsync(order, ct);

        order.Discount = await GetDiscountAsync(order, ct);
        order.ShippingCost = await CalculateShippingCostAsync(order, ct);

        order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
    }



}