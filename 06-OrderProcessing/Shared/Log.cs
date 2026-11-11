using System;
using System.Collections.Generic;
using System.Text;

namespace _06_OrderProcessing.Shared;

public static class Log //no creating instance from static class
{
    public static readonly object _lock = new object();
    
    public static void Info(string who,string msg) => Write(who,msg,null);
    public static void Error(string who, string msg) => Write(who, msg, ConsoleColor.Red);
    public static void Success(string who, string msg) => Write(who, msg, ConsoleColor.Green);
    public static void Write(string who, string msg, ConsoleColor? color)
    {
        lock (_lock)
        {
            if(color!=null) Console.ForegroundColor=color.Value;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Environment.CurrentManagedThreadId}] {who} -> {msg}");
            Console.ResetColor(); //recet color after logging
        }
    }
}
