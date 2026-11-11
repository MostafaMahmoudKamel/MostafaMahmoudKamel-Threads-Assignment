namespace _06_OrderProcessing.Runners;


using global::_06_OrderProcessing.Models;
using global::_06_OrderProcessing.Repositories.AsyncRepositories;
using global::_06_OrderProcessing.Services;
using global::_06_OrderProcessing.Shared;
using System.Diagnostics;

public class Phase2Runner
{
    public static async Task RunAsync(CancellationToken ct)
    {
        var incomingRepo = new IncomingOrderRepository();
        var orderRepo = new OrderRepository();


        //  1) Reset 
        await DataSeeder.ResetAsync();
        await RaceConditionDemo.RunAsync(ct);
        await DataSeeder.ResetAsync();

        // 2) Pricing comparison on order 
        var pricer = new OrderPricer(new[] { 7 });   

        var orders = await incomingRepo.GetAllAsync(ct);
        var order1 = orders.First(o => o.Id == 1);

        var sw = Stopwatch.StartNew();
        await pricer.PriceOrderAsync(order1, ct);
        sw.Stop();
        Console.WriteLine($"Concurrent pricing: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        await pricer.PriceOrderSequentialAsync(order1, ct);
        sw.Stop();
        Console.WriteLine($"Sequential pricing: {sw.ElapsedMilliseconds} ms");

        
        var total = Stopwatch.StartNew();

        //  Validate all 20 concurrently 
        orders = await incomingRepo.GetAllAsync(ct);

        var validator = new OrderValidator();
        var validationTasks = orders.Select(async o =>
        {
            await validator.ValidateOrderAsync(o, ct);
            await orderRepo.SaveAsync(o, ct);
        });
        await Task.WhenAll(validationTasks);

        // 4) Start processing every validated order 
        var processor = new OrderProcessorV2(pricer);

        var validatedOrders = orders.Where(o => o.Status == OrderStatus.Validated).ToList();
        var tasks = new List<Task<bool>>();
        foreach (var o in validatedOrders)
            tasks.Add(processor.ProcessValidatedOrderAsync(o, ct));

        // 5) Status of task[0]
        Console.WriteLine($"task[0] status right after creation: {tasks[0].Status}");

        // 6) After 1 second:
        await Task.Delay(1000, ct);
        for (int i = 0; i < tasks.Count; i++)
            Console.WriteLine($"Order #{validatedOrders[i].Id} -> {tasks[i].Status}");

        //  7) Await each task 
        foreach (var (order, task) in validatedOrders.Zip(tasks))
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Order #{order.Id} failed. Task status: {task.Status}");

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                await processor.ReleaseOrderStockAsync(order, ct);
                await orderRepo.SaveAsync(order, ct);
            }
        }

        // 8) Final status of every task and total time 
        Console.WriteLine("---- Final task statuses ----");
        foreach (var (order, task) in validatedOrders.Zip(tasks))
            Console.WriteLine($"Order #{order.Id} -> {task.Status} ({order.Status})");

        total.Stop();
        Console.WriteLine($"Total time: {total.Elapsed.TotalSeconds:F1} seconds");

        //  9) Read orders.json
        var all = await orderRepo.GetAllAsync(ct);
        Console.WriteLine($"orders.json now contains {all.Count} records (expected 20)");
        Console.WriteLine($"Completed: {all.Count(o => o.Status == OrderStatus.Completed)} (expected 13)");
        Console.WriteLine($"Rejected:  {all.Count(o => o.Status == OrderStatus.Rejected)} (expected 6)");
        Console.WriteLine($"Failed:    {all.Count(o => o.Status == OrderStatus.Failed)} (expected 1)");

        // 10) Report 
        var reportGenerator = new ReportGenerator();
        Task<Report> reportTask = reportGenerator.GenerateAsync(ct);   // no await yet

        Console.WriteLine("Generating report...");

        Report report = await reportTask;

        Console.WriteLine("===== Report =====");
        Console.WriteLine($"Total orders:      {report.TotalOrders}");
        Console.WriteLine($"Completed:         {report.CompletedCount}");
        Console.WriteLine($"Rejected:          {report.RejectedCount}");
        Console.WriteLine($"Failed:            {report.FailedCount}");
        Console.WriteLine($"PaymentFailed:     {report.PaymentFailedCount}");
        Console.WriteLine($"Total revenue:     {report.TotalRevenue}");
        Console.WriteLine($"Average order:     {report.AverageOrderValue}");
        Console.WriteLine($"Top selling:       {report.TopSellingProduct} ({report.TopSellingQuantity} units)");
    }
}