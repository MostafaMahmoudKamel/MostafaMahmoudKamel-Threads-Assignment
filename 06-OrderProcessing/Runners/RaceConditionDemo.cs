using _06_OrderProcessing.Models;
using _06_OrderProcessing.Repositories.AsyncRepositories;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Runners;


public static class RaceConditionDemo
{
    private static readonly JsonFileStore _store = new JsonFileStore();
    private static readonly ProductRepository _products = new ProductRepository();

    public static async Task RunAsync(CancellationToken ct)
    {
        //1) unSafe : الاثنين هيقراوا نفس stock==1
        await SetKeyboardStockToOneAsync(ct);

        Task<bool> order10 = _products.ReserveStockUnsafeAsync(4, 1, ct);
        Task<bool> order11 = _products.ReserveStockUnsafeAsync(4, 1, ct);
        bool[] unsafeResults = await Task.WhenAll(order10, order11);

        await PrintResultAsync("UNSAFE", unsafeResults, ct);

        //2)Safe
        await SetKeyboardStockToOneAsync(ct);

        Task<bool> order10Safe = _products.ReserveStockAsync(4, 1, ct);
        Task<bool> order11Safe = _products.ReserveStockAsync(4, 1, ct);
        bool[] safeResults = await Task.WhenAll(order10Safe, order11Safe);

        await PrintResultAsync("SAFE", safeResults, ct);
    }

    //set keyboardQunatity =1
    private static async Task SetKeyboardStockToOneAsync(CancellationToken ct)
    {
        await _store.UpdateAsync<Product>(DataFiles.Products, products =>
        {
            var keyboard = products.First(p => p.Id == 4);
            keyboard.StockQuantity = 1;
        }, ct);
    }

    //print Async
    private static async Task PrintResultAsync(string title, bool[] results, CancellationToken ct)
    {
        var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
        int stock = products.First(p => p.Id == 4).StockQuantity;

        Console.WriteLine($"===== {title} =====");
        Console.WriteLine($"Order #10 got the keyboard: {results[0]}");
        Console.WriteLine($"Order #11 got the keyboard: {results[1]}");
        Console.WriteLine($"Stock in file: {stock}");
    }
}
