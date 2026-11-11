using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Repositories.syncRepositories;



public class SyncCouponRepository
{
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();

    public Coupon? GetByCode(string code)
    {
        Log.Info("SyncCouponRepository", "Opening connection...");
        Thread.Sleep(200);

        Log.Info("SyncCouponRepository", $"Executing query for Coupon '{code}'...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var coupons = _jsonFileStore.ReadAll<Coupon>(DataFiles.Coupons);
        var coupon = coupons.FirstOrDefault(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

        if (coupon is null) Log.Error("SyncCouponRepository", $"Coupon '{code}' not found");
        else Log.Info("SyncCouponRepository", $"Coupon '{coupon.Code}' found ({coupon.DiscountPercent}%)");
        return coupon;
    }
}
