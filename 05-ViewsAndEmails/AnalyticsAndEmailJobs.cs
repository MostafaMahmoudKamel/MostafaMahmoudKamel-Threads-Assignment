using System.Collections.Concurrent;

namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{

    //public readonly Dictionary<string, int> Views = new();
    //conccurrent dictionary is thread safe, but not atomic for incrementing values
    public readonly ConcurrentDictionary <string,int> ViewsConcurrent = new();
    public readonly ConcurrentQueue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();

    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {

        //if (!ViewsConcurrent(product))
        ViewsConcurrent.AddOrUpdate(product, 1, (key, old) => old + 1);

        //Views[product] = Views[product] + 1;//not atomic 
    }

    public void PlaceOrder(int orderId)
    {
        EmailJobs.Enqueue($"Email: order {orderId}");
    }

    public void EmailWorker()
    {
        while (true)
        {
            
                //string? job = null;
                //if (EmailJobs.Count > 0)
                if(EmailJobs.TryDequeue(out string? job))
                {
                    lock (_sentSync)
                        SentEmails.Add(job);
                    continue;

                }

            

                if (ProducersDone && EmailJobs.Count == 0)
                    return;

                Thread.Sleep(1);
            }
         
        
    }
}
