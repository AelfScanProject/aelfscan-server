using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Client.Service;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AElf.Standards.ACS10;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using Binance.Spot;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public class BlockChainDataProvider : AbpRedisCache, ISingletonDependency
{
    private readonly INESTRepository<AddressIndex, string> _addressIndexRepository;
    private readonly GlobalOptions _globalOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<string> _tokenUsdPriceCache;

    // private readonly IElasticClient _elasticClient;

    private ConcurrentDictionary<string, string> _contractAddressCache;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private Dictionary<string, string> _tokenImageUrlCache;
    private  readonly ITokenPriceService _tokenPriceService;
    private readonly IDistributedCache<string> _tokenDecimalsCache;
    private readonly ILogger<BlockChainDataProvider> _logger;
    private readonly IOptionsMonitor<SecretOptions> _secretOptions;
    public BlockChainDataProvider(
        ILogger<BlockChainDataProvider> logger, IOptionsMonitor<GlobalOptions> blockChainOptions,
        IOptions<ElasticsearchOptions> options,IOptionsMonitor<SecretOptions> secretOptions,
        INESTRepository<AddressIndex, string> addressIndexRepository,
        IOptions<RedisCacheOptions> optionsAccessor,
        IHttpProvider httpProvider,
        IDistributedCache<string> tokenUsdPriceCache,ITokenIndexerProvider tokenIndexerProvider,ITokenPriceService tokenPriceService,
        IDistributedCache<string> tokenDecimalsCache
    ) : base(optionsAccessor)
    {
        _logger = logger;
        _globalOptions = blockChainOptions.CurrentValue;
        _httpProvider = httpProvider;
        var uris = options.Value.Url.ConvertAll(x => new Uri(x));
        // var connectionPool = new StaticConnectionPool(uris);
        // var settings = new ConnectionSettings(connectionPool);
        // _elasticClient = new ElasticClient(settings);
        _addressIndexRepository = addressIndexRepository;
        _contractAddressCache = new ConcurrentDictionary<string, string>();
        _tokenUsdPriceCache = tokenUsdPriceCache;
        _tokenImageUrlCache = new Dictionary<string, string>();
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenPriceService = tokenPriceService;
        _tokenDecimalsCache = tokenDecimalsCache;
        _secretOptions = secretOptions;
    }

    public async Task<string> TransformTokenToUsdValueAsync(string symbol, long amount,string chainId)
    {
        
        var tokenPriceAsync = await _tokenPriceService.GetTokenPriceAsync(symbol);
        if (tokenPriceAsync==null)
        {
            return "0";
        }
        
        var tokenDecimals = await GetTokenDecimals(symbol, chainId);

        _logger.LogInformation($"TransformTokenToUsdValueAsync token:{symbol} " +
                               $"price:{tokenPriceAsync.Price},amount:{amount},decimals:{tokenDecimals}");
        return (tokenPriceAsync.Price * amount /(decimal) Math.Pow(10, tokenDecimals)).ToString();
        
    }


    public async Task<string> GetDecimalAmountAsync(string symbol, long amount, string chainId)
    {
        var tokenDecimals = await GetTokenDecimals(symbol, chainId);

        return amount.ToDecimalsString(tokenDecimals);
    }


    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetTokenUsdPriceAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["symbol"])]
    public virtual async Task<string> GetTokenUsdPriceAsync(string symbol)
    {
        if (symbol == "USDT")
        {
            return "1";
        }

        var market = new Market(_globalOptions.BNBaseUrl);
       
            var usdPrice = await _tokenUsdPriceCache.GetAsync(symbol);
            if (!usdPrice.IsNullOrEmpty())
            {
                return usdPrice;
            }
        
            
            var currentAveragePrice = await market.CurrentAveragePrice(symbol + "USDT");
            JObject jsonObject = JsonConvert.DeserializeObject<JObject>(currentAveragePrice);
            var price = jsonObject["price"].ToString();
            await _tokenUsdPriceCache.SetAsync(symbol, price, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddSeconds(_globalOptions.TokenUsdPriceExpireDurationSeconds)
            });

            return price;
      
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetTokenUsd24ChangeAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGetTokenUsd24ChangeAsync), LogTargets = ["symbol"])]
    public virtual async Task<BinancePriceDto> GetTokenUsd24ChangeAsync(string symbol)
    {
        
            
            _logger.LogInformation("[TokenPriceProvider] [Binance] Start.");
            var market = new Market();
            var symbolPriceTicker = await market.TwentyFourHrTickerPriceChangeStatistics(symbol + "USDT");
            var binancePriceDto = JsonConvert.DeserializeObject<BinancePriceDto>(symbolPriceTicker);
            return binancePriceDto;
    
    }


    public async Task<int> GetTokenDecimals(string symbol, string chainId)
    {

        var key = "token-decimal:" + symbol + chainId;
        var decimalStr = await _tokenDecimalsCache.GetAsync(key);
        if (!decimalStr.IsNullOrEmpty())
        {
            return int.Parse(decimalStr);
        }
        
        var tokenDetailAsync = await _tokenIndexerProvider.GetTokenDetailAsync(chainId,symbol); 
        
        if(tokenDetailAsync ==null ||tokenDetailAsync.IsNullOrEmpty())
        {
            _logger.LogError($"can not find token detail for {symbol}");
            return 0;
        }
        var decimals = tokenDetailAsync.First().Decimals;
        _logger.LogInformation($"GetTokenDecimals {symbol} decimal:{decimals}");
        await _tokenDecimalsCache.SetAsync(key,decimals.ToString());


        return decimals;
       
    }

    public async Task<BlockDetailDto> GetBlockDetailAsync(string chainId, long blockHeight)
    {
        var apiPath = string.Format("/api/blockChain/blockByHeight?blockHeight={0}&includeTransactions=true",
            blockHeight);


        var response =
            await _httpProvider.InvokeAsync<BlockDetailDto>(_globalOptions.ChainNodeHosts[chainId],
                new ApiInfo(HttpMethod.Get, apiPath));


        return response;
    }


    public async Task<NodeTransactionDto> GetTransactionDetailAsync(string chainId, string transactionId)
    {
        var apiPath = string.Format("/api/blockChain/transactionResult?transactionId={0}",
            transactionId);


        var response =
            await _httpProvider.InvokeAsync<NodeTransactionDto>(_globalOptions.ChainNodeHosts[chainId],
                new ApiInfo(HttpMethod.Get, apiPath));


        return response;
    }


    public async Task<string> GeFormatTransactionParamAsync(string chainId, string contractAddress, string methodName,
        string param)
    {
        var apiPath = string.Format("/api/contract/formatTransactionParams");


        var response =
            await _httpProvider.PostAsync(_globalOptions.ChainNodeHosts[chainId] + apiPath,
                RequestMediaType.Json, new Dictionary<string, string>
                {
                    { "ContractAddress", contractAddress }, { "MethodName", methodName },
                    { "Param", param }
                });


        return response;
    }
}