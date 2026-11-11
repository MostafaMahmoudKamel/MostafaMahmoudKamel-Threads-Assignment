using _06_OrderProcessing.Models;
using _06_OrderProcessing.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Runners;

public class Phase4Runner
{
    public static async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("     TASK 10.4: FRAUD SCORING & EMAIL RE-SEND     ");
        Console.WriteLine("==================================================\n");

        // ==========================================================
        // A — Fraud scoring (CPU-bound) [Requirements R1 - R5]
        // ==========================================================
        Console.WriteLine("--- [A] Fraud Scoring (10,000 Orders) ---");

        // R1: Sequential loop timing
        var swSeq = Stopwatch.StartNew();
        var flaggedSequential = new List<int>();
        for (int id = 1; id <= 10_000; id++)
        {
            if (Score(id) >= 90)
            {
                flaggedSequential.Add(id);
            }
        }
        swSeq.Stop();

        // R2 & R4: Parallel processing using built-in library limits honoring CancellationToken
        var swPar = Stopwatch.StartNew();
        var flaggedConcurrentBag = new ConcurrentBag<int>();
        var distinctThreads = new ConcurrentDictionary<int, bool>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount, // R2
            CancellationToken = ct // R4
        };

        try
        {
            Parallel.For(1, 10_001, parallelOptions, id =>
            {
                distinctThreads.TryAdd(Environment.CurrentManagedThreadId, true);

                if (Score(id) >= 90)
                {
                    flaggedConcurrentBag.Add(id);
                }
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Fraud scoring parallel execution was cancelled.");
        }
        swPar.Stop();

        var flaggedParallel = flaggedConcurrentBag.OrderBy(x => x).ToList();
        double speedUp = (double)swSeq.ElapsedMilliseconds / Math.Max(1, swPar.ElapsedMilliseconds);

        Console.WriteLine($"Flagged count: {flaggedParallel.Count} (First five: {string.Join(", ", flaggedParallel.Take(5))})"); // R3, R5
        Console.WriteLine($"Sequential time: {swSeq.ElapsedMilliseconds} ms"); // R5
        Console.WriteLine($"Parallel time:   {swPar.ElapsedMilliseconds} ms"); // R5
        Console.WriteLine($"Speed-up:        {speedUp:F2}x"); // R5
        Console.WriteLine($"Distinct threads used: {distinctThreads.Count}"); // R5
        Console.WriteLine($"Environment.ProcessorCount: {Environment.ProcessorCount}\n"); // R5


        // ==========================================================
        // B — Email re-send (I/O-bound) [Requirements R1 - R4]
        // ==========================================================
        Console.WriteLine("--- [B] Email Re-send (30 Test Emails, Max 4 Concurrent) ---");

        var emailService = new EmailService(); // خدمة الإيميلات في المشروع
        var testOrderIds = Enumerable.Range(1001, 30).ToList();

        int currentConcurrentSends = 0;
        int maxObservedConcurrent = 0;
        object lockObj = new();

        var swEmails = Stopwatch.StartNew();

        var emailParallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4, // R1
            CancellationToken = ct // R3
        };

        try
        {
            await Parallel.ForEachAsync(testOrderIds, emailParallelOptions, async (orderId, token) =>
            {
                lock (lockObj)
                {
                    currentConcurrentSends++;
                    if (currentConcurrentSends > maxObservedConcurrent)
                    {
                        maxObservedConcurrent = currentConcurrentSends;
                    }
                }

                try
                {
                    // تجهيز الأوردر والعميل الوهمي واستدعاء الميثود الحقيقية لتخزين الإيميل في emails.json
                    var dummyOrder = new Order { Id = orderId, TotalAmount = 200.0m };
                    var dummyCustomer = new Customer { Name = $"User {orderId}", Email = $"user{orderId}@example.com" };

                    await emailService.SendConfirmationAsync(dummyOrder, dummyCustomer, token);
                }
                finally
                {
                    lock (lockObj)
                    {
                        currentConcurrentSends--;
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Email re-send batch was cancelled.");
        }

        swEmails.Stop();

        Console.WriteLine($"Highest number of concurrent sends: {maxObservedConcurrent} (Expected: exactly 4)"); // R2
        Console.WriteLine($"Total time for 30 emails: {swEmails.ElapsedMilliseconds} ms (About 8 seconds due to batching 4 by 4)"); // R4
        Console.WriteLine("=== Task 10.4 Completed Successfully ===");
    }

    // دالة الـ Score الصحيحة والمعدلة
    private static int Score(int orderId)
    {
        long h = orderId;
        for (int i = 0; i < 100_000; i++)
        {
            h = (h * 31 + i) % 1_000_000_007;
        }
        return (int)(h % 100);
    }
}


//using _06_OrderProcessing.Models;
//using _06_OrderProcessing.Services;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace _06_OrderProcessing.Runners;

//public class FraudScoringAndEmailBatchRunner
//{
//    public static async Task ExecuteAsync(CancellationToken ct = default)
//    {
//        Console.WriteLine("==================================================");
//        Console.WriteLine("     TASK 10.4: FRAUD SCORING & EMAIL RE-SEND     ");
//        Console.WriteLine("==================================================\n");

//        // ==========================================================
//        // A — Fraud scoring (CPU-bound) [Requirements R1 - R5]
//        // ==========================================================
//        Console.WriteLine("--- [A] Fraud Scoring (10,000 Orders) ---");

//        // R1: Sequential loop timing
//        var swSeq = Stopwatch.StartNew();
//        var flaggedSequential = new List<int>();
//        for (int id = 1; id <= 10_000; id++)
//        {
//            if (Score(id) >= 90)
//            {
//                flaggedSequential.Add(id);
//            }
//        }
//        swSeq.Stop();

//        // R2 & R4: Parallel processing using built-in library limits honoring CancellationToken
//        var swPar = Stopwatch.StartNew();
//        var flaggedConcurrentBag = new ConcurrentBag<int>();
//        var distinctThreads = new ConcurrentDictionary<int, bool>();

//        var parallelOptions = new ParallelOptions
//        {
//            MaxDegreeOfParallelism = Environment.ProcessorCount, // R2
//            CancellationToken = ct // R4
//        };

//        try
//        {
//            Parallel.For(1, 10_001, parallelOptions, id =>
//            {
//                distinctThreads.TryAdd(Environment.CurrentManagedThreadId, true);

//                if (Score(id) >= 90)
//                {
//                    flaggedConcurrentBag.Add(id);
//                }
//            });
//        }
//        catch (OperationCanceledException)
//        {
//            Console.WriteLine("Fraud scoring parallel execution was cancelled.");
//        }
//        swPar.Stop();

//        var flaggedParallel = flaggedConcurrentBag.OrderBy(x => x).ToList();
//        double speedUp = (double)swSeq.ElapsedMilliseconds / Math.Max(1, swPar.ElapsedMilliseconds);

//        Console.WriteLine($"Flagged count: {flaggedParallel.Count} (First five: {string.Join(", ", flaggedParallel.Take(5))})"); // R3, R5
//        Console.WriteLine($"Sequential time: {swSeq.ElapsedMilliseconds} ms"); // R5
//        Console.WriteLine($"Parallel time:   {swPar.ElapsedMilliseconds} ms"); // R5
//        Console.WriteLine($"Speed-up:        {speedUp:F2}x"); // R5
//        Console.WriteLine($"Distinct threads used: {distinctThreads.Count}"); // R5
//        Console.WriteLine($"Environment.ProcessorCount: {Environment.ProcessorCount}\n"); // R5


//        Console.WriteLine("--- [B] Email Re-send (30 Test Emails, Max 4 Concurrent) ---");

//        var emailService = new EmailService(); // خدمة الإيميلات في المشروع
//        var testOrderIds = Enumerable.Range(1001, 30).ToList();

//        int currentConcurrentSends = 0;
//        int maxObservedConcurrent = 0;
//        object lockObj = new();

//        var swEmails = Stopwatch.StartNew();

//        var emailParallelOptions = new ParallelOptions
//        {
//            MaxDegreeOfParallelism = 4, // R1
//            CancellationToken = ct // R3
//        };

//        try
//        {
//            await Parallel.ForEachAsync(testOrderIds, emailParallelOptions, async (orderId, token) =>
//            {
//                lock (lockObj)
//                {
//                    currentConcurrentSends++;
//                    if (currentConcurrentSends > maxObservedConcurrent)
//                    {
//                        maxObservedConcurrent = currentConcurrentSends;
//                    }
//                }

//                try
//                {
//                    await Task.Delay(1000, token); // كل إيميل ياخد حوالي ثانية

//                }
//                finally
//                {
//                    lock (lockObj)
//                    {
//                        currentConcurrentSends--;
//                    }
//                }
//            });
//        }
//        catch (OperationCanceledException)
//        {
//            Console.WriteLine("Email re-send batch was cancelled.");
//        }

//        swEmails.Stop();

//        Console.WriteLine($"Highest number of concurrent sends: {maxObservedConcurrent} (Expected: exactly 4)"); // R2
//        Console.WriteLine($"Total time for 30 emails: {swEmails.ElapsedMilliseconds} ms (About 8 seconds due to batching 4 by 4)"); // R4
//        Console.WriteLine("=== Task 10.4 Completed Successfully ===");
//    }

//    // Score
//    private static int Score(int orderId)
//    {
//        long h = orderId;
//        for (int i = 0; i < 100_000; i++)
//        {
//            h = (h * 31 + i) % 1_000_000_007;
//        }
//        return (int)(h % 100);
//    }
//}