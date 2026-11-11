
## Q1 - What is System.Threading.Monitor used for?

Source:
- https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor?view=net-10.0

`Monitor` is a class in the `System.Threading` namespace that provides a mechanism for synchronizing access to objects by multiple threads
 It preventing race conditions and ensuring thread safety.
`Monitor` provides mutual exclusion on an object, just like `lock`, which is
actually shorthand for `Monitor.Enter` and `Monitor.Exit` in a try/finally.
Using `Monitor` directly gives more control: `TryEnter` lets a thread try to
get the lock with a timeout instead of waiting forever, and `Wait`, `Pulse`
and `PulseAll` let threads wait for a condition and signal each other.

## Q2 - Lock statement in C#

Source:
- https://dev.to/rasulhsn/under-the-hood-of-thread-synchronization-with-lock-3h50

The C# lock statement is compiled into calls to System.Threading.Monitor.Enter() and Monitor.Exit(),
with a try/finally block to guarantee that the lock is released even if an exception occurs.


## Q3- Monitor vs lock

`Monitor` is the .NET class that provides mutual exclusion: it lets only one
thread at a time enter a critical section guarded by an object. The C# `lock`
statement is syntactic sugar for `Monitor`, with automatic exception handling
(the lock is released even if an exception is thrown).

Monitor 

```csharp
Monitor.Enter(lockObject);
try
{
    // critical section code
}
finally
{
    Monitor.Exit(lockObject);
}
```

lock 

```csharp
lock (lockObject)
{
    // critical section code
}
```

### lock vs Monitor

So `lock` is enough for basic synchronization, and `Monitor` is for advanced
threading scenarios that need a timeout or thread signaling.

Both implementations achieve the same goal: protecting shared data from race conditions.
The `lock` statement is generally preferred for ordinary synchronization because it is simpler and less error-prone.
`Monitor` is useful when more advanced synchronization control is required.

Source:
- https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor?view=net-10.0

## Q4 - What is the difference between Monitor.Wait and Monitor.Pulse?
Sources:
- https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor?view=net-10.0

A plain `lock` only waits until the lock is free. I use `Monitor` directly when I need more control.

- **Monitor.TryEnter:** tries to take the lock without waiting forever. If the lock is busy, it returns `false` (or gives up after a timeout), so the thread can skip the work and avoid freezing.
- **Monitor.Wait:** releases the lock and puts the thread to sleep until another thread signals it, so it can wait for a condition (like "the queue is not empty") without wasting CPU.
- **Monitor.Pulse:** wakes up one thread that is waiting. `PulseAll` wakes all of them.


## Q5:write same critical secotion  : once with lock, once with Monitor.Enter / Monitor.Exit.



Both approaches provide mutual exclusion, ensuring that only one thread at a time can execute the critical section.


```csharp
private static readonly object _locker = new();
private static int _counter = 0;

public static void IncrementWithLock()
{
    lock (_locker)
    {
        _counter++;
    }
}
```

## 2. Using Monitor

```csharp
using System.Threading;

private static readonly object _locker = new();
private static int _counter = 0;

public static void IncrementWithMonitor()
{
    bool lockTaken = false;

    try
    {
        Monitor.Enter(_locker, ref lockTaken);

        _counter++;
    }
    finally
    {
        if (lockTaken)
        {
            Monitor.Exit(_locker);
        }
    }
}
```



https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor?view=net-10.0&utm_source=chatgpt.com
