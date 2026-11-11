using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Repositories.AsyncRepositories;

//saving order in orders.json
public class OrderRepository
{
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();

    private readonly HashSet<int> _failSaveForOrderIds;

    public OrderRepository(IEnumerable<int>? failSaveForOrderIds = null)
    {
        _failSaveForOrderIds = new HashSet<int>(failSaveForOrderIds ?? Array.Empty<int>());
    }

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        Log.Info("OrderRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", "Beginning transaction...");
        await Task.Delay(100, ct);

        Log.Info("OrderRepository", $"Saving Order #{order.Id}...");
        await Task.Delay(400, ct);

        //fail =>throw exception
        if (_failSaveForOrderIds.Contains(order.Id) && order.Status == OrderStatus.Completed)
        {
            Log.Error("OrderRepository", $"Order #{order.Id}: connection lost during commit");
            throw new DatabaseWriteException("Connection lost during commit");
        }

        await _jsonFileStore.UpdateAsync<Order>(DataFiles.Orders, orders =>
        {
            var index = orders.FindIndex(o => o.Id == order.Id);

            if (index >= 0) orders[index] = order;   // موجود: استبدله
            else orders.Add(order);                  // مش موجود: ضيفه
        }, ct);

        Log.Info("OrderRepository", "Committing...");
        await Task.Delay(200, ct);

        Log.Success("OrderRepository", $"Order #{order.Id} saved");
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        Log.Info("OrderRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", "Executing query for all orders...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        var orders = await _jsonFileStore.ReadAllAsync<Order>(DataFiles.Orders, ct);

        Log.Info("OrderRepository", $"{orders.Count} orders found");
        return orders;
    }

    public async Task<List<Order>> GetByStatusAsync(OrderStatus s, CancellationToken ct)
    {
        Log.Info("OrderRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", "Executing query for all orders...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);
        var orders = await _jsonFileStore.ReadAllAsync<Order>(DataFiles.Orders, ct);
        var filteredByStatus = orders.Where(o => o.Status == s).ToList();

        Log.Info("OrderRepository", $"{filteredByStatus.Count} orders found with status {s}");

        return filteredByStatus;
    }
}