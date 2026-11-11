using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Repositories.AsyncRepositories;

public class CouponRepository
{
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
    {
        Log.Info("CouponRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("CouponRepository", $"Executing query for Coupon {code}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        var coupons = await _jsonFileStore.ReadAllAsync<Coupon>(DataFiles.Coupons, ct);
        var coupon = coupons.FirstOrDefault(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

        if (coupon == null) Log.Error("CouponRepository", $"Coupon {code} not found");
        else Log.Success("CouponRepository", $"Coupon {code} found ({coupon.DiscountPercent}%)");

        return coupon;
    }
}
