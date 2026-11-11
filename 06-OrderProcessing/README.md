## Phase 1: sync vs async

**1. Why are the sync and async times almost the same? Why is async slower? How can it be faster?**

The times are almost the same because the async version runs sequentially:
each order is awaited before the next one starts, so the delays add up just like
in the sync version. The async run is slightly slower because it also saves each
order to orders.json. To make it faster, start all the validations together and
wait for them with `Task.WhenAll`.

**2. Is the thread ID after an `await` the same as before it?**

No. The thread ID after an `await` is not always the same as before it.
`await Task.Delay` does not block the thread. It releases the thread back to the
thread pool while the delay runs, and the code continues later on whichever
thread is free.

```
[03:43:19.340] [Thread 10] OrderRepository -> Saving Order #15...
[03:43:19.747] [Thread 14] OrderRepository -> Committing...
[03:43:19.952] [Thread 14] OrderRepository -> Order #15 saved
[03:43:19.952] [Thread 14] CustomerRepository -> Opening connection...
[03:43:20.156] [Thread 14] CustomerRepository -> Executing query for Customer #6...
[03:43:20.610] [Thread 12] CustomerRepository -> Customer #6 found (Nour Khaled)
```

**3. What does the thread do while `Task.Delay()` is waiting? And while `Thread.Sleep()` is waiting?**

- `Thread.Sleep`: the thread is blocked. It stays reserved and can't do anything
  else until the sleep finishes.
- `Task.Delay`: the thread is released back to the ThreadPool and can do other
  work. A timer continues the code after the delay finishes.

**4. Why should a repository method return `Task<T>` instead of `T`?**

Repository methods do slow work (database delays and file reading). If the method
returned `T` directly, it would be synchronous and the caller would be blocked
until the work finished. Returning `Task<T>` lets the caller use `await`, so the
thread is freed while waiting.

**5. What is the difference between `File.ReadAllText` and `File.ReadAllTextAsync`?**

`File.ReadAllText` blocks the thread until the whole file is read.
`File.ReadAllTextAsync` returns a `Task<string>` and is used with `await`.

Does the async version occupy a thread while the disk is reading?
No. The thread is only used to start the request and to continue the code after
it finishes. While the disk is reading, no thread is blocked or waiting.

---

## Phase 2 — Concurrent Processing

### Race condition (Task 8.3)
- Unsafe reservation: both orders #10 and #11 got the last keyboard (True / True).
  Each one read stock = 1 before the other one wrote, so both succeeded.
- Safe reservation (with a lock): one order got it, the other was refused.
  Stock in file = 0.

### Pricing: concurrent vs sequential (Task 8.2) — order #1
| Method | Time |
|---|---|
| Concurrent (discount + shipping together) | 4014 ms |
| Sequential | 4979 ms |

Concurrent is faster because the discount and shipping calls run at the same time,
so we wait only for the slower one instead of the sum of both.

### Processing all orders (Task 8.4)
| Phase | Time |
|---|---|
| Phase 1 (one order after another) | XX.X seconds |
| Phase 2 (validate + process concurrently) | 15.9 seconds |

Phase 2 handles all orders at the same time instead of waiting for each order to
finish.

Note: Phase 2 does more work per order (reserve stock, pricing, save) than
Phase 1 (validation only), so the two times are not a one-to-one comparison.

### Result
- orders.json contains exactly 20 records (no lost writes).
- 13 Completed, 6 Rejected, 1 Failed (order #7: Shipping provider unavailable).
- Order #11 got the last keyboard and #10 was rejected (both outcomes are valid).

**Why `Task.Run()` fits the invoice:**
The invoice loop is CPU-bound, so a thread is busy working the whole time.
`Task.Run` moves that work to another thread, and the calling thread stays free.

**Why it does not fit the database calls:**
Database calls are I/O-bound, so most of the time is spent waiting. `await` already frees the thread while waiting, so `Task.Run` would only add an extra thread for no benefit.

## phase2 Questions

1.  What is the difference between starting a task and awaiting it?
- start task : begain method and return task 
- await : pasue method until the mthod is finished 

2. Does every Task create a new thread? Does `Task.Delay()` use a thread while waiting?**
-No 
_ thread is used while waiting. A thread-pool thread is needed only to run the code before and after the wait.

**3. What happens if you call `.Result` or `.Wait()` on an unfinished task instead of `await`? Why is it dangerous?**
-result or wait is blocking 
- await is non- blocking the thread 

**4. What happens to an exception thrown inside a task that is never awaited?**
- program still running and error may lost becasuse no-observes it 

**5. When should you use `Task.Run()`, and when should you avoid it?**
- use it in cpu-Bound 
- avoid in IO-Bound like calling db or api

**6. What is the difference between `Task` and `Task<T>`?**
- Task: return no value 
- Task<T> : return value 

**7. Why can a `lock` block not contain an `await`?**
 `lock` is owned by one specific thread, and it must be released by that same
thread. After an `await`, the code may continue on a different thread, so it
could not release the lock correctly, and other code could be blocked or
deadlocked. The compiler does not allow it. `SemaphoreSlim`

**8. Why did the unprotected stock reservation oversell, and how did you prevent it?**
rders #10 and #11 both read the stock (1) before either one wrote the new value
(0), so both thought the keyboard was available and both succeeded
-I fixed it with a
`SemaphoreSlim(1, 1)` in `ReserveStockAsync` and `ReleaseStockAsync`

## phase 3 questions

1. **Does async make one order's workflow faster?**
   - No, because individual delays (`Task.Delay` vs `Thread.Sleep`) and I/O wait times remain identical for a single sequential workflow.

2. **Why does async scale better for many orders?**
   - It releases threads back to the ThreadPool during I/O wait times and delays instead of blocking OS threads, avoiding thread starvation and massive context switching.

3. **What happens to the sync version with 1,000 orders?**
   - It will exhaust the thread pool, cause massive context switching, consume excessive memory, and potentially lead to system gridlock or Out-Of-Memory errors due to blocked OS threads.

4. **Why is async essential for apps that call databases, files and APIs?**
   - Because I/O bound operations spend most of their time waiting on external resources, and async ensures maximum resource efficiency by keeping threads unblocked and available during these wait periods.


   ## Task 9.7 — Fire-and-forget investigation

1. **Find the problem in this code:**
   - The `_emailService.SendConfirmationAsync` method is called without an `await`. Because the outer method is marked `async Task`, it will execute fire-and-forget style. If an exception occurs inside the unawaited task, it becomes an unobserved exception that can crash the application, and the caller has no way of knowing when or if the email operation actually finishes.

2. **Show what happens if the email is still being sent when the method returns, or when the console app exits:**
   - Since it's a console application, as soon as `ProcessOrderAsync` finishes and returns to `Main`, the application might exit immediately. If the app exits, the background thread pool task running the email will be abruptly terminated mid-execution, and the email will never be sent.

3. **Show what happens if the email operation throws an exception:**
   - It throws an **Unobserved Task Exception** (or unhandled exception on the thread pool depending on the runtime context), which can crash the entire process abruptly because there is no `await` to catch or capture the exception inside the synchronization context.

4. **Fix the implementation & explanations:**
   - **The Fix:** Simply add `await` before `_emailService.SendConfirmationAsync(order, customer);` so the method waits for the email to complete safely and handles exceptions properly.
   
   - **When is `await` necessary?** 
     - Whenever you need the result of a task, or when subsequent code depends on its completion, or to ensure exceptions are caught correctly.
   - **When is fire-and-forget acceptable (if ever)?** 
     - Only in very rare scenarios like non-critical telemetry logging or background metrics where failure or completion doesn't impact system state or user experience (and even then, it must wrap exceptions in a try-catch block).
   - **Why is `async void` dangerous?** 
     - Because exceptions thrown inside an `async void` method cannot be caught by a caller using `try-catch` (they crash the process directly), and there is no `Task` object to track completion.
   - **Why is it acceptable for event handlers?** 
     - Because UI frameworks (like WinForms, WPF, or ASP.NET event hooks) expect event signatures to be `void` (e.g., `Button_Click(object sender, EventArgs e)`), so `async void` is allowed solely to permit `await` inside event handling, but they must contain internal `try-catch` blocks to swallow and log exceptions safely.

     1. For each of Tasks 10.1–10.4: which tool did you use, and why can't "start, then await one by one" meet the requirement?
Task 10.1 (Live Checkout Progress):

Tool Used: Task.WhenAll (or initiating all tasks concurrently without immediate sequential awaits).

Why "start, then await one by one" fails: Awaiting each checkout task sequentially forces execution back into a single-threaded blocking flow. It loses concurrency, causing the total execution time to equal the sum of all individual checkout durations rather than running them in parallel.

Task 10.2 (Customer Dashboard Aggregator):

Tool Used: Task.WhenAll.

Why "start, then await one by one" fails: Fetching data endpoints (profile, orders, rewards) one after another introduces unnecessary network latency. Task.WhenAll fires all fetch requests at the same instant, so the total wait time is bounded only by the slowest single request.

Task 10.3 (Fastest Shipping Quote Competition):

Tool Used: Task.WhenAny.

Why "start, then await one by one" fails: The core goal is a race condition where we accept the first carrier to respond. If we await carriers sequentially, we are forced to wait for carrier #1 to complete—even if carrier #2 or #3 responded much faster.

Task 10.4 (Fraud Scoring & Email Batching):

Tool Used: Parallel.For for CPU-bound scoring (Part A), and Parallel.ForEachAsync with MaxDegreeOfParallelism = 4 for I/O-bound email batching (Part B).

Why "start, then await one by one" fails: For CPU-bound work, sequential processing uses only a single CPU core, ignoring available hardware parallelism. For I/O-bound email batching, awaiting sequentially processes 1 email at a time instead of controlled concurrency throttling (exactly 4 at a time).