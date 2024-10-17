using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Volo.Abp.Caching;


namespace AElfScanServer.Common.ThirdPart.Exchange;

public static class BinanceApi
{
    public static ApiInfo TickerPrice = new(HttpMethod.Get, "/api/v3/ticker/price");
    public static ApiInfo KLine = new(HttpMethod.Get, "/api/v3/klines");
}

public class BinanceProvider : IExchangeProvider
{
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<string> _blockedCache;
    private readonly ILogger<BinanceProvider> _logger;

    public BinanceProvider(IOptionsMonitor<ExchangeOptions> exchangeOptions, IHttpProvider httpProvider,
        IDistributedCache<string> blocked, ILogger<BinanceProvider> logger)
    {
        _exchangeOptions = exchangeOptions;
        _httpProvider = httpProvider;
        _blockedCache = blocked;
        _logger = logger;
    }


    public BinanceOptions BinanceOptions()
    {
        return _exchangeOptions.CurrentValue.Binance;
    }

    public ExchangeProviderName Name()
    {
        return ExchangeProviderName.Binance;
    }

    public async Task<TokenExchangeDto> LatestAsync(string fromToken, string toToken)
    {
        return await BlockDetectAsync(async () =>
        {
            var res = await _httpProvider.InvokeAsync<BinanceTickerPrice>(
                BinanceOptions().BaseUrl, BinanceApi.TickerPrice,
                param: new Dictionary<string, string> { ["symbol"] = fromToken.ToUpper() + toToken.ToUpper() }
            );

            return new TokenExchangeDto
            {
                FromSymbol = fromToken,
                ToSymbol = toToken,
                Exchange = res.Price.SafeToDecimal(),
                Timestamp = DateTime.UtcNow.WithMicroSeconds(0).WithMilliSeconds(0).WithSeconds(0).ToUtcMilliSeconds()
            };
        });
    }


    public async Task<TokenExchangeDto> HistoryAsync(string fromToken, string toToken, long timestamp)
    {
        return await BlockDetectAsync(async () =>
        {
            var time = TimeHelper.GetDateTimeFromTimeStamp(timestamp)
                .WithSeconds(0).WithMicroSeconds(0).WithMilliSeconds(0).ToUtcMilliSeconds().ToString();
            var req = new KLineReq
            {
                Symbol = fromToken.ToUpper() + toToken.ToUpper(),
                Interval = Interval.Minute1,
                StartTime = time,
                EndTime = time,
                Limit = "1",
            };

            var res = await _httpProvider.InvokeAsync<List<List<string>>>(
                BinanceOptions().BaseUrl, BinanceApi.KLine,
                param: JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    JsonConvert.SerializeObject(req, HttpProvider.DefaultJsonSettings))
            );
            AssertHelper.NotEmpty(res, "Binance k-line resp empty");

            var kLine = KLineItem.FromArray(res[0]);
            return new TokenExchangeDto
            {
                FromSymbol = fromToken,
                ToSymbol = toToken,
                Exchange = kLine.EndTime >= timestamp ? kLine.EndAmount.SafeToDecimal() : kLine.AvgAmount(),
                Timestamp = DateTime.UtcNow.WithMicroSeconds(0).WithMilliSeconds(0).WithSeconds(0).ToUtcMilliSeconds()
            };
        });
    }


    public async Task<T> BlockDetectAsync<T>(Func<Task<T>> func)
    {
        var blockCacheKey = "BinanceBlocked429";
        AssertHelper.IsTrue(CollectionUtilities.IsNullOrEmpty((await _blockedCache.GetAsync(blockCacheKey))),
            "Binance api 429 blocked");

        var result = await ExecuteBlockDetectAsync(func);
        if (!result.Item1)
        {
            await _blockedCache.SetAsync(blockCacheKey, "1", new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTime.Now.AddSeconds(BinanceOptions().Block429Seconds)
            });
        }

        return result.Item2;
    }

    [ExceptionHandler(typeof(HttpRequestException), typeof(TimeoutException), typeof(Exception),
        Message = "ExecuteBlockDetectAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<(bool, T)> ExecuteBlockDetectAsync<T>(Func<Task<T>> func)
    {
        return (true, await func());
    }
}

public class BinanceTickerPrice
{
    public string Symbol { get; set; }
    public string Price { get; set; }
}

public class KLineItem
{
    private long StartTime { get; set; }
    public string StartAmount { get; set; }
    public string MaxAmount { get; set; }
    public string MinAmount { get; set; }
    public string EndAmount { get; set; }
    public long EndTime { get; set; }


    public decimal AvgAmount()
    {
        return (MaxAmount.SafeToDecimal() + MinAmount.SafeToDecimal()) / 2;
    }

    public static KLineItem FromArray(List<string> data)
    {
        AssertHelper.NotEmpty(data, "Binance k-line data empty");
        AssertHelper.IsTrue(data.Count >= 7, "Iinvalic Binance k-line data: {Data}", string.Join(",", data));

        return new KLineItem
        {
            StartTime = data[0].SafeToLong(),
            StartAmount = data[1],
            MaxAmount = data[2],
            MinAmount = data[3],
            EndAmount = data[4],
            EndTime = data[6].SafeToLong()
        };
    }
}

public class KLineReq
{
    public string Symbol { get; set; }
    public string Interval { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string Limit { get; set; }
}

public static class Interval
{
    public static string Second1 = "1s";
    public static string Minute1 = "1m";
    public static string Minute3 = "3m";
    public static string Minute5 = "5m";
    public static string Minute15 = "15m";
    public static string Minute30 = "30m";
    public static string Hour1 = "1h";
    public static string Hour2 = "2h";
    public static string Hour4 = "4h";
    public static string Hour6 = "6h";
    public static string Hour8 = "8h";
    public static string Hour12 = "12h";
}