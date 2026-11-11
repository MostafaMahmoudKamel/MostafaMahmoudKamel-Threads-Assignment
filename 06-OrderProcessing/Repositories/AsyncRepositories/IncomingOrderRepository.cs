using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Repositories.AsyncRepositories;

public class IncomingOrderRepository
{
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();

    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        Log.Info("IncomingOrderRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("IncomingOrderRepository", "Executing query for incoming orders...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        var orders = await _jsonFileStore.ReadAllAsync<Order>(DataFiles.IncomingOrders, ct);

        Log.Info("IncomingOrderRepository", $"{orders.Count} incoming orders found");
        return orders;
    }
}
