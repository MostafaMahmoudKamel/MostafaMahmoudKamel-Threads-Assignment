using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Models;

public class Coupon
{
    public string Code { get; set; } = string.Empty;
    public int DiscountPercent { get; set; }
    public DateTime ExpiryDate { get; set; }
}
