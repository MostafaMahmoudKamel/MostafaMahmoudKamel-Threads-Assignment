using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Models;

public record ShippingQuote(string Carrier, decimal Price, double ElapsedMs);

