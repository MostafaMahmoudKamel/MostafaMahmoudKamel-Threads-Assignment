using _06_OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Shared;

//هيكتبلي كل الداتا الاوليه 
public class DataSeeder
{
    public static async Task ResetAsync()
    {
        //if found delete the folder and all its contents
        if (Directory.Exists(DataFiles.DataFolder))
        {
            Directory.Delete(DataFiles.DataFolder, true);
        }

        //if not found create the folder
        Directory.CreateDirectory(DataFiles.DataFolder);

        var store = new JsonFileStore();


        var customers = new List<Customer>
        {
            new() { Id = 1,  Name = "Ahmed Ali",     Email = "ahmed.ali@example.com",     IsActive = true,  LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 2,  Name = "Sara Mohamed",  Email = "sara.mohamed@example.com",  IsActive = true,  LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 3,  Name = "Omar Hassan",   Email = "omar.hassan@example.com",   IsActive = true,  LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 4,  Name = "Mona Ibrahim",  Email = "mona.ibrahim@example.com",  IsActive = false, LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 5,  Name = "Youssef Adel",  Email = "youssef.adel@example.com",  IsActive = true,  LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 6,  Name = "Nour Khaled",   Email = "nour.khaled@example.com",   IsActive = true,  LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 7,  Name = "Karim Samir",   Email = "karim.samir@example.com",   IsActive = true,  LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 8,  Name = "Laila Mahmoud", Email = "laila.mahmoud@example.com", IsActive = true,  LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 9,  Name = "Hassan Tarek",  Email = "hassan.tarek@example.com",  IsActive = true,  LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 10, Name = "Dina Fathy",    Email = "dina.fathy@example.com",    IsActive = true,  LoyaltyTier = LoyaltyTier.Silver },
        };


        var products = new List<Product>
        {
        new() { Id = 1, Name = "Laptop",              Price = 1200.00m, StockQuantity = 10, WeightKg = 2.2m },
        new() { Id = 2, Name = "Smartphone",          Price = 800.00m,  StockQuantity = 15, WeightKg = 0.4m },
        new() { Id = 3, Name = "Wireless Mouse",      Price = 25.00m,   StockQuantity = 2,  WeightKg = 0.1m },
        new() { Id = 4, Name = "Mechanical Keyboard", Price = 90.00m,   StockQuantity = 1,  WeightKg = 0.9m },
        new() { Id = 5, Name = "USB-C Hub",           Price = 45.00m,   StockQuantity = 30, WeightKg = 0.2m },
        new() { Id = 6, Name = "Headphones",          Price = 150.00m,  StockQuantity = 12, WeightKg = 0.3m },
        new() { Id = 7, Name = "27\" Monitor",        Price = 350.00m,  StockQuantity = 6,  WeightKg = 5.0m },
        new() { Id = 8, Name = "Webcam",              Price = 70.00m,   StockQuantity = 20, WeightKg = 0.2m },
        };

        var copouns = new List<Coupon>
        {

            new() { Code = "SAVE10",    DiscountPercent = 10, ExpiryDate = DateTime.Today.AddDays(180) },
            new() { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = DateTime.Today.AddDays(90)  },
            new() { Code = "FLASH20",   DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(7)   },
            new() { Code = "OLD20",     DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(-30) }, // expired coupon

        };

        var orders = new List<Order>
        {
            new ()
            {
                Id = 1, CustomerId = 1, CreatedAt = DateTime.Now,CouponCode = "SAVE10", TotalAmount = 0m,Status = OrderStatus.Pending, 
                Items=new List<OrderItem>
                {
                    new (){ProductId=1, Quantity=1,UnitPrice=0},
                    new (){ProductId=3, Quantity=1,UnitPrice=0}
                }
            },
            new()
            {
                Id = 2, CustomerId = 2, CreatedAt = DateTime.Now, TotalAmount = 0m,Status = OrderStatus.Pending,
                Items=new List<OrderItem>
                {
                    new (){ProductId=2, Quantity=1,UnitPrice=0},
                }
            },
            new()
            {
                Id = 3, CustomerId = 3, CreatedAt = DateTime.Now,CouponCode = "WELCOME15", TotalAmount = 0m,Status = OrderStatus.Pending,
                Items=new List<OrderItem>
                {
                    new (){ProductId=5, Quantity=2,UnitPrice=0},
                    new (){ProductId=8, Quantity=1,UnitPrice=0}
                }
            },
            new()
            {
                Id = 4, CustomerId = 4, CreatedAt = DateTime.Now, TotalAmount = 0m,Status = OrderStatus.Pending,
                Items=new List<OrderItem>
                {
                    new (){ProductId=6, Quantity=1,UnitPrice=0}
                }
            },
            new()
            {
                Id = 5, CustomerId = 5, CreatedAt = DateTime.Now, TotalAmount = 0m,Status = OrderStatus.Pending,
                Items=new List<OrderItem>
                {
                    new (){ProductId=7, Quantity=1,UnitPrice=0},
                }
            },
            new()
            {
                Id = 6, CustomerId = 6, CreatedAt = DateTime.Now,CouponCode = "FLASH20", TotalAmount = 0m,Status = OrderStatus.Pending,
                Items=new List<OrderItem>
                {
                    new (){ProductId=5, Quantity=1,UnitPrice=0},
                }
            },
            new()
            {
                Id = 7, CustomerId = 7, CreatedAt = DateTime.Now, TotalAmount = 0m,Status = OrderStatus.Pending,
                Items=new List<OrderItem>
                {
                    new (){ProductId=6, Quantity=1,UnitPrice=0},
                    new (){ProductId=8, Quantity=1,UnitPrice=0}
                }
            },
            new()
            {
                Id = 8, CustomerId = 8, CreatedAt = DateTime.Now,CouponCode = "SAVE10", TotalAmount = 0m,Status = OrderStatus.Pending,
                Items=new List<OrderItem>
                {
                    new (){ProductId=1, Quantity=1,UnitPrice=0},
                }
            },
                        new()
            {
                Id = 9, CustomerId = 9, CreatedAt = DateTime.Now, TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 3, Quantity = 5, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 10, CustomerId = 10, CreatedAt = DateTime.Now, TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 4, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 11, CustomerId = 1, CreatedAt = DateTime.Now, TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 4, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 12, CustomerId = 2, CreatedAt = DateTime.Now, CouponCode = "OLD20", TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 2, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 13, CustomerId = 3, CreatedAt = DateTime.Now, TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 6, Quantity = 2, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 14, CustomerId = 5, CreatedAt = DateTime.Now, CouponCode = "WELCOME15", TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 8, Quantity = 2, UnitPrice = 0 },
                    new() { ProductId = 5, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 15, CustomerId = 99, CreatedAt = DateTime.Now, TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 2, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 16, CustomerId = 6, CreatedAt = DateTime.Now, TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 7, Quantity = 1, UnitPrice = 0 },
                    new() { ProductId = 5, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 17, CustomerId = 7, CreatedAt = DateTime.Now, CouponCode = "SAVE10", TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 2, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 18, CustomerId = 8, CreatedAt = DateTime.Now, CouponCode = "FAKE50", TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 6, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 19, CustomerId = 9, CreatedAt = DateTime.Now, TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 8, Quantity = 1, UnitPrice = 0 },
                }
            },
            new()
            {
                Id = 20, CustomerId = 10, CreatedAt = DateTime.Now, CouponCode = "FLASH20", TotalAmount = 0m, Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 1, Quantity = 1, UnitPrice = 0 },
                    new() { ProductId = 6, Quantity = 1, UnitPrice = 0 },
                }
            },
        };
       

        //write in customers.json
        await store.WriteAllAsync(DataFiles.Customers, customers);
        Console.WriteLine($"[Seeder] {DataFiles.Customers} -> {customers.Count} records");

        //write in products.json
        await store.WriteAllAsync(DataFiles.Products, products);
        Console.WriteLine($"[Seeder] {DataFiles.Products} -> {products.Count} records");

        //write in coupons.json
        await store.WriteAllAsync(DataFiles.Coupons, copouns);
        Console.WriteLine($"[Seeder] {DataFiles.Coupons} -> {copouns.Count} records");

        //write in incoming_orders.json
        await store.WriteAllAsync(DataFiles.IncomingOrders, orders);
        Console.WriteLine($"[Seeder] {DataFiles.IncomingOrders} -> {orders.Count} records");

        //write in orders.json
        await store.WriteAllAsync(DataFiles.Orders, new List<Order>());
        Console.WriteLine($"[Seeder] {DataFiles.Orders} -> 0 records");

        //write in payments.json
        await store.WriteAllAsync(DataFiles.Payments, new List<PaymentTransaction>());
        Console.WriteLine($"[Seeder] {DataFiles.Payments} -> 0 records");

        //write in emails.json
        await store.WriteAllAsync(DataFiles.Emails, new List<EmailMessage>());
        Console.WriteLine($"[Seeder] {DataFiles.Emails} -> 0 records");

        //write in email_retry.json
        await store.WriteAllAsync(DataFiles.EmailRetry, new List<EmailMessage>());
        Console.WriteLine($"[Seeder] {DataFiles.EmailRetry} -> 0 records");
    }

}
