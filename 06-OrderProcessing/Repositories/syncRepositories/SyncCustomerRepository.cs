using _06_OrderProcessing.Models;
using _06_OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Repositories.syncRepositories;

public class SyncCustomerRepository
{
    private readonly JsonFileStore _jsonFileStore = new JsonFileStore();

    public Customer? GetById(int id)
    {
        Log.Info("SyncCustomerRepository", "Opening connection...");
        Thread.Sleep(200);

        Log.Info("SyncCustomerRepository", $"Executing query for Customer #{id}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var customers = _jsonFileStore.ReadAll<Customer>(DataFiles.Customers);
        var customer = customers.FirstOrDefault(c => c.Id == id);

        if (customer is null) Log.Error("SyncCustomerRepository", $"Customer #{id} not found");
        else Log.Info("SyncCustomerRepository", $"Customer #{id} found ({customer.Name})");
        return customer;
    }
}
