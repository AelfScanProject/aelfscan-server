using System;
using System.IO;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.Common.ExceptionHandling;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;

namespace AElfScanServer.DataStrategy;

public interface IDataStrategy<TInput, TOutPut>
{
    Task LoadData(TInput input);
    Task<TOutPut> DisplayData(TInput input);
}

public class DataStrategyContext<TInput, TOutPut>
{
    private IDataStrategy<TInput, TOutPut> _dataStrategy;

    public DataStrategyContext(IDataStrategy<TInput, TOutPut> dataStrategy)
    {
        _dataStrategy = dataStrategy;
    }

    public async Task LoadData(TInput input)
    {
        await _dataStrategy.LoadData(input);
    }

    public async Task<TOutPut> DisplayData(TInput input)
    {
        return await _dataStrategy.DisplayData(input);
    }
}

public abstract class DataStrategyBase<TInput, TOutPut> : AbpRedisCache, IDataStrategy<TInput, TOutPut>
{
    protected ILogger<DataStrategyBase<TInput, TOutPut>> DataStrategyLogger { get; set; }
    protected IDistributedCache<string> _cache { get; set; }

    protected DataStrategyBase(IOptions<RedisCacheOptions> optionsAccessor,
        ILogger<DataStrategyBase<TInput, TOutPut>> logger, IDistributedCache<string> cache) : base(optionsAccessor)
    {
        DataStrategyLogger = logger;
        _cache = cache;
    }

    public async Task LoadData(TInput input)
    {
        var queryData = await QueryData(input);
        await SaveData(queryData, input);
    }

    public abstract Task<TOutPut> QueryData(TInput input);


    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception), Message = "SaveData",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New)]
    public virtual async Task SaveData(TOutPut data, TInput input)
    {
        var key = DisplayKey(input);
        var value = JsonConvert.SerializeObject(data);

        await _cache.SetAsync(key, value);
    }


    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception), Message = "DisplayData",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<TOutPut> DisplayData(TInput input)
    {
        var key = DisplayKey(input);
        var s = await _cache.GetAsync(key);
        return JsonConvert.DeserializeObject<TOutPut>(s);
    }

    public abstract string DisplayKey(TInput input);
}