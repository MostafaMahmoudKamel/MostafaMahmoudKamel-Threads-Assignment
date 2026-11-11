using _06_OrderProcessing.Models;
using System.Threading;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Services;

public interface IShippingCarrier
{
    string Name { get; }
    Task<decimal> GetQuoteAsync(Order order, CancellationToken ct);
}