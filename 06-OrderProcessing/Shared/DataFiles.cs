using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Shared;

public class DataFiles
{
    public static readonly string DataFolder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    public const string Customers= "customers.json";
    public const string Products = "products.json";
    public const string Coupons = "coupons.json";
    public const string IncomingOrders = "incoming_Orders.json";

    public const string Orders = "orders.json";
    public const string Payments = "payments.json";
    public const string Emails = "emails.json";
    public const string EmailRetry = "email_retry.json";



}

