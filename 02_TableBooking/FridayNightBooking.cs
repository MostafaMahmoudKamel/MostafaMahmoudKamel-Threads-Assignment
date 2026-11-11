namespace TableBooking;

public sealed class FridayNightBooking
{
    public int TablesLeft { get; private set; } = 5;
    public int Booked { get; private set; }


    readonly object _object = new();

    public void Book(string customerName)
    {
        //Race condition happend so i should lock or using Interlocked or sempophorslim 
        //i solve using lock statement because i found condition 

        //lock (_object)//same object lock for all threads
        //{
        //    if (TablesLeft <= 0)
        //        return;

        //    Thread.Sleep(20);

        //    TablesLeft--;
        //    Booked++;
        //    Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
        //}

        bool taken = false;
        try
        {
            Monitor.Enter(_object, ref taken);
            // code
            if (TablesLeft <= 0)
                return;

            Thread.Sleep(20);

            TablesLeft--;
            Booked++;
            Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
        }
        finally
        {
            if (taken) Monitor.Exit(_object);


        }
    }
}
