using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Repositories.AsyncRepositories;

public   class CustomerRepository
{
    //The best is inject it using Dependency injection 
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();//الافضل ابعته في الكونستركتور علشان لو عاوز اغيره  يبقا سهل 
    public  async Task<Customer?> GetByIdAsync(int id , CancellationToken ct)
    {
        Log.Info("CustomerRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("CustomerRepository", $"Executing query for Customer #{id}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct); //get by id take [300:600] delay

        //reading all customer.json from FileStore
        var customers=await _jsonFileStore.ReadAllAsync<Customer>(DataFiles.Customers, ct);
        var customer = customers.FirstOrDefault(c => c.Id == id);

        if (customer == null) Log.Error("CustomerRepository", $"Customer #{id} not found");
        else Log.Success("CustomerRepository", $"Customer #{id} found ({customer.Name})");

        return customer;
       
    }


}
