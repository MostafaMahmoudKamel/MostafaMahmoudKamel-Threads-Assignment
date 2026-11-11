

## R1 - Failures I saw


- An exception: `ArgumentException: An item with the same key has already been added. Key: koshary-bowl`
- A wrong view count: Views expected=50000 , and the value is differenct for each running
- Emails sent twice or lost, or `InvalidOperationException`: `Email jobs placed=20, unique sent=18, total sends=18`


## R2 - The fix
i change from dictionary and queue to concurrent dictionary and concurrent queue 
i change method from Add to AddOrUpdate and from Dequeue to TryDequeue

After the fix:
Views expected=50000, actual=50000
Email jobs placed=20, unique sent=20, total sends=20


## R3 - Why a safer collection does not make the business rule safe

A thread-safe collection only protects each single operation.
It does not protect a rule that needs several steps. 
If my code checks something and then acts on it, 
another thread can change the data between the two steps,
even if each call is safe on its own.

A rule like "each email is sent exactly once" or "count every view" depends on the whole logic being correct.
In my first fix I wrote `AddOrUpdate(product, 0, ...)` by mistake, and the collection was fully safe,
but the first view was lost and the result was 49999. So the collection being safe is not enough.
The rule itself must be written as one safe step, or protected with a `lock`.