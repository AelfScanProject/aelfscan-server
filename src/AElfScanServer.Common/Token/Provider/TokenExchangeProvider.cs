using System;
using System.Threading.Tasks;
using AElfScanServer.Common.Options;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.Token.Provider;

public interface ITokenExchangeProvider
{
    Task<decimal> GetTokenPriceAsync(string baseCoin, string quoteCoin);
    Task<decimal> GetHistoryAsync(string baseCoin, string quoteCoin, string timestamp);
}

public class TokenExchangeProvider : ITokenExchangeProvider, ISingletonDependency
{
    private const string CacheKeyPrefix = "TokenExchangeHistory";
    private readonly ILogger<TokenExchangeProvider> _logger;
    private readonly IOptionsMonitor<NetWorkReflectionOptions> _netWorkReflectionOption;
    private readonly IPriceServerProvider _priceServerProvider;
    private readonly IDistributedCache<string> _priceCache;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;


    public TokenExchangeProvider(
        IOptionsMonitor<NetWorkReflectionOptions> netWorkReflectionOption, ILogger<TokenExchangeProvider> logger,IPriceServerProvider priceServerProvider,IDistributedCache<string> priceCache,IOptionsMonitor<GlobalOptions> globalOptions)
    {
        _priceCache= priceCache;
        _priceServerProvider = priceServerProvider;
        _netWorkReflectionOption = netWorkReflectionOption;
        _logger = logger;
        _globalOptions = globalOptions;
    }


    public async Task<decimal> GetTokenPriceAsync(string baseCoin, string quoteCoin)
    {
        try
        {
            if (_globalOptions.CurrentValue.NftSymbolConvert.TryGetValue(baseCoin,out var s))
            {
                baseCoin = s;
            };
            var pair = $"{baseCoin.ToLower()}-{quoteCoin.ToLower()}";
            var key = "GetTokenPrice" + pair;
            var priceCache = await _priceCache.GetAsync(key);
            
            if (!priceCache.IsNullOrEmpty())
            {
                return decimal.Parse(priceCache);
            }
            var res = await _priceServerProvider.GetAggregatedTokenPriceAsync(new ()
            {
                TokenPair =pair,
                AggregateType = AggregateType.Latest
            });

            if (res == null || res.Data == null)
            {
                _logger.LogError($"GetExchangeAsync err,pair:{pair}");
                return 0;
            }
            
            var price = res.Data.Price / (decimal)Math.Pow(10, (double)res.Data.Decimal);
            await _priceCache.SetAsync(key, price.ToString(), new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3)

            });
        
            _logger.LogInformation($"GetExchangeAsync success,pair:{baseCoin},price:{price},decimal:{res.Data.Decimal}");
            return price;
        }
        catch (Exception e)
        {
            
            _logger.LogError(e, $"GetExchangeAsync err,pair:{baseCoin}");
            throw;
        }
    }

    
    
    public async Task<decimal> GetHistoryAsync(string baseCoin, string quoteCoin, string timestamp)
    {
        if (_globalOptions.CurrentValue.NftSymbolConvert.TryGetValue(baseCoin,out var s))
        {
            baseCoin = s;
        };
        var pair = $"{baseCoin.ToLower()}-{quoteCoin.ToLower()}";
        var key = GetHistoryKey(CacheKeyPrefix, pair, timestamp);
        var priceCache = await _priceCache.GetAsync(key);
            
        if (!priceCache.IsNullOrEmpty())
        {
            return decimal.Parse(priceCache);
        }
        var res = await _priceServerProvider.GetDailyPriceAsync(new ()
        {
            TokenPair =pair,
            TimeStamp = timestamp
        });
        if (res == null || res.Data == null)
        {
            _logger.LogError($"GetExchangeAsync err,pair:{pair}");
            return 0;
        }
        var price = res.Data.Price / (decimal)Math.Pow(10, (double)res.Data.Decimal);
        await _priceCache.SetAsync(key, price.ToString(), new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)

        });
        _logger.LogInformation($"GetHistoryAsync success,pair:{baseCoin},price:{price},decimal:{res.Data.Decimal}");
        return price;
    }
    private string GetHistoryKey(string prefix, string pair, string timestamp)
    {
        return $"{prefix}-{pair}-{timestamp}";
    }
}