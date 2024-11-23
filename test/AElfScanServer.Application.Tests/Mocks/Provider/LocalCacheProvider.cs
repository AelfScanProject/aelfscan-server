using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Provider;

public class LocalCacheProvider : ICacheProvider
{
    private readonly Dictionary<string, (RedisValue value, DateTime? expiry)> _cache = new Dictionary<string, (RedisValue, DateTime?)>();

    public Task StringSetAsync(string key, RedisValue value, TimeSpan? expire)
    {
        DateTime? expiry = expire.HasValue ? DateTime.Now.Add(expire.Value) : (DateTime?)null;
        _cache[key] = (value, expiry);
        return Task.CompletedTask;
    }

    public Task StringIncrement(string key, long value, TimeSpan? expire)
    {
        if (_cache.TryGetValue(key, out var entry) && (entry.expiry == null || entry.expiry > DateTime.Now))
        {
            long currentValue = (long)entry.value;
            currentValue += value;
            _cache[key] = (currentValue, expire.HasValue ? DateTime.Now.Add(expire.Value) : entry.expiry);
        }
        else
        {
            _cache[key] = (value, expire.HasValue ? DateTime.Now.Add(expire.Value) : (DateTime?)null);
        }
        return Task.CompletedTask;
    }

    public Task<string> StringGetAsync(string key)
    {
        if (_cache.TryGetValue(key, out var entry) && (entry.expiry == null || entry.expiry > DateTime.Now))
        {
            return Task.FromResult(entry.value.ToString());
        }
        return Task.FromResult<string>(null);
    }
}