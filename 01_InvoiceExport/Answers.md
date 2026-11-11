## Invoice Export 
-- R2:

**Everything is blocking.** `Thread.Sleep` in `OrderDatabase`, `CustomerFileStore`
   and `InvoiceFileWriter`, plus the synchronous `File.ReadAllText` and
   `File.WriteAllText`, freeze the calling thread while it waits. The thread does
   no useful work during those waits.

Each order takes about 1000 ms (400 ms load order Pending + 300 ms read from  customer +
   300 ms write invoice)
   . Order-2 don't depend on  order1, but it still blocking and waing them for to Finsih

 Total Time = 800 ms ( pending before ) + 5 x 1000  = about 5.8 s


--R4
R4 – Changes and Why

1. What I Changed
I converted the invoice export workflow from synchronous (Blocking ) to Asynchronous (Non-Blocking) execution. 
i replaced all sync method to async method and used await to wait for their completion. using Task.Delay();


Why?

Asynchronous file reading avoids blocking the calling 
and the synchronous `Thread.Sleep` take much time to wait for the completion of the task. [5.9 ms ]
and task.Delay() take only [1.9 ms]
using WhenAll I start all five order  before waiting for all of them to complete. and i run them sequentially.


2. Console Output Before
Export finished in  5.9 s

3. Console Output After
 Export finished in  1.9 s

 Becouse PendingOrdersAsync take [800 ms ] + [300 ms ] for reading customer profile + [300 ms ] for writing invoice + [400 ms ] getOrderBYId = 1.9 s


The updated asynchronous implementation starts all five order exports before waiting for all of them.


