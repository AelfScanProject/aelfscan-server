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

public class TokenHolderPercentProvider :  ITokenHolderPercentProvider, ISingletonDependency
{
    private const string TokenHolderCountRedisKey = "TokenHolderCount";
    private readonly ICacheProvider _cacheProvider;

    public TokenHolderPercentProvider( ICacheProvider cacheProvider) 
    {
        _cacheProvider = cacheProvider;
    }


    public async Task UpdateTokenHolderCount(Dictionary<string, long> counts, string chainId)
    {

        var today = DateTime.Now.ToString("yyyyMMdd");
        var key = GetRedisKey(chainId, today);

        var entries = counts.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
        await _cacheProvider.HashSetAsync(key, entries);
    }

    public async Task<Dictionary<string, long>> GetTokenHolderCount(string chainId, string date)
    {

        var allEntries = await _cacheProvider.HashGetAllAsync(GetRedisKey(chainId, date));

        return allEntries.ToDictionary(entry => (string)entry.Name, entry => (long)entry.Value);
    }

    public async Task<bool> CheckExistAsync(string chainId, string date)
    { 
        var key = GetRedisKey(chainId, date);
        return await _cacheProvider.KeyExistsAsync(key);
    }

    private static string GetRedisKey(string chainId, string date)
    {
        return IdGeneratorHelper.GenerateId(chainId, date, TokenHolderCountRedisKey);
    }
}