using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.Caching.StackExchangeRedis;

namespace AElfScanServer.Common.Redis;

public class RedisCacheExtension : AbpRedisCache
{
    public RedisCacheExtension(IOptions<RedisCacheOptions> optionsAccessor) : base(optionsAccessor)
    {
    }
    
    public async Task<T> GetObjectAsync<T>(string key)
    {
        var value = await RedisDatabase.StringGetAsync(key);
        if (!value.HasValue)
        {
            return default(T);
        }
        return JsonConvert.DeserializeObject<T>(value);
    }
    
    public async Task SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonConvert.SerializeObject(value);
        await RedisDatabase.StringSetAsync(key, serializedValue, expiry);
    }
}