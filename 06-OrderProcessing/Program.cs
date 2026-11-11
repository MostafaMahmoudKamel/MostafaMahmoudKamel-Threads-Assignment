using _06_OrderProcessing.Models;
using _06_OrderProcessing.Runners;
using _06_OrderProcessing.Services;
using _06_OrderProcessing.Shared;
using System.Diagnostics;

namespace _06_OrderProcessing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                await Phase1Runner.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Run cancelled");
            }

            //phase 2 

            //await DataSeeder.ResetAsync();
            //await RaceConditionDemo.RunAsync(cts.Token);

            //await Phase2Runner.RunAsync(CancellationToken.None);


            //pase 3
            //Console.WriteLine("=== Running Sync vs Async Comparison (Task 9.6) ===");

            //// --- 1. Sync Run (أول 10 أوردرات) ---
            //await DataSeeder.ResetAsync();
            //var ordersSync = (await new JsonFileStore().ReadAllAsync<Order>(DataFiles.IncomingOrders))
            //                 .Where(o => o.Id <= 10).ToList();

            //var syncProcessor = new SyncOrderProcessor();
            //var threads = new List<Thread>();
            //var swSync = Stopwatch.StartNew();

            //foreach (var order in ordersSync)
            //{
            //    var t = new Thread(() => syncProcessor.ProcessOrder(order));
            //    threads.Add(t);
            //    t.Start();
            //}

            //foreach (var t in threads) t.Join();
            //swSync.Stop();
            //Console.WriteLine($"[Sync] Time: {swSync.ElapsedMilliseconds} ms | Threads used: {ThreadPool.ThreadCount}");

            //// --- 2. Async Run (أول 10 أوردرات) ---
            //await DataSeeder.ResetAsync();
            //var ordersAsync = (await new JsonFileStore().ReadAllAsync<Order>(DataFiles.IncomingOrders))
            //                  .Where(o => o.Id <= 10).ToList();

            //var swAsync = Stopwatch.StartNew();
            //// تشغيل الـ Async tasks مع بعض
            //var tasks = ordersAsync.Select(o => Task.Run(() => new SyncOrderProcessor().ProcessOrder(o))); // أو الـ OrderProcessor الحقيقي لو جاهز عندك
            //await Task.WhenAll(tasks);
            //swAsync.Stop();

            //Console.WriteLine($"[Async] Time: {swAsync.ElapsedMilliseconds} ms");
            //Console.WriteLine("=== Done Successfully! ===");



            //phase 4 

            await Phase4Runner.RunAsync(cts.Token);
        }




    }
}
