using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.DataStrategy;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Service;
using Elasticsearch.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using AddressIndex = AElfScanServer.Common.Dtos.ChartData.AddressIndex;

namespace AElfScanServer.HttpApi.DataStrategy;

public class OverviewDataStrategy : DataStrategyBase<string, HomeOverviewResponseDto>
{
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly HomePageProvider _homePageProvider;
    private readonly BlockChainDataProvider _blockChainProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IEntityMappingRepository<DailyUniqueAddressCountIndex, string> _uniqueAddressRepository;
    private readonly IBlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly IChartDataService _chartDataService;
    private readonly IEntityMappingRepository<AddressIndex, string> _addressRepository;
    private readonly IElasticClient _elasticClient;


    public OverviewDataStrategy(IOptions<RedisCacheOptions> optionsAccessor,
        IOptionsMonitor<GlobalOptions> globalOptions,
        AELFIndexerProvider aelfIndexerProvider,
        HomePageProvider homePageProvider,
        BlockChainDataProvider blockChainProvider,
        ITokenIndexerProvider tokenIndexerProvider,
        IBlockChainIndexerProvider blockChainIndexerProvider,
        IEntityMappingRepository<DailyUniqueAddressCountIndex, string> uniqueAddressRepository,
        ILogger<DataStrategyBase<string, HomeOverviewResponseDto>> logger, IDistributedCache<string> cache,
        IChartDataService chartDataService, IEntityMappingRepository<AddressIndex, string> addressRepository,
        IOptionsMonitor<ElasticsearchOptions> options) : base(
        optionsAccessor, logger, cache)
    {
        _globalOptions = globalOptions;
        _aelfIndexerProvider = aelfIndexerProvider;
        _homePageProvider = homePageProvider;
        _blockChainProvider = blockChainProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _uniqueAddressRepository = uniqueAddressRepository;
        _chartDataService = chartDataService;
        _addressRepository = addressRepository;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        _elasticClient = new ElasticClient(settings);
        EsIndex.SetElasticClient(_elasticClient);
    }

    public override async Task<HomeOverviewResponseDto> QueryData(string chainId)
    {
        DataStrategyLogger.LogInformation("GetBlockchainOverviewAsync:{chainId}", chainId);
        var overviewResp = new HomeOverviewResponseDto();
        try
        {
            if (chainId.IsNullOrEmpty())
            {
                var queryMergeChainData = await QueryMergeChainData();
                return queryMergeChainData;
            }

            var tasks = new List<Task>();
            tasks.Add(_aelfIndexerProvider.GetLatestBlockHeightAsync(chainId).ContinueWith(
                task => { overviewResp.BlockHeight = task.Result; }));


            tasks.Add(_blockChainIndexerProvider.GetTransactionCount(chainId).ContinueWith(task =>
            {
                overviewResp.MergeTransactions.Total = task.Result;
            }));


            tasks.Add(_uniqueAddressRepository.GetQueryableAsync().ContinueWith(
                task =>
                {
                    overviewResp.MergeAccounts.Total =
                        task.Result.Where(c => c.ChainId == chainId).OrderByDescending(c => c.Date).Take(1).ToList()
                            .First().TotalUniqueAddressees;
                }));


            tasks.Add(_homePageProvider.GetRewardAsync(chainId).ContinueWith(
                task =>
                {
                    overviewResp.Reward = task.Result.ToDecimalsString(8);
                    overviewResp.CitizenWelfare = (task.Result * 0.75).ToDecimalsString(8);
                }));

            tasks.Add(_blockChainProvider.GetTokenUsd24ChangeAsync("ELF").ContinueWith(
                task =>
                {
                    overviewResp.TokenPriceRate24h = task.Result.PriceChangePercent;
                    overviewResp.TokenPriceInUsd = task.Result.LastPrice;
                }));
            tasks.Add(_homePageProvider.GetTransactionCountPerLastMinute(chainId).ContinueWith(
                task => { overviewResp.MergeTps.Total = (task.Result / 60).ToString("F2"); }));

            await Task.WhenAll(tasks);


            DataStrategyLogger.LogInformation("Set home page overview success:{chainId}", chainId);
        }
        catch (Exception e)
        {
            DataStrategyLogger.LogError(e, "get home page overview err,chainId:{chainId}", chainId);
        }

        return overviewResp;
    }


    // public async Task<long> GetTokens(string chainId, SymbolType symbolType, List<string> specialSymbols = null)
    // {
    //     try
    //     {
    //         var searchDescriptor = new SearchDescriptor<TokenInfoIndex>()
    //             .Index("tokeninfoindex")
    //             .Query(q => q
    //                 .Bool(b => b
    //                     .Must(m =>
    //                     {
    //                         return !string.IsNullOrEmpty(chainId)
    //                             ? m.Terms(t => t.Field("chainId").Terms(chainId))
    //                             : null;
    //                     })
    //                     .Should(
    //                         s => s.Term(t => t.Field("type").Value(symbolType)),
    //                         s => s.Terms(t =>
    //                             t.Field("symbol")
    //                                 .Terms(specialSymbols))
    //                     )
    //                     .MinimumShouldMatch(1)
    //                 )
    //             )
    //             .Aggregations(a => a
    //                 .Cardinality("unique_symbol", t => t.Field("symbol"))
    //             );
    //
    //         var searchResponse = await _elasticClient.SearchAsync<TokenInfoIndex>(searchDescriptor);
    //
    //         var total = searchResponse.Aggregations.Cardinality("unique_symbol").Value;
    //         DataStrategyLogger.LogInformation("GetTokens: chain:{chainId},{total}",
    //             string.IsNullOrEmpty(chainId) ? "Merge" : chainId, total);
    //         return (long)total;
    //     }
    //     catch (Exception e)
    //     {
    //         DataStrategyLogger.LogError(e, "get token count err");
    //     }
    //
    //     return 0;
    // }


    public async Task<long> GetTokens(string chainId, SymbolType symbolType, List<string> specialSymbols = null)
    {
        try
        {
            var searchDescriptor = new SearchDescriptor<TokenInfoIndex>()
                .Index("tokeninfoindex")
                .Size(0).TrackTotalHits()
                .Query(q => q
                    .Bool(b =>
                    {
                        var mustClause = new List<Func<QueryContainerDescriptor<TokenInfoIndex>, QueryContainer>>();
                        if (!chainId.IsNullOrEmpty())
                        {
                            mustClause.Add(m => m.Terms(t => t.Field("chainId").Terms(chainId)));
                        }

                        var shouldClause = new List<Func<QueryContainerDescriptor<TokenInfoIndex>, QueryContainer>>
                        {
                            s => s.Term(t => t.Field("type").Value(symbolType))
                        };

                        if (!specialSymbols.IsNullOrEmpty())
                        {
                            shouldClause.Add(s => s.Terms(t => t.Field("symbol").Terms(specialSymbols)));
                        }

                        return b
                            .Must(mustClause.ToArray())
                            .Should(shouldClause.ToArray())
                            .MinimumShouldMatch(1);
                    })
                );

            var searchResponse = await _elasticClient.SearchAsync<TokenInfoIndex>(searchDescriptor);

            var total = searchResponse.Total;
            DataStrategyLogger.LogInformation("GetTokens: chain:{chainId},{total},{symbolType}",
                string.IsNullOrEmpty(chainId) ? "Merge" : chainId, total, symbolType);
            return total;
        }
        catch (Exception e)
        {
            DataStrategyLogger.LogError(e, "get token count err");
        }

        return 0;
    }


    public async Task<string> GetMarketCap()
    {
        var marketCap = await _cache.GetAsync("MarketCap");
        if (marketCap.IsNullOrEmpty())
        {
            try
            {
                var marketCapInfo = await _chartDataService.GetDailyMarketCapRespAsync();
                marketCap = marketCapInfo.List.Last().TotalMarketCap;
                await _cache.SetAsync("MarketCap", marketCap);
            }
            catch (Exception e)
            {
                DataStrategyLogger.LogError(e, "get market cap err");
            }
        }

        return marketCap;
    }


    public async Task<long> GetTotalAccount(string chainId)
    {
        var totalCount = 0;
        try
        {
            var key = "TotalAccount" + chainId;
            var count = await _cache.GetAsync(key);
            count = "";

            var queryableAsync = await _addressRepository.GetQueryableAsync();
            if (!chainId.IsNullOrEmpty())
            {
                queryableAsync = queryableAsync.Where(c => c.ChainId == chainId);
            }

            if (count.IsNullOrEmpty())
            {
                totalCount = queryableAsync.Count();
                await _cache.SetAsync(key, totalCount.ToString());
                DataStrategyLogger.LogInformation("overviewtest:TotalAccount {chainId},{count}",
                    chainId.IsNullOrEmpty() ? "merge" : chainId, totalCount);
                return totalCount;
            }


            return long.Parse(count);
        }
        catch (Exception e)
        {
            DataStrategyLogger.LogError(e, "get total account err");
        }

        return totalCount;
    }

    public async Task<HomeOverviewResponseDto> QueryMergeChainData()
    {
        var overviewResp = new HomeOverviewResponseDto();
        try
        {
            decimal mainChainTps = 0;
            decimal sideChainTps = 0;


            var tasks = new List<Task>();


            tasks.Add(_blockChainIndexerProvider.GetTransactionCount("AELF").ContinueWith(task =>
            {
                overviewResp.MergeTransactions.MainChain = task.Result;
            }));


            tasks.Add(_blockChainIndexerProvider.GetTransactionCount(_globalOptions.CurrentValue.SideChainId)
                .ContinueWith(task => { overviewResp.MergeTransactions.SideChain = task.Result; }));


            tasks.Add(_blockChainProvider.GetTokenUsd24ChangeAsync("ELF").ContinueWith(
                task =>
                {
                    overviewResp.TokenPriceRate24h = task.Result.PriceChangePercent;
                    overviewResp.TokenPriceInUsd = task.Result.LastPrice;
                }));

            tasks.Add(_homePageProvider.GetTransactionCountPerLastMinute("AELF").ContinueWith(
                task => { overviewResp.MergeTps.MainChain = (task.Result / 60).ToString("F2"); }));

            tasks.Add(_homePageProvider.GetTransactionCountPerLastMinute(_globalOptions.CurrentValue.SideChainId)
                .ContinueWith(
                    task => { overviewResp.MergeTps.SideChain = (task.Result / 60).ToString("F2"); }));

            tasks.Add(GetMarketCap().ContinueWith(task => { overviewResp.MarketCap = task.Result; }));

            tasks.Add(GetTotalAccount("AELF").ContinueWith(task =>
            {
                overviewResp.MergeAccounts.MainChain = task.Result;
            }));

            tasks.Add(GetTotalAccount(_globalOptions.CurrentValue.SideChainId).ContinueWith(task =>
            {
                overviewResp.MergeAccounts.SideChain = task.Result;
            }));

            tasks.Add(GetTokens("AELF", SymbolType.Token, _globalOptions.CurrentValue.SpecialSymbols)
                .ContinueWith(task => { overviewResp.MergeTokens.MainChain = task.Result; }));

            tasks.Add(GetTokens(_globalOptions.CurrentValue.SideChainId, SymbolType.Token,
                _globalOptions.CurrentValue.SpecialSymbols).ContinueWith(task =>
            {
                overviewResp.MergeTokens.SideChain = task.Result;
            }));

            tasks.Add(GetTokens("", SymbolType.Token, _globalOptions.CurrentValue.SpecialSymbols).ContinueWith(task =>
            {
                overviewResp.MergeTokens.Total = task.Result;
            }));


            tasks.Add(GetTokens("", SymbolType.Nft)
                .ContinueWith(task => { overviewResp.MergeNfts.Total = task.Result; }));

            tasks.Add(GetTokens("AELF", SymbolType.Nft).ContinueWith(task =>
            {
                overviewResp.MergeNfts.MainChain = task.Result;
            }));

            tasks.Add(GetTokens(_globalOptions.CurrentValue.SideChainId, SymbolType.Nft).ContinueWith(task =>
            {
                overviewResp.MergeNfts.SideChain = task.Result;
            }));


            await Task.WhenAll(tasks);

            overviewResp.MergeTps.Total =
                (decimal.Parse(overviewResp.MergeTps.MainChain) + decimal.Parse(overviewResp.MergeTps.SideChain))
                .ToString("F2");

            overviewResp.MergeTransactions.Total =
                overviewResp.MergeTransactions.MainChain + overviewResp.MergeTransactions.SideChain;
            overviewResp.MergeAccounts.Total =
                overviewResp.MergeAccounts.MainChain + overviewResp.MergeAccounts.SideChain;
            DataStrategyLogger.LogInformation("Set home page overview success: merge chain");
        }
        catch (Exception e)
        {
            DataStrategyLogger.LogError(e, "get home page overview err,chainId:merge chain");
        }

        return overviewResp;
    }


    public override string DisplayKey(string chainId)
    {
        return RedisKeyHelper.HomeOverview(chainId);
    }
}