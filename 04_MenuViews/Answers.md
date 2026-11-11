## Why Do the Numbers Disagree?

Expected: 400000

problem: race condition
The actual result can be smaller than because this operation isn't atomic  and it can be 3 operations: read, increment, write.
If two threads read the same value before either writes it back, one of the increments will be lost.
Example: if two threads read the value 100, both increment it to 101, and then both write back 101, the final value will be 101 instead of 102.

## right solution is 
to use Interlocked.Increment(ref counter.Views) which is atomic and thread-safe.
This ensures that the increment operation is performed without interference from other threads,
preventing race conditions and ensuring the correct final count.

## When would I protect this kind of update differently?

`Interlocked.Increment` works here because the update is one simple operation
on one single number. I 


