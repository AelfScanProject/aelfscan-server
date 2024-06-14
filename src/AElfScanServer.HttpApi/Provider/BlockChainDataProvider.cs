using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using AElf.Standards.ACS10;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.Options;
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

public class BlockChainDataProvider : AbpRedisCache, ISingletonDependency
{
    private readonly INESTRepository<AddressIndex, string> _addressIndexRepository;
    private readonly GlobalOptions _globalOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<string> _tokenUsdPriceCache;

    // private readonly IElasticClient _elasticClient;

    private ConcurrentDictionary<string, string> _contractAddressCache;
    private Dictionary<string, string> _tokenImageBase64Cache;
    private readonly ILogger<BlockChainDataProvider> _logger;

    public BlockChainDataProvider(
        ILogger<BlockChainDataProvider> logger, IOptionsMonitor<GlobalOptions> blockChainOptions,
        IOptions<ElasticsearchOptions> options,
        INESTRepository<AddressIndex, string> addressIndexRepository,
        IOptions<RedisCacheOptions> optionsAccessor,
        IHttpProvider httpProvider,
        IDistributedCache<string> tokenUsdPriceCache
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
        _tokenImageBase64Cache = new Dictionary<string, string>();
    }


    public async Task<string> GetBlockRewardAsync(long blockHeight, string chainId)
    {
        try
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

            var address = chainId == "AELF"
                ? _globalOptions.TreasuryContractAddress
                : _globalOptions.ContractAddressConsensus;
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
        catch (Exception e)
        {
            _logger.LogError("get reward error:{@e}", e);
        }

        return "0";
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


    public async Task GetKlInePrice(long startTime, long endTime, string symbol, string chainId)
    {
        var market = new Market(_globalOptions.BNBaseUrl, _globalOptions.BNApiKey,
            _globalOptions.BNSecretKey);

        var klines = await market.KlineCandlestickData(symbol, Interval.ONE_HOUR, startTime, endTime);
    }


    public async Task<string> TransformTokenToUsdValueAsync(string symbol, long amount)
    {
        var tokenUsdPriceAsync = await GetTokenUsdPriceAsync(symbol);


        var tokenDecimals = await GetTokenDecimals(symbol, "AELF");
        var price = double.Parse(tokenUsdPriceAsync);

        return (price * amount / Math.Pow(10, tokenDecimals)).ToString();
    }


    public async Task<string> GetDecimalAmountAsync(string symbol, long amount)
    {
        var tokenDecimals = await GetTokenDecimals(symbol, "AELF");

        return amount.ToDecimalsString(tokenDecimals);
    }


    public async Task<string> GetTokenUsdPriceAsync(string symbol)
    {
        if (symbol == "USDT")
        {
            return "1";
        }

        var market = new Market(_globalOptions.BNBaseUrl);


        try
        {
            var usdPrice = _tokenUsdPriceCache.Get(symbol);
            if (!usdPrice.IsNullOrEmpty())
            {
                return usdPrice;
            }


            var currentAveragePrice = await market.CurrentAveragePrice(symbol + "USDT");
            JObject jsonObject = JsonConvert.DeserializeObject<JObject>(currentAveragePrice);
            var price = jsonObject["price"].ToString();
            _tokenUsdPriceCache.Set(symbol, price, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddSeconds(_globalOptions.TokenUsdPriceExpireDurationSeconds)
            });

            return price;
        }
        catch (Exception e)
        {
            _logger.LogError("get token usd price error:{@e}", e);
        }

        return "";
    }

    public async Task<BinancePriceDto> GetTokenUsd24ChangeAsync(string symbol)
    {
        // var market = new Market(_blockChainOptions.BNBaseUrl, _blockChainOptions.BNApiKey,
        //     _blockChainOptions.BNSecretKey);

        try
        {
            _logger.LogInformation("[TokenPriceProvider] [Binance] Start.");
            var market = new Market();

            // await ConnectAsync();
            // var redisValue = await RedisDatabase.StringGetAsync(symbol);
            // if (redisValue.HasValue)
            // {
            //     return _serializer.Deserialize<BinancePriceDto>(redisValue);
            // }

            var symbolPriceTicker = await market.TwentyFourHrTickerPriceChangeStatistics(symbol + "USDT");
            var binancePriceDto = JsonConvert.DeserializeObject<BinancePriceDto>(symbolPriceTicker);
            // await RedisDatabase.StringSetAsync(symbol, _serializer.Serialize(binancePriceDto), TimeSpan.FromHours(2));
            return binancePriceDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[TokenPriceProvider] [Binance] Parse response error.");
            return new BinancePriceDto();
        }
    }


    public async Task<string> GetTokenImageBase64Async(string symbol)
    {
        try
        {
            if (_tokenImageBase64Cache.TryGetValue(symbol, out var imageBase64))
            {
                return imageBase64;
            }

            if (TokenSymbolHelper.GetSymbolType(symbol) == SymbolType.Token)
            {
                try
                {
                    var file = File.ReadAllBytes($"TokenImage/{symbol}.png");
                    string base64Image = Convert.ToBase64String(file);
                    _tokenImageBase64Cache.Add(symbol, base64Image);
                    return base64Image;
                }
                catch (Exception e)
                {
                    _logger.LogWarning("get token:{0} image file err:{1}", symbol, e);
                    return "";
                }
            }


            if (TokenSymbolHelper.GetSymbolType(symbol) == SymbolType.Nft)
            {
                AElfClient elfClient = new AElfClient(_globalOptions.ChainNodeHosts["AELF"]);
                var tokenInfoInput = new GetTokenInfoInput
                {
                    Symbol = symbol
                };
                var transactionGetToken =
                    await elfClient.GenerateTransactionAsync(
                        elfClient.GetAddressFromPrivateKey(GlobalOptions.PrivateKey),
                        "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                        "GetTokenInfo",
                        tokenInfoInput);
                var txWithSignGetToken = elfClient.SignTransaction(GlobalOptions.PrivateKey, transactionGetToken);
                var transactionGetTokenResult = await elfClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txWithSignGetToken.ToByteArray().ToHex()
                });

                var token = new TokenInfo();
                token.MergeFrom(ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));

                if (token.ExternalInfo.Value.TryGetValue("inscription_image", out var inscription_image))
                {
                    try
                    {
                        Convert.FromBase64String(inscription_image);
                        _tokenImageBase64Cache.Add(symbol, inscription_image);
                        return inscription_image;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("parse token:{0} image base64  error:{1}", symbol, e);
                    }
                }

                if (token.ExternalInfo.Value.TryGetValue("__inscription_image", out var __inscription_image))
                {
                    try
                    {
                        Convert.FromBase64String(__inscription_image);
                        _tokenImageBase64Cache.Add(symbol, __inscription_image);
                        return __inscription_image;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("parse token:{0} image base64  error:{1}", symbol, e);
                    }
                }

                foreach (var keyValuePair in token.ExternalInfo.Value)
                {
                    if (keyValuePair.Key != "inscription_image" && keyValuePair.Key != "__inscription_image" &&
                        keyValuePair.Key.Contains("image"))
                    {
                        try
                        {
                            Convert.FromBase64String(keyValuePair.Value);
                            _tokenImageBase64Cache.Add(symbol, keyValuePair.Value);
                            return keyValuePair.Value;
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning("parse token:{0} other image base64  error:{1}", symbol, e);
                        }


                        var webClient = new WebClient();
                        var downloadData = webClient.DownloadData(keyValuePair.Value);
                        var base64String = Convert.ToBase64String(downloadData);
                        _tokenImageBase64Cache.Add(symbol, base64String);
                        return base64String;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("get token:{0} image base64  error:{1}", symbol, e);
        }


        return "";
    }


    public async Task<int> GetTokenDecimals(string symbol, string chainId)
    {
        await ConnectAsync();
        var redisValue = RedisDatabase.StringGet(RedisKeyHelper.TokenInfoKey(chainId, symbol));
        if (!redisValue.IsNullOrEmpty)
        {
            return Convert.ToInt32(redisValue);
        }


        var elfClient = new AElfClient(_globalOptions.ChainNodeHosts[chainId]);
        var address = (await elfClient.GetContractAddressByNameAsync(
            HashHelper.ComputeFrom("AElf.ContractNames.Token"))).ToBase58();
        var paramGetBalance = new GetTokenInfoInput
        {
            Symbol = symbol
        };


        var transactionGetToken =
            await elfClient.GenerateTransactionAsync(elfClient.GetAddressFromPrivateKey(GlobalOptions.PrivateKey),
                address,
                "GetTokenInfo",
                paramGetBalance);
        var txWithSignGetToken = elfClient.SignTransaction(GlobalOptions.PrivateKey, transactionGetToken);
        var transactionGetTokenResult = await elfClient.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSignGetToken.ToByteArray().ToHex()
        });
        var tokeninfo = AElf.Client.MultiToken.TokenInfo.Parser.ParseFrom(
            ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));

        RedisDatabase.StringSet(RedisKeyHelper.TokenInfoKey(chainId, symbol), tokeninfo.Decimals);
        return tokeninfo.Decimals;
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
}