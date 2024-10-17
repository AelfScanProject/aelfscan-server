using 
    System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Redis;
using AElfScanServer.Common.ThirdPart.Exchange;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.Token.Provider;

public interface ITokenExchangeProvider
{
    Task<Dictionary<string, TokenExchangeDto>> GetAsync(string baseCoin, string quoteCoin);
    
    Task<Dictionary<string, TokenExchangeDto>> GetHistoryAsync(string baseCoin, string quoteCoin, long timestamp);
}

public class TokenExchangeProvider : RedisCacheExtension, ITokenExchangeProvider, ISingletonDependency
{
    private const string CacheKeyPrefix = "TokenExchange";
    private readonly ILogger<TokenExchangeProvider> _logger;
    private readonly Dictionary<string, IExchangeProvider> _exchangeProviders;
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly IOptionsMonitor<NetWorkReflectionOptions> _netWorkReflectionOption;
    private readonly SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim HisWriteLock = new SemaphoreSlim(1, 1);


    public TokenExchangeProvider(IOptions<RedisCacheOptions> optionsAccessor,
        IEnumerable<IExchangeProvider> exchangeProviders,
        IOptionsMonitor<ExchangeOptions> exchangeOptions,
        IOptionsMonitor<NetWorkReflectionOptions> netWorkReflectionOption, ILogger<TokenExchangeProvider> logger) :
        base(optionsAccessor)
    {
        _exchangeOptions = exchangeOptions;
        _netWorkReflectionOption = netWorkReflectionOption;
        _logger = logger;
        _exchangeProviders = exchangeProviders.ToDictionary(p => p.Name().ToString());
    }
    
    
    public async Task<Dictionary<string, TokenExchangeDto>> GetAsync(string baseCoin, string quoteCoin)
    {
        await ConnectAsync();
        var key = GetKey(CacheKeyPrefix, baseCoin, quoteCoin);
        var value = await GetObjectAsync<Dictionary<string, TokenExchangeDto>>(key);

        if (!value.IsNullOrEmpty())
        {
            return value;
        }

        // Wait to acquire the lock before proceeding with the update
        await WriteLock.WaitAsync();
        try
        {
            var asyncTasks = new List<Task<KeyValuePair<string, TokenExchangeDto>>>();
            foreach (var provider in _exchangeProviders.Values)
            {
                var providerName = provider.Name().ToString();
                asyncTasks.Add(GetExchangeAsync(provider, baseCoin, quoteCoin, providerName));
            }
            
            var results = await Task.WhenAll(asyncTasks);
            var exchangeInfos = results.Where(r => r.Value != null)
                .ToDictionary(r => r.Key, r => r.Value);
            await SetObjectAsync(key, exchangeInfos, TimeSpan.FromSeconds(_exchangeOptions.CurrentValue.DataExpireSeconds));
            return exchangeInfos;
        }
        finally
        {
            WriteLock.Release();
        }
    }
    
    public async Task<Dictionary<string, TokenExchangeDto>> GetHistoryAsync(string baseCoin, string quoteCoin, long timestamp)
    {
        await ConnectAsync();
        var key = GetHistoryKey(CacheKeyPrefix, baseCoin, quoteCoin, timestamp);
        var value = await GetObjectAsync<Dictionary<string, TokenExchangeDto>>(key);

        if (!value.IsNullOrEmpty())
        {
            return value;
        }

        // Wait to acquire the lock before proceeding with the update
        await HisWriteLock.WaitAsync();
        try
        {
            var asyncTasks = new List<Task<KeyValuePair<string, TokenExchangeDto>>>();
            foreach (var provider in _exchangeProviders.Values)
            {
                var providerName = provider.Name().ToString();
                asyncTasks.Add(GetHistoryExchangeAsync(provider, baseCoin, quoteCoin, timestamp, providerName));
            }

            var results = await Task.WhenAll(asyncTasks);
            var exchangeInfos = results.Where(r => r.Value != null)
                .ToDictionary(r => r.Key, r => r.Value);
            await SetObjectAsync(key, exchangeInfos, TimeSpan.FromDays(7));
            return exchangeInfos;
        }
        finally
        {
            HisWriteLock.Release();
        }
    }
    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetExchangeAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["provider","baseCoin","quoteCoin","providerName"])]
    public virtual async Task<KeyValuePair<string, TokenExchangeDto>> GetExchangeAsync(
        IExchangeProvider provider, string baseCoin, string quoteCoin, string providerName)
    {
       
            var result = await provider.LatestAsync(MappingSymbol(baseCoin.ToUpper()), 
                MappingSymbol(quoteCoin.ToUpper()));
            return new KeyValuePair<string, TokenExchangeDto>(providerName, result);
       
    }
    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetHistoryExchangeAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["provider","baseCoin","quoteCoin","timestamp","providerName"])]
    public virtual async Task<KeyValuePair<string, TokenExchangeDto>> GetHistoryExchangeAsync(
        IExchangeProvider provider, string baseCoin, string quoteCoin, long timestamp, string providerName)
    {
        
            var result = await provider.HistoryAsync(MappingSymbol(baseCoin.ToUpper()), 
                MappingSymbol(quoteCoin.ToUpper()), timestamp);
            return new KeyValuePair<string, TokenExchangeDto>(providerName, result);
       
    }

    private string MappingSymbol(string sourceSymbol)
    {
        return _netWorkReflectionOption.CurrentValue.SymbolItems.TryGetValue(sourceSymbol, out var targetSymbol)
            ? targetSymbol
            : sourceSymbol;
    }
    
    private string GetKey(string prefix, string baseCoin, string quoteCoin)
    {
        return $"{prefix}-{baseCoin}-{quoteCoin}";
    }
    
    private string GetHistoryKey(string prefix, string baseCoin, string quoteCoin, long timestamp)
    {
        return $"{prefix}-{baseCoin}-{quoteCoin}-{timestamp}";
    }
}