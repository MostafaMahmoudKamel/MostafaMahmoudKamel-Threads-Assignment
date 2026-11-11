namespace _06_OrderProcessing.Services;

using global::_06_OrderProcessing.Models;
using global::_06_OrderProcessing.Repositories.AsyncRepositories;
using System.Text.Json;


public class ReportGenerator
{
    private readonly OrderRepository _orderRepository = new OrderRepository();
    private readonly ProductRepository _productRepository = new ProductRepository();

    public async Task<Report> GenerateAsync(CancellationToken ct)
    {
        // 1) load all orders
        var orders = await _orderRepository.GetAllAsync(ct);
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        var report = new Report
        {
            TotalOrders = orders.Count,
            CompletedCount = completed.Count,
            RejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected),
            FailedCount = orders.Count(o => o.Status == OrderStatus.Failed),
            PaymentFailedCount = orders.Count(o => o.Status == OrderStatus.PaymentFailed),
            GeneratedAt = DateTime.Now
        };

        // 2) revenue Completed
        report.TotalRevenue = completed.Sum(o => o.TotalAmount);
        report.AverageOrderValue = completed.Count == 0
            ? 0
            : Math.Round(report.TotalRevenue / completed.Count, 2, MidpointRounding.AwayFromZero);

        // 3) top selling product 
        var top = completed
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Units)
            .ThenBy(x => x.ProductId)
            .FirstOrDefault();

        if (top != null)
        {
            var product = await _productRepository.GetByIdAsync(top.ProductId, ct);
            report.TopSellingProduct = product!.Name;
            report.TopSellingQuantity = top.Units;
        }

        // 4) write reports
        Directory.CreateDirectory("Data/reports");
        string path = $"Data/reports/report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, ct);

        return report;
    }
}

