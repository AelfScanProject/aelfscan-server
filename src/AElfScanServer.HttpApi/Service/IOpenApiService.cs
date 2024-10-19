using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.ThirdPart.Exchange;
using AElfScanServer.HttpApi.Dtos.ChartData;
using AElfScanServer.HttpApi.Dtos.OpenApi;
using AElfScanServer.HttpApi.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace AElfScanServer.HttpApi.Service;

public interface IOpenApiService
{
    public Task<SupplyApiResp> GetSupplyAsync();

    public Task<DailyActivityAddressApiResp> GetDailyActivityAddressAsync(string startDate, string endDate);

    public Task<DailyTransactionCountApiResp> GetDailyTransactionCountAsync(string startDate, string endDate);

    public Task<List<CurrencyPrice>> GetCurrencyPriceAsync();
}

public class OpenApiService : IOpenApiService
{
    private readonly IChartDataService _chartDataService;
    private readonly ILogger<OpenApiService> _logger;
    private readonly decimal MaxSupply = 1000000000;
    private readonly IndexerTokenProvider _indexerTokenProvider;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly CoinMarketCapProvider _coinMarketCapProvider;
    private readonly List<string> CurrencyList = new List<string>() { "krw", "usd", "idr", "sgd", "thb" };

    public OpenApiService(IChartDataService chartDataService, ILogger<OpenApiService> logger,
        IndexerTokenProvider indexerTokenProvider,
        IOptionsMonitor<GlobalOptions> globalOptions, CoinMarketCapProvider coinMarketCapProvider)
    {
        _chartDataService = chartDataService;
        _logger = logger;
        _indexerTokenProvider = indexerTokenProvider;
        _globalOptions = globalOptions;
        _coinMarketCapProvider = coinMarketCapProvider;
    }

    public async Task<SupplyApiResp> GetSupplyAsync()
    {
        var dailySupplyGrowthRespAsync = new DailySupplyGrowthResp();
        var organizationAddressBalance = 0l;
        var consensusContractAddressBalance = 0l;
        var tasks = new List<Task>();

        tasks.Add(_chartDataService.GetDailySupplyGrowthRespAsync().ContinueWith(task =>
        {
            dailySupplyGrowthRespAsync = task.Result;
        }));

        tasks.Add(_indexerTokenProvider
            .GetAddressElfBalanceAsync("AELF", _globalOptions.CurrentValue.OrganizationAddress).ContinueWith(
                task => { organizationAddressBalance = task.Result; }));

        tasks.Add(_indexerTokenProvider
            .GetAddressElfBalanceAsync("AELF", _globalOptions.CurrentValue.ConsensusContractAddress).ContinueWith(
                task => { consensusContractAddressBalance = task.Result; }));

        await tasks.WhenAll();

        var dailySupplyGrowth = dailySupplyGrowthRespAsync.List.Last();
        var supplyApiResp = new SupplyApiResp();
        supplyApiResp.MaxSupply = MaxSupply;
        supplyApiResp.Burn = dailySupplyGrowth.TotalBurnt;
        supplyApiResp.TotalSupply = MaxSupply - dailySupplyGrowth.TotalBurnt;
        supplyApiResp.CirculatingSupply = supplyApiResp.TotalSupply -
                                          decimal.Parse((organizationAddressBalance / 1e8).ToString()) -
                                          decimal.Parse((consensusContractAddressBalance / 1e8).ToString());

        return supplyApiResp;
    }


    public async Task<DailyActivityAddressApiResp> GetDailyActivityAddressAsync(string startDate, string endDate)
    {
        var tasks = new List<Task>();
        var mainActiveAddressData = new ActiveAddressCountResp();
        var sideActiveAddressData = new ActiveAddressCountResp();
        tasks.Add(_chartDataService.GetActiveAddressCountAsync(new ChartDataRequest
        {
            ChainId = "AELF"
        }).ContinueWith(task => { mainActiveAddressData = task.Result; }));

        tasks.Add(_chartDataService.GetActiveAddressCountAsync(new ChartDataRequest
        {
            ChainId = _globalOptions.CurrentValue.SideChainId
        }).ContinueWith(task => { sideActiveAddressData = task.Result; }));

        await tasks.WhenAll();
        var days = DateTimeHelper.GetDateIntervalDay(startDate, endDate);
        var mainDailyActiveAddressCounts = mainActiveAddressData.List.Where(c =>
                c.Date >= DateTimeHelper.ConvertYYMMDD(startDate) && c.Date < DateTimeHelper.ConvertYYMMDD(endDate))
            .ToList();

        var sideDailyActiveAddressCounts = sideActiveAddressData.List.Where(c =>
                c.Date >= DateTimeHelper.ConvertYYMMDD(startDate) && c.Date < DateTimeHelper.ConvertYYMMDD(endDate))
            .ToList();


        var dailyActivityAddressApiResp = new DailyActivityAddressApiResp();
        dailyActivityAddressApiResp.MainChain.Max =
            mainDailyActiveAddressCounts.MaxBy(c => c.AddressCount).AddressCount;
        dailyActivityAddressApiResp.MainChain.Min =
            mainDailyActiveAddressCounts.MinBy(c => c.AddressCount).AddressCount;
        dailyActivityAddressApiResp.MainChain.Avg = mainDailyActiveAddressCounts.Sum(c => c.AddressCount) / days;


        dailyActivityAddressApiResp.SideChain.Max =
            sideDailyActiveAddressCounts.MaxBy(c => c.AddressCount).AddressCount;
        dailyActivityAddressApiResp.SideChain.Min =
            sideDailyActiveAddressCounts.MinBy(c => c.AddressCount).AddressCount;
        dailyActivityAddressApiResp.SideChain.Avg = sideDailyActiveAddressCounts.Sum(c => c.AddressCount) / days;


        return dailyActivityAddressApiResp;
    }

    public async Task<DailyTransactionCountApiResp> GetDailyTransactionCountAsync(string startDate, string endDate)
    {
        var tasks = new List<Task>();
        var mainTransactionData = new DailyTransactionCountResp();
        var sideTransactionData = new DailyTransactionCountResp();
        tasks.Add(_chartDataService.GetDailyTransactionCountAsync(new ChartDataRequest
        {
            ChainId = "AELF"
        }).ContinueWith(task => { mainTransactionData = task.Result; }));

        tasks.Add(_chartDataService.GetDailyTransactionCountAsync(new ChartDataRequest
        {
            ChainId = _globalOptions.CurrentValue.SideChainId
        }).ContinueWith(task => { sideTransactionData = task.Result; }));

        await tasks.WhenAll();
        var days = DateTimeHelper.GetDateIntervalDay(startDate, endDate);
        var dailyTransactionCountApiResp = new DailyTransactionCountApiResp();

        var mainDailyTransactionCounts = mainTransactionData.List.Where(c =>
                c.Date >= DateTimeHelper.ConvertYYMMDD(startDate) && c.Date < DateTimeHelper.ConvertYYMMDD(endDate))
            .ToList();

        var sideDailyTransactionAddressCounts = sideTransactionData.List.Where(c =>
                c.Date >= DateTimeHelper.ConvertYYMMDD(startDate) && c.Date < DateTimeHelper.ConvertYYMMDD(endDate))
            .ToList();

        dailyTransactionCountApiResp.MainChain.TransactionAvgByAllType =
            mainDailyTransactionCounts.Sum(c => c.TransactionCount) / days;
        dailyTransactionCountApiResp.MainChain.TransactionAvgByExcludeSystem =
            (mainDailyTransactionCounts.Sum(c => c.TransactionCount) -
             mainDailyTransactionCounts.Sum(c => c.BlockCount) * 2) / days;

        dailyTransactionCountApiResp.SideChain.TransactionAvgByAllType =
            sideDailyTransactionAddressCounts.Sum(c => c.TransactionCount) / days;
        dailyTransactionCountApiResp.SideChain.TransactionAvgByExcludeSystem =
            (sideDailyTransactionAddressCounts.Sum(c => c.TransactionCount) -
             sideDailyTransactionAddressCounts.Sum(c => c.BlockCount) * 2) / days;

        return dailyTransactionCountApiResp;
    }


    public async Task<List<CurrencyPrice>> GetCurrencyPriceAsync()
    {
        var result = new List<CurrencyPrice>();
        var tasks = new List<Task>();

        var volume24HFromCmc = new SymbolInfo();
        var currencyPrice = new CoinInfo();

        var supplyApiResp = new SupplyApiResp();
        tasks.Add(_coinMarketCapProvider.GetVolume24hFromCMC("ELF").ContinueWith(task =>
        {
            volume24HFromCmc = task.Result;
        }));

        tasks.Add(_coinMarketCapProvider.GetCurrencyPrice().ContinueWith(task => { currencyPrice = task.Result; }));

        tasks.Add(GetSupplyAsync().ContinueWith(task => { supplyApiResp = task.Result; }));
        await tasks.WhenAll();


        var elfPriceUsdPrice = volume24HFromCmc.Quote["USD"].Price;
        var volume24H = volume24HFromCmc.Quote["USD"].Volume_24h;
        foreach (var keyValuePair in currencyPrice.Market_Data.Current_Price)
        {
            if (CurrencyList.Contains(keyValuePair.Key))
            {
                result.Add(new CurrencyPrice()
                {
                    CurrencyCode = keyValuePair.Key,
                    Price = keyValuePair.Value,
                    MarketCap = keyValuePair.Value * supplyApiResp.TotalSupply,
                    AccTradePrice24h = elfPriceUsdPrice / keyValuePair.Value * volume24H,
                    CirculatingSupply = supplyApiResp.CirculatingSupply,
                    MaxSupply = MaxSupply,
                    LastUpdatedTimestamp = DateTime.Now.Date.ToUtcMilliSeconds()
                });
            }
        }

        return result;
    }
}