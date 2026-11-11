using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Repositories.syncRepositories;



public class SyncProductRepository
{
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();

    public Product? GetById(int id)
    {
        Log.Info("SyncProductRepository", "Opening connection...");
        Thread.Sleep(200);

        Log.Info("SyncProductRepository", $"Executing query for Product #{id}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var products = _jsonFileStore.ReadAll<Product>(DataFiles.Products);
        var product = products.FirstOrDefault(p => p.Id == id);

        if (product is null) Log.Error("SyncProductRepository", $"Product #{id} not found");
        else Log.Info("SyncProductRepository", $"Product #{id} found ({product.Name})");
        return product;
    }


}
