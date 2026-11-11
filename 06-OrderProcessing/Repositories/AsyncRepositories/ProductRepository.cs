using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace _06_OrderProcessing.Repositories.AsyncRepositories;

public class ProductRepository
{
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();
    public async Task<Product?> GetByIdAsync(int id,CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", $"Executing query for product #{id}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct); //get by id take [300:600] delay

        var products=await _jsonFileStore.ReadAllAsync<Product>(DataFiles.Products, ct);
        var product=products.FirstOrDefault<Product>(p => p.Id == id);//first matching product

        if (product == null) Log.Error("ProductRepository", $"Product #{id} not found");
        else Log.Success("ProductRepository", $"Product #{id} found ({product.Name})");

        return product;
    }

    public async Task<bool> ReserveStockAsync(int productId, int qty,CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", "Beginning transaction...");
        await Task.Delay(100, ct);

        Log.Info("ProductRepository", $"Reserving {qty} of product #{productId}...");
        await Task.Delay(400, ct);

        bool reserved = false;

        await _jsonFileStore.UpdateAsync<Product>(DataFiles.Products, products =>
        {
            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product != null && product.StockQuantity >= qty)
            {
                product.StockQuantity -= qty;
                reserved = true;
            }
        }, ct);

        Log.Info("ProductRepository", "Committing...");
        await Task.Delay(200, ct);

        if (reserved) Log.Success("ProductRepository", $"Reserved {qty} of product #{productId}");
        else Log.Error("ProductRepository", $"Not enough stock for product #{productId}");

        return reserved;
    
    }

    // 1) Release stock : هرجع الكميه للمخزون تاني 
    public async Task ReleaseStockAsync(int productId, int qty, CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", "Beginning transaction...");
        await Task.Delay(100, ct);

        Log.Info("ProductRepository", $"Releasing {qty} of product #{productId}...");
        await Task.Delay(400, ct);

        await _jsonFileStore.UpdateAsync<Product>(DataFiles.Products, products =>
        {
            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product != null)
            {
                product.StockQuantity += qty;//add stockQunatitiy
            }
        }, ct);

        Log.Info("ProductRepository", "Committing...");
        await Task.Delay(200, ct);

        Log.Success("ProductRepository", $"Released {qty} of product #{productId}");
    }

    //Race condition
    public async Task<bool> ReserveStockUnsafeAsync(int productId, int qty, CancellationToken ct)
    {
        var products = await _jsonFileStore.ReadAllAsync<Product>(DataFiles.Products, ct);
        var product = products.FirstOrDefault(p => p.Id == productId);

        if (product == null || product.StockQuantity < qty) return false;

        int newStock = product.StockQuantity - qty;  

        await Task.Delay(200, ct);   // open connection
        await Task.Delay(100, ct);   // begin transaction
        await Task.Delay(400, ct);   // update

        await _jsonFileStore.UpdateAsync<Product>(DataFiles.Products, list =>
        {
            var p = list.FirstOrDefault(x => x.Id == productId);
            if (p != null) p.StockQuantity = newStock;
        }, ct);

        return true;
    }

}
