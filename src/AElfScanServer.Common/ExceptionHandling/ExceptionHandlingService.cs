using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElfScanServer.Common.ExceptionHandling;

public class ExceptionHandlingService
{
    public static async Task<FlowBehavior> HandleException(Exception ex, string message)
    {
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }


    public static async Task<FlowBehavior> HandleExceptionDictionaryLongLongException(Exception ex)
    {
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new Dictionary<long, long>()
        };
    }


    public static async Task<FlowBehavior> HandleExceptionBoolException(Exception ex)
    {
        Console.WriteLine($"Handled exception: {ex.Message}");
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}