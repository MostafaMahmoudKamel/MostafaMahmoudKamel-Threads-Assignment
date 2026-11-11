using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Models;

public class CustomerDashboard
{
    public Customer Customer { get; set; }
    public List<Order> Orders { get; set; }
    public List<PaymentTransaction> Payments { get; set; }
    public List<EmailMessage> Emails { get; set; }
}
