using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OpenTelemetry;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using Microsoft.Extensions.DependencyInjection;


namespace AElfScanServer.Common.ExceptionHandling;

public class ExceptionHandlingService 
{
    private static IServiceProvider _serviceProvider;
    private static Histogram<long> _exceptionHistogram;
    private static Counter<long> _alarmCounter;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static  async Task<FlowBehavior> HandleException(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetTokenPriceAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =   new CommonTokenPriceDto()
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetExchangeAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =    new KeyValuePair<string, TokenExchangeDto>()
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetDailyHolderListAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new IndexerDailyHolderDto()
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetTransactionsAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new List<TransactionIndex>()
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetTransactionsDataAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new List<TransactionData>()
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetTokenDetailAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new TokenDetailDto()
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetTokenImageAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  ""
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionIsContractAddressAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  false
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetContractListAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new Dictionary<string, ContractInfoDto>()
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetContractAddressAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new IndexerContractListResultDto()
        };
    }
    public static  async Task<FlowBehavior> HandleExceptionGetRewardAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  0L
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetAddressElfBalanceAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  0L
        };
    }
    
    public static  async Task<FlowBehavior> HandleExceptionGetTokenUsd24ChangeAsync(Exception ex)
    {
        SetExceptionCount();
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new BinancePriceDto()
        };
    }
    
    
    
    private static void SetExceptionCount()
    {
        if (_exceptionHistogram == null)
        {
            _exceptionHistogram = _serviceProvider.GetRequiredService<IInstrumentationProvider>().Meter.CreateHistogram<long>("aelfScanExceptionTotal", "ms", "Histogram for total execution time");
        }

        _exceptionHistogram.Record(1);
    }


    public static async Task<FlowBehavior> HandleExceptionParseBlockBurntAsync(Exception ex, string chainId,
        long startBlockHeight,
        long endBlockHeight)
    {
        Console.WriteLine($"Handled exception: {ex.Message}，{chainId}, {startBlockHeight}, {endBlockHeight}");
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new Dictionary<long, long>()
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
    
    public static  async Task<FlowBehavior> AlarmNftException(Exception ex)
    {
        SetExceptionCount();
        AlarmException("NFT");
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
    
    public static  async Task<FlowBehavior> AlarmException(Exception ex)
    {
        SetExceptionCount();
        AlarmException(ex.Message);
        return new FlowBehavior()
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }

    private static void AlarmException(string type)
    {
        if (_alarmCounter == null)
        {
            _alarmCounter = _serviceProvider.GetRequiredService<IInstrumentationProvider>().Meter.
                CreateCounter<long>(
                    "aelfScanAlarmCount", 
                    "counts", 
                    "The number of Alarm"
                );        
        }

        _alarmCounter.Add(1,new KeyValuePair<string, object>("alarmType",type));
    }
}