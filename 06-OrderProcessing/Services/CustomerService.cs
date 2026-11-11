namespace _06_OrderProcessing.Services;

using global::_06_OrderProcessing.Models;
using global::_06_OrderProcessing.Repositories.AsyncRepositories;
using global::_06_OrderProcessing.Shared;



public class CustomerService //get customer from repository 
{
    private readonly CustomerRepository _customerRepository = new CustomerRepository();

    //get Customer or Throw Exception if missing 
    public async Task<Customer> GetCustomerAsync(int id, CancellationToken ct)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);

        if (customer == null)
            throw new CustomerNotFoundException($"Customer #{id} not found");

        return customer;
    }
}
