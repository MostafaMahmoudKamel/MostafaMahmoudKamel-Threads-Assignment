namespace _06_OrderProcessing.Services;

using global::_06_OrderProcessing.Models;
using global::_06_OrderProcessing.Repositories.AsyncRepositories;
using global::_06_OrderProcessing.Shared;


public class PricingService
{
    private readonly OrderPricer _pricer;
    private readonly CouponRepository _couponRepository = new CouponRepository();

    public PricingService(IEnumerable<int> failShippingForOrderIds)
    {
        _pricer = new OrderPricer(failShippingForOrderIds);
    }

    public async Task<decimal> CalculateTotalAsync(Order order, CancellationToken ct)
    {
        // 1) coupon check first: missing or expired -> InvalidCouponException
        if (order.CouponCode != null)
        {
            var coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, ct);

            if (coupon == null)
                throw new InvalidCouponException($"Coupon '{order.CouponCode}' does not exist");

            if (coupon.ExpiryDate < DateTime.Today)
                throw new InvalidCouponException($"Coupon '{order.CouponCode}' has expired");
        }

        // 2) fills SubTotal, Discount, ShippingCost, TotalAmount
        //    (SubTotal first, then discount + shipping concurrently)
        await _pricer.PriceOrderAsync(order, ct);

        return order.TotalAmount;
    }
}
