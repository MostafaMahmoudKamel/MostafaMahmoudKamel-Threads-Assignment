using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Shared;

public class DataFileCorruptedException : Exception
{
    public string FileName { get; }

    public DataFileCorruptedException(string fileName, Exception inner) : base($"Data file is corrupt: {fileName} contains invalid Json", inner)
    {
        FileName = fileName;
    }   
}
