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
using AElf.EntityMapping.Repositories;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AElf.Standards.ACS10;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using Binance.Spot;
using Binance.Spot.Models;
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
using Convert = System.Convert;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

namespace AElfScanServer.HttpApi.Provider;

public interface IBlockChainDataProvider
{
    Task<string> GetBlockRewardAsync(long blockHeight, string chainId);
    Task<string> GetContractAddressAsync(string chainId, string contractName);
    Task<string> TransformTokenToUsdValueAsync(string symbol, long amount, string chainId);
    Task<string> GetDecimalAmountAsync(string symbol, long amount, string chainId);
    Task<string> GetTokenUsdPriceAsync(string symbol);
    Task<BinancePriceDto> GetTokenUsd24ChangeAsync(string symbol);
    Task<int> GetTokenDecimals(string symbol, string chainId);
    Task<BlockDetailDto> GetBlockDetailAsync(string chainId, long blockHeight);
    Task<NodeTransactionDto> GetTransactionDetailAsync(string chainId, string transactionId);
    Task<string> GeFormatTransactionParamAsync(string chainId, string contractAddress, string methodName, string param);
}
public class BlockChainDataProvider : AbpRedisCache ,IBlockChainDataProvider,ISingletonDependency
{
    private readonly GlobalOptions _globalOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<string> _tokenUsdPriceCache;

    // private readonly IElasticClient _elasticClient;

    private ConcurrentDictionary<string, string> _contractAddressCache;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private Dictionary<string, string> _tokenImageUrlCache;
    private  readonly ITokenPriceService _tokenPriceService;
    private readonly IDistributedCache<string> _tokenDecimalsCache;
    private readonly ILogger<IBlockChainDataProvider> _logger;

    public BlockChainDataProvider(
        ILogger<IBlockChainDataProvider> logger, IOptionsMonitor<GlobalOptions> blockChainOptions,
        IOptions<ElasticsearchOptions> options,
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
        _contractAddressCache = new ConcurrentDictionary<string, string>();
        _tokenUsdPriceCache = tokenUsdPriceCache;
        _tokenImageUrlCache = new Dictionary<string, string>();
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenPriceService = tokenPriceService;
        _tokenDecimalsCache = tokenDecimalsCache;
    }


    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetBlockRewardAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["blockHeight","chainId"])]
    public virtual async Task<string> GetBlockRewardAsync(long blockHeight, string chainId)
    {
        
            await ConnectAsync();
            var redisValue = RedisDatabase.StringGet(RedisKeyHelper.BlockRewardKey(chainId, blockHeight));
            if (!redisValue.IsNullOrEmpty)
            {
                _logger.LogInformation("hit cache");
                return redisValue;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var elfClient = new AElfClient(_globalOptions.ChainNodeHosts[chainId]);


            var name = chainId == "AELF" ? "Treasury" : "Consensus";

            var int64Value = new Int64Value();
            int64Value.Value = blockHeight;

            var address = _globalOptions.ContractAddressConsensus[chainId];
            if (address.IsNullOrEmpty())
            {
                return "";
            }

            var transaction =
                await elfClient.GenerateTransactionAsync(
                    elfClient.GetAddressFromPrivateKey(GlobalOptions.PrivateKey),
                    address,
                    "GetDividends", int64Value);
            var signTransaction =
                elfClient.SignTransaction(GlobalOptions.PrivateKey, transaction);
            var transactionResult = await elfClient.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = signTransaction.ToByteArray().ToHex()
            });

            var mapField = Dividends.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult)).Value;
            mapField.TryGetValue("ELF", out var reward);
            RedisDatabase.StringSet(RedisKeyHelper.BlockRewardKey(chainId, blockHeight), reward.ToString());
            stopwatch.Stop();

            _logger.LogInformation($"time get block reward {stopwatch.Elapsed.TotalSeconds} ,{blockHeight}");
            return reward.ToString();
     
    }

    public async Task<string> GetContractAddressAsync(string chainId, string contractName)
    {
        if (_contractAddressCache.TryGetValue($"{chainId}_{contractName}", out var address))
        {
            return address;
        }


        var elfClient = new AElfClient(_globalOptions.ChainNodeHosts[chainId]);
        var contractAddress = (await elfClient.GetContractAddressByNameAsync(
            HashHelper.ComputeFrom(contractName))).ToBase58();

        _contractAddressCache.TryAdd(contractName, contractAddress);

        return contractAddress;
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
        
            var tokenPriceAsync = await _tokenPriceService.GetTokenPriceAsync(symbol);
            
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