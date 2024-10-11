using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.Common.ExceptionHandling;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Worker.Core.Provider;

public interface IStorageProvider
{
    public Task SetAsync<T>(string key, T data) where T : class;
    public Task<T> GetAsync<T>(string key) where T : class, new();
}

public class StorageProvider : AbpRedisCache, IStorageProvider, ITransientDependency
{
    private readonly ILogger<StorageProvider> _logger;
    private readonly IDistributedCacheSerializer _serializer;

    public StorageProvider(IOptions<RedisCacheOptions> optionsAccessor, ILogger<StorageProvider> logger,
        IDistributedCacheSerializer serializer) : base(optionsAccessor)
    {
        _logger = logger;
        _serializer = serializer;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException))]
    public virtual async Task SetAsync<T>(string key, T data) where T : class
    {
        await ConnectAsync();

        await RedisDatabase.StringSetAsync(key, _serializer.Serialize(data));
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException))]
    public async Task<T> GetAsync<T>(string key) where T : class, new()
    {
        await ConnectAsync();

        var redisValue = await RedisDatabase.StringGetAsync(key);

        _logger.LogDebug("[StorageProvider] {key} spec: {spec}", key, redisValue);

        return redisValue.HasValue ? _serializer.Deserialize<T>(redisValue) : null;
    }
}