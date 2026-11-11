using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Models;

public enum OrderStatus
{
    Pending, Validated, Rejected, PaymentFailed, Completed, Failed
}
