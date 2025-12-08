using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Helper;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public interface ITokenHolderPercentProvider
{
    public Task UpdateTokenHolderCount(Dictionary<string, long> counts, string chainId);
    public Task<Dictionary<string, long>> GetTokenHolderCount(string chainId, string date);
    public Task<bool> CheckExistAsync(string chainId, string date);
}

public class TokenHolderPercentProvider : AbpRedisCache, ITokenHolderPercentProvider, ISingletonDependency
{
    private const string TokenHolderCountRedisKey = "TokenHolderCount";

    private readonly IDistributedCache<string> _distributedCache;

    public TokenHolderPercentProvider(IOptions<RedisCacheOptions> optionsAccessor, IDistributedCache<string> distributedCache) : base(optionsAccessor)
    {
        _distributedCache = distributedCache;
    }


    public async Task UpdateTokenHolderCount(Dictionary<string, long> counts, string chainId)
    {
        await ConnectAsync();

        var today = DateTime.Now.ToString("yyyyMMdd");
        var key = GetRedisKey(chainId, today);

        var entries = counts.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
        await RedisDatabase.HashSetAsync(key, entries);
    }

    public async Task<Dictionary<string, long>> GetTokenHolderCount(string chainId, string date)
    {
        await ConnectAsync();

        var allEntries = await RedisDatabase.HashGetAllAsync(GetRedisKey(chainId, date));

        return allEntries.ToDictionary(entry => (string)entry.Name, entry => (long)entry.Value);
    }

    public async Task<bool> CheckExistAsync(string chainId, string date)
    { 
        await ConnectAsync();

        var key = GetRedisKey(chainId, date);
        return await RedisDatabase.KeyExistsAsync(key);
    }

    private static string GetRedisKey(string chainId, string date)
    {
        return IdGeneratorHelper.GenerateId(chainId, date, TokenHolderCountRedisKey);
    }
}