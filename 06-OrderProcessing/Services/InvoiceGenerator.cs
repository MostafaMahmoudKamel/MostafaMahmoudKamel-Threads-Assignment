using _06_OrderProcessing.Models;

namespace _06_OrderProcessing.Services;



public class InvoiceGenerator
{
    public async Task GenerateAsync(Order order, CancellationToken ct)
    {
        // CPU-bound work: moved to a thread-pool thread with Task.Run (used once, as the task requires)
        await Task.Run(() =>
        {
            // simulate rendering a PDF: a pure CPU loop of 20,000,000 iterations
            long dummy = 0;
            for (int i = 0; i < 20_000_000; i++)
            {
                dummy += i % 7;
            }

            // keep the result used so the compiler doesn't optimize the loop away
            string content =
                $"Invoice for order #{order.Id}\n" +
                $"SubTotal: {order.SubTotal}\n" +
                $"Discount: {order.Discount}\n" +
                $"Shipping: {order.ShippingCost}\n" +
                $"Total:    {order.TotalAmount}\n" +
                $"(render checksum: {dummy})\n";

            Directory.CreateDirectory("Data/invoices");
            File.WriteAllText($"Data/invoices/invoice_{order.Id}.txt", content);
        },ct);
    }
}
