using System.Diagnostics;

namespace InvoiceExport;

public sealed class InvoiceExportService
{
    readonly OrderDatabase _db;
    readonly CustomerFileStore _customers;
    readonly InvoiceFileWriter _invoices;

    public InvoiceExportService(OrderDatabase db, CustomerFileStore customers, InvoiceFileWriter invoices)
    {
        _db = db;
        _customers = customers;
        _invoices = invoices;
    }

    public async Task ExportAllPendingAsync()
    {
        var watch = Stopwatch.StartNew();

        // 1. Get all pending orders
        List<Order> orders = await _db.GetPendingOrdersAsync();//800ms pending orders


        // 2. Start exporting all orders
        var tasks = new List<Task<string>>();

        foreach (Order order in orders)
            tasks.Add(ExportOneAsync(order));

        string[] paths = await Task.WhenAll(tasks);
        foreach (string path in paths)
        {
            Console.WriteLine($"Wrote {path}");
        }

        //ExportOneAsync(order)

        Console.WriteLine($"Export finished in {watch.Elapsed.TotalSeconds:F1} s");
    }


   public  async Task<string> ExportOneAsync(Order order)
    {

        Order fresh = await _db.GetOrderByIdAsync(order.Id)  ?? throw new InvalidOperationException($"Order {order.Id} missing");
        CustomerProfile customer = await _customers.ReadProfileAsync(fresh.CustomerId);
        return await _invoices.WriteInvoiceAsync(fresh, customer);

    }
}