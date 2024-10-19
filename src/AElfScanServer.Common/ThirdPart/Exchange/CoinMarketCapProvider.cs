using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.Caching;

namespace AElfScanServer.Common.ThirdPart.Exchange;

public class CoinMarketCapProvider
{
    private readonly IOptionsMonitor<GlobalOptions> _options;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<string> _blockedCache;
    private readonly ILogger<CoinMarketCapProvider> _logger;
    private readonly SecretOptions _secretOptions;
    private readonly string CMCDomain = "https://pro-api.coinmarketcap.com";

    private readonly string CMCUrl =
        "/v1/cryptocurrency/listings/latest?start=50&limit=200&convert=USD";

    public readonly string CurrencyPriceDomain = "https://api.coingecko.com/";
    public readonly string CurrencyPriceUrl = "api/v3/coins/aelf";


    public CoinMarketCapProvider(IOptionsMonitor<GlobalOptions> options, IHttpProvider httpProvider,
        IDistributedCache<string> blocked, ILogger<CoinMarketCapProvider> logger,
        IOptionsMonitor<SecretOptions> secretOptions)
    {
        _options = options;
        _httpProvider = httpProvider;
        _blockedCache = blocked;
        _logger = logger;
        _secretOptions = secretOptions.CurrentValue;
    }


    public async Task<SymbolInfo> GetVolume24hFromCMC(string symbol)
    {
        var head = new Dictionary<string, string>();
        head["X-CMC_PRO_API_KEY"] = _secretOptions.CMCApiKey;
        var key = _secretOptions.CMCApiKey + "-------";
        _logger.LogInformation($"wuhaoxuan {key}");
        var response =
            await _httpProvider.InvokeAsync<CMCCryptoCurrency>(CMCDomain,
                new ApiInfo(HttpMethod.Get, CMCUrl), header: head);

        var symbolInfos = response.Data.Where(c => c.Symbol == symbol).ToList();

        return symbolInfos.First();
    }


    public async Task<CoinInfo> GetCurrencyPrice()
    {
        var response =
            await _httpProvider.InvokeAsync<CoinInfo>(CurrencyPriceDomain,
                new ApiInfo(HttpMethod.Get, CurrencyPriceUrl));

        return response;
    }
}