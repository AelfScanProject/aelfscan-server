using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public interface ICacheProvider
{
    public  Task StringSetAsync(string key, RedisValue value, TimeSpan? expire);
    public  Task StringIncrement(string key, long value, TimeSpan? expire);
    public  Task<string> StringGetAsync(string key);

    public Task HashSetAsync(string key, HashEntry[] entries);
    
    public Task<HashEntry[]> HashGetAllAsync(string getRedisKey);
    Task<bool> KeyExistsAsync(string key);
}

public class RedisCacheProvider : ICacheProvider, ISingletonDependency
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;

    public RedisCacheProvider(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer.GetDatabase();
    }

    public async Task StringSetAsync(string key, RedisValue value, TimeSpan? expire)
    {
        await _database.StringSetAsync(key, value, expiry: expire);
    }

    public async Task StringIncrement(string key, long value, TimeSpan? expire)
    {
       await _database.StringIncrementAsync(key,1);
       await _database.KeyExpireAsync(key, TimeSpan.FromDays(7));
    }



    public async Task<string> StringGetAsync(string key) 
    { 
        return await _database.StringGetAsync(key);
    }

    public async Task HashSetAsync(string key, HashEntry[] entries)
    {
        await _database.HashSetAsync(key,entries);
    }

    public async Task<HashEntry[]> HashGetAllAsync(string getRedisKey)
    {
       return await _database.HashGetAllAsync(getRedisKey);
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }
}