using _06_OrderProcessing.Models;
using _06_OrderProcessing.Repositories.AsyncRepositories;
using _06_OrderProcessing.Repositories.syncRepositories;
using _06_OrderProcessing.Services;
using _06_OrderProcessing.Services.syncService;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace _06_OrderProcessing.Runners;

public class Phase1Runner
{
    public static async Task RunAsync(CancellationToken ct)
    {
        //Asyn rpo
        var incomingRepo = new IncomingOrderRepository();
        var orderRepo = new OrderRepository();

        //1)reset
        await DataSeeder.ResetAsync();

        //2)load 20 orders
        var orders = await incomingRepo.GetAllAsync(ct);

        // 3)sync run (results not saved)
        var syncOrderValidator = new SyncOrderValidator();
        var syncWatch = Stopwatch.StartNew();
        foreach (var order in orders)
            syncOrderValidator.ValidateOrder(order);
        syncWatch.Stop();

        //4)reset again Async
        await DataSeeder.ResetAsync();
        orders = await incomingRepo.GetAllAsync(ct);

        //5 
        var asyncOrderValidator = new OrderValidator();
        var asyncWatch = Stopwatch.StartNew();
        foreach (var order in orders)
        {
            await asyncOrderValidator.ValidateOrderAsync(order,ct);
            await orderRepo.SaveAsync(order, ct);
        }
        asyncWatch.Stop();

        var all = await orderRepo.GetAllAsync(ct);
        var validated = await orderRepo.GetByStatusAsync(OrderStatus.Validated, ct);
        var rejected = await orderRepo.GetByStatusAsync(OrderStatus.Rejected, ct);

        var inv = System.Globalization.CultureInfo.InvariantCulture;

        Console.WriteLine();
        Console.WriteLine("===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {all.Count}");
        Console.WriteLine($"Validated:    {validated.Count}");
        Console.WriteLine($"Rejected:     {rejected.Count}");

        foreach (var o in rejected.OrderBy(o => o.Id))
            Console.WriteLine($"  - Order #{o.Id}:".PadRight(14) + " " + o.FailureReason);

        Console.WriteLine($"Sync Time:  {syncWatch.Elapsed.TotalSeconds.ToString("F1", inv)} seconds");
        Console.WriteLine($"Async Time: {asyncWatch.Elapsed.TotalSeconds.ToString("F1", inv)} seconds");
        Console.WriteLine($"orders.json now contains {all.Count} records.");


    }

}
