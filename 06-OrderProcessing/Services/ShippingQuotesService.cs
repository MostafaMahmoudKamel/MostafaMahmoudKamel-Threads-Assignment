using _06_OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Services;

public class ShippingQuotesService
{
    private readonly List<IShippingCarrier> _carriers;

    public ShippingQuotesService(IEnumerable<IShippingCarrier> carriers)
    {
        _carriers = new List<IShippingCarrier>(carriers);
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(Order order, CancellationToken ct = default)
    {
        // R4: ميزانية زمنية 1.2 ثانية لكل عملية شحن
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1.2));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var cancellationToken = linkedCts.Token;

        var carrierCtsList = new List<CancellationTokenSource>();
        var runningTasks = new List<Task<(IShippingCarrier Carrier, decimal Price, double Elapsed)>>();

        var stopwatch = Stopwatch.StartNew();

        foreach (var carrier in _carriers)
        {
            var individualCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            carrierCtsList.Add(individualCts);

            var task = Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                decimal price = await carrier.GetQuoteAsync(order, individualCts.Token);
                sw.Stop();
                return (Carrier: carrier, Price: price, Elapsed: sw.Elapsed.TotalMilliseconds);
            }, individualCts.Token);

            runningTasks.Add(task);
        }

        var exceptions = new List<Exception>();

        while (runningTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(runningTasks);
            runningTasks.Remove(finishedTask);

            try
            {
                var result = await finishedTask;
                stopwatch.Stop();

                foreach (var cts in carrierCtsList)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                    }
                }
                
                foreach (var carrier in _carriers)
                {
                    if (carrier.Name != result.Carrier.Name)
                    {
                        Console.WriteLine($"[{carrier.Name}] cancelled.");
                    }
                }

                return new ShippingQuote(result.Carrier.Name, result.Price, result.Elapsed);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        //if all carriess failed
        if (exceptions.Count > 0 && timeoutCts.IsCancellationRequested && stopwatch.Elapsed.TotalSeconds >= 1.2)
        {
            return new ShippingQuote("Flat rate", 25.00m, stopwatch.Elapsed.TotalMilliseconds);
        }

        throw new Exception("All carriers failed");
    }
}