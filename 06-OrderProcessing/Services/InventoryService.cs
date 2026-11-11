namespace _06_OrderProcessing.Services;

using global::_06_OrderProcessing.Models;
using global::_06_OrderProcessing.Repositories.AsyncRepositories;


public class InventoryService
{
    private readonly ProductRepository _productRepository = new ProductRepository();

    //reserve all item or nothing( if one fail)
    public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct)
    {
        var reserved = new List<OrderItem>();

        foreach (var item in order.Items)
        {
            bool ok = await _productRepository.ReserveStockAsync(item.ProductId, item.Quantity,ct);

            if (!ok)
            {
                // rollback 
                foreach (var done in reserved)
                    await _productRepository.ReleaseStockAsync(done.ProductId, done.Quantity, CancellationToken.None);

                return false;
            }

            reserved.Add(item);
        }

        return true;
    }

    // Adds every item's quantity back to the stock
    public async Task ReleaseStockAsync(Order order,CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
        }
    }
}

