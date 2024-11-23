using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Service;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Elasticsearch.Net;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;
using Field = Google.Protobuf.WellKnownTypes.Field;

namespace AElfScanServer.HttpApi.Service;

public interface IHomePageService
{

    public Task<BlocksResponseDto> GetLatestBlocksAsync(LatestBlocksRequestDto requestDto);


    public Task<HomeOverviewResponseDto> GetBlockchainOverviewAsync(BlockchainOverviewRequestDto req);

    public Task<TransactionPerMinuteResponseDto> GetTransactionPerMinuteAsync(
        string chainId);

    public Task<TransactionPerMinuteResponseDto> GetAllTransactionPerMinuteAsync();

    public Task<FilterTypeResponseDto> GetFilterType();
}

[Ump]
public class HomePageService : AbpRedisCache, IHomePageService, ITransientDependency
{
    private readonly INESTRepository<AddressIndex, string> _addressIndexRepository;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly HomePageProvider _homePageProvider;
    private readonly BlockChainDataProvider _blockChainProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IBlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly ITokenPriceService _tokenPriceService;

    private readonly ILogger<HomePageService> _logger;
    private const string SearchKeyPattern = "[^a-zA-Z0-9-_]";


    public HomePageService(IOptions<RedisCacheOptions> optionsAccessor,
        ILogger<HomePageService> logger, IOptionsMonitor<GlobalOptions> globalOptions,
        AELFIndexerProvider aelfIndexerProvider,
        INESTRepository<AddressIndex, string> addressIndexRepository,
        HomePageProvider homePageProvider, ITokenIndexerProvider tokenIndexerProvider,
        BlockChainDataProvider blockChainProvider, IBlockChainIndexerProvider blockChainIndexerProvider,
        ITokenPriceService tokenPriceService
    ) : base(optionsAccessor)
    {
        _logger = logger;
        _globalOptions = globalOptions;
        _aelfIndexerProvider = aelfIndexerProvider;
        _addressIndexRepository = addressIndexRepository;
        _homePageProvider = homePageProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _blockChainProvider = blockChainProvider;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _tokenPriceService = tokenPriceService;
    }

    public async Task<TransactionPerMinuteResponseDto> GetTransactionPerMinuteAsync(
        string chainId)
    {
        var transactionPerMinuteResp = new TransactionPerMinuteResponseDto();
        await ConnectAsync();
        var key = RedisKeyHelper.TransactionChartData(chainId);

        var dataValue = RedisDatabase.StringGet(key);

        var data =
            JsonConvert.DeserializeObject<List<TransactionCountPerMinuteDto>>(dataValue);

        transactionPerMinuteResp.Owner = data;

        var redisValue = RedisDatabase.StringGet(RedisKeyHelper.TransactionChartData("merge"));
        var mergeData =
            JsonConvert.DeserializeObject<List<TransactionCountPerMinuteDto>>(redisValue);

        transactionPerMinuteResp.All = mergeData;


        return transactionPerMinuteResp;
    }

    public async Task<TransactionPerMinuteResponseDto> GetAllTransactionPerMinuteAsync()
    {
        var transactionPerMinuteResp = new TransactionPerMinuteResponseDto();
        await ConnectAsync();

        var redisValue = RedisDatabase.StringGet(RedisKeyHelper.TransactionChartData("merge"));
        var mergeData =
            JsonConvert.DeserializeObject<List<TransactionCountPerMinuteDto>>(redisValue);

        transactionPerMinuteResp.All = mergeData;


        return transactionPerMinuteResp;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "GetBlockchainOverviewAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["req"])]
    public virtual async Task<HomeOverviewResponseDto> GetBlockchainOverviewAsync(BlockchainOverviewRequestDto req)
    {
        var overviewResp = new HomeOverviewResponseDto();
        if (!_globalOptions.CurrentValue.ChainIds.Exists(s => s == req.ChainId))
        {
            _logger.LogWarning("Get blockchain overview chainId not exist:{chainId},chainIds:{chainIds}", req.ChainId,
                _globalOptions.CurrentValue.ChainIds);
            return overviewResp;
        }

      
            var tasks = new List<Task>();
            tasks.Add(_aelfIndexerProvider.GetLatestBlockHeightAsync(req.ChainId).ContinueWith(
                task => { overviewResp.BlockHeight = task.Result; }));

            tasks.Add(_blockChainIndexerProvider.GetTransactionCount(req.ChainId).ContinueWith(task =>
            {
                overviewResp.Transactions = task.Result;
            }));

            tasks.Add(_tokenIndexerProvider.GetAccountCountAsync(req.ChainId).ContinueWith(
                task => { overviewResp.Accounts = task.Result; }));


            tasks.Add(_homePageProvider.GetRewardAsync(req.ChainId).ContinueWith(
                task =>
                {
                    overviewResp.Reward = task.Result.ToDecimalsString(8);
                    overviewResp.CitizenWelfare = (task.Result * 0.75).ToDecimalsString(8);
                }));

            tasks.Add(_blockChainProvider.GetTokenUsd24ChangeAsync("ELF").ContinueWith(
                task =>
                {
                    overviewResp.TokenPriceRate24h = task.Result.PriceChangePercent;
                }));
            
            
                tasks.Add(_tokenPriceService.GetTokenPriceAsync("ELF").ContinueWith(
                task =>
                {
                    if (task.Result != null)
                    {
                     overviewResp.TokenPriceInUsd = task.Result.Price;

                    }
                }));
            tasks.Add(_homePageProvider.GetTransactionCountPerLastMinute(req.ChainId).ContinueWith(
                task => { overviewResp.Tps = (task.Result / 60).ToString("F2"); }));

            await Task.WhenAll(tasks);
       

        return overviewResp;
    }

    
    public async Task<FilterTypeResponseDto> GetFilterType()
    {
        var filterTypeResp = new FilterTypeResponseDto();
        filterTypeResp.FilterTypes = new List<FilterTypeDto>();
        foreach (var keyValuePair in _globalOptions.CurrentValue.FilterTypes)
        {
            var filterTypeDto = new FilterTypeDto();
            filterTypeDto.FilterType = keyValuePair.Value;
            filterTypeDto.FilterInfo = keyValuePair.Key;
            filterTypeResp.FilterTypes.Add(filterTypeDto);
        }

        filterTypeResp.FilterTypes = filterTypeResp.FilterTypes.OrderBy(o => o.FilterType).ToList();

        return filterTypeResp;
    }


    [ExceptionHandler(typeof(Exception),
        Message = "GetLatestBlocksAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["requestDto"])]
    public virtual async Task<BlocksResponseDto> GetLatestBlocksAsync(LatestBlocksRequestDto requestDto)
    {
        var result = new BlocksResponseDto() { };
        if (!_globalOptions.CurrentValue.ChainIds.Exists(s => s == requestDto.ChainId) ||
            requestDto.MaxResultCount <= 0 ||
            requestDto.MaxResultCount > _globalOptions.CurrentValue.MaxResultCount)
        {
            return result;
        }
        
            var aElfClient = new AElfClient(_globalOptions.CurrentValue.ChainNodeHosts[requestDto.ChainId]);
            var blockHeightAsync = await aElfClient.GetBlockHeightAsync();


            var blockList = await _aelfIndexerProvider.GetLatestBlocksAsync(requestDto.ChainId,
                blockHeightAsync - requestDto.MaxResultCount,
                blockHeightAsync);
            
            result.Blocks = new List<BlockRespDto>();
            result.Total = blockList.Count;

            for (var i = blockList.Count - 1; i > 0; i--)
            {
                var indexerBlockDto = blockList[i];
                var latestBlockDto = new BlockRespDto();

                latestBlockDto.BlockHeight = indexerBlockDto.BlockHeight;
                latestBlockDto.Timestamp = DateTimeHelper.GetTotalSeconds(indexerBlockDto.BlockTime);
                latestBlockDto.TransactionCount = indexerBlockDto.TransactionIds.Count;

                latestBlockDto.TimeSpan = (Convert.ToDouble(0 < blockList.Count
                    ? DateTimeHelper.GetTotalMilliseconds(indexerBlockDto.BlockTime) -
                      DateTimeHelper.GetTotalMilliseconds(blockList[i - 1].BlockTime)
                    : 0) / 1000).ToString("0.0");
               
                latestBlockDto.Reward = "";
                result.Blocks.Add(latestBlockDto);
            }

            return result;
    }


}