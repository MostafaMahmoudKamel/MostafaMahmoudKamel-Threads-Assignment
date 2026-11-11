## R1 - Recording 
	40 customers booked tables at the same time
## R2 - What is wrong and why

This is a race condition 
There are 40 customers and each
one runs in its own thread
All of them can read `TablesLeft` and pass the
check `TablesLeft <= 0` at the same time, because the value is still 5 for
all of them. 
`Thread.Sleep(20)`making  every thread inside the method long enough
to pass .
After the sleep, they all decrement`TablesLeft` and increment `Booked`
So more tables are booked than exist,
and `TablesLeft` goes below zero. Also, `TablesLeft--` and `Booked++` are
read-modify-write operations, so updates can be lost.

## R3 & R4 - The fix and Explanation
i solved using `lock` to same object for all threads



