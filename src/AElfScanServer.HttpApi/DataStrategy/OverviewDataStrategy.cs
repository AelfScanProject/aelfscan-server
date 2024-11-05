using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElf.ExceptionHandler;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.ExceptionHandling;
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
    private readonly IEntityMappingRepository<MergeAddressIndex, string> _mergeAddressRepository;
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
        IEntityMappingRepository<MergeAddressIndex, string> mergeAddressRepository,
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
        _mergeAddressRepository = mergeAddressRepository;
    }


    public override async Task<HomeOverviewResponseDto> QueryData(string chainId)
    {
        return await ExecuteQueryData(chainId);
    }
    
  
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetBlockchainOverviewAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId"])]
    public virtual async Task<HomeOverviewResponseDto> ExecuteQueryData(string chainId)
    {
        DataStrategyLogger.LogInformation("GetBlockchainOverviewAsync:{chainId}", chainId);
        var overviewResp = new HomeOverviewResponseDto();
      
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


            tasks.Add(GetMergeTotalAccount(chainId).ContinueWith(task =>
            {
                overviewResp.MergeAccounts.Total = task.Result;
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


            return overviewResp;
    }
    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetTokens err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId","symbolType","specialSymbols"])]
    public virtual async Task<long> GetTokens(string chainId, SymbolType symbolType, List<string> specialSymbols = null)
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
                            mustClause.Add(m => m.Terms(t => t.Field("chainIds.keyword").Terms(chainId)));
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

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetMarketCap err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<string> GetMarketCap()
    {
        var marketCap = await _cache.GetAsync("MarketCap");
        if (marketCap.IsNullOrEmpty())
        {
           
                var marketCapInfo = await _chartDataService.GetDailyMarketCapRespAsync();
                marketCap = marketCapInfo.List.Last().TotalMarketCap;
                await _cache.SetAsync("MarketCap", marketCap);
          
        }

        return marketCap;
    }
    
    [ExceptionHandler( typeof(Exception),
        Message = "GetMergeTotalAccount err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId"])]
    public async Task<long> GetMergeTotalAccount(string chainId)
    {
         var totalCount = 0;
       
            var key = "MergeTotalAccount" + chainId;
            var count = await _cache.GetAsync(key);
            count = "";

            var queryableAsync = await _mergeAddressRepository.GetQueryableAsync();
            if (!chainId.IsNullOrEmpty())
            {
                queryableAsync = queryableAsync.Where(c => c.ChainId == chainId);
            }

            if (count.IsNullOrEmpty())
            {
                totalCount = queryableAsync.Count();
                await _cache.SetAsync(key, totalCount.ToString(), new DistributedCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
                DataStrategyLogger.LogInformation("overviewtest:TotalAccount {chainId},{count}",
                    chainId.IsNullOrEmpty() ? "merge" : chainId, totalCount);
                return totalCount;
            }


            return long.Parse(count);
      

    }
    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "QueryMergeChainData err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<HomeOverviewResponseDto> QueryMergeChainData()
    {
        var overviewResp = new HomeOverviewResponseDto();
       
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

            tasks.Add(GetMergeTotalAccount("AELF").ContinueWith(task =>
            {
                overviewResp.MergeAccounts.MainChain = task.Result;
            }));

            tasks.Add(GetMergeTotalAccount(_globalOptions.CurrentValue.SideChainId).ContinueWith(task =>
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
       

        return overviewResp;
    }


    public override string DisplayKey(string chainId)
    {
        return RedisKeyHelper.HomeOverview(chainId);
    }
}