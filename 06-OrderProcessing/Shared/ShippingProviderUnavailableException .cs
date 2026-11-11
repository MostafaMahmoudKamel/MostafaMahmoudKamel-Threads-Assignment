using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Shared;

public class ShippingProviderUnavailableException : Exception
{
    public ShippingProviderUnavailableException(string message) : base(message) { }
}

public class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(string message) : base(message) { }
}

public class InvalidCouponException : Exception
{
    public InvalidCouponException(string message) : base(message) { }
}

public class DatabaseWriteException : Exception
{
    public DatabaseWriteException(string message) : base(message) { }
}

public class SmtpUnavailableException : Exception
{
    public SmtpUnavailableException(string message) : base(message) { }
}