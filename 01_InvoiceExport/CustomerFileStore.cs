namespace InvoiceExport;

public sealed class CustomerFileStore
{
    readonly string _folder;

    public CustomerFileStore(string folder) => _folder = folder;

     public async Task< CustomerProfile> ReadProfileAsync(int customerId)
    {
        string path = Path.Combine(_folder, $"customer-{customerId}.txt");
        await Task.Delay(300);
        Task<string> textTask = File.ReadAllTextAsync(path);
        string text = await textTask;
        string[] parts = text.Split('|');
        return new CustomerProfile(int.Parse(parts[0]), parts[1], parts[2]);
    }
}
