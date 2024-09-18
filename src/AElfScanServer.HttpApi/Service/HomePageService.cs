using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using Elasticsearch.Net;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
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
    public Task<LatestTransactionsResponseSto> GetLatestTransactionsAsync(LatestTransactionsReq req);
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

    private readonly ILogger<HomePageService> _logger;
    private const string SearchKeyPattern = "[^a-zA-Z0-9-_]";


    public HomePageService(IOptions<RedisCacheOptions> optionsAccessor,
        ILogger<HomePageService> logger, IOptionsMonitor<GlobalOptions> globalOptions,
        AELFIndexerProvider aelfIndexerProvider,
        INESTRepository<AddressIndex, string> addressIndexRepository,
        HomePageProvider homePageProvider, ITokenIndexerProvider tokenIndexerProvider,
        BlockChainDataProvider blockChainProvider, IBlockChainIndexerProvider blockChainIndexerProvider
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

    public async Task<HomeOverviewResponseDto> GetBlockchainOverviewAsync(BlockchainOverviewRequestDto req)
    {
        var overviewResp = new HomeOverviewResponseDto();
        if (!_globalOptions.CurrentValue.ChainIds.Exists(s => s == req.ChainId))
        {
            _logger.LogWarning("Get blockchain overview chainId not exist:{chainId},chainIds:{chainIds}", req.ChainId,
                _globalOptions.CurrentValue.ChainIds);
            return overviewResp;
        }

        try
        {
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
                    overviewResp.TokenPriceInUsd = task.Result.LastPrice;
                }));
            tasks.Add(_homePageProvider.GetTransactionCountPerLastMinute(req.ChainId).ContinueWith(
                task => { overviewResp.Tps = (task.Result / 60).ToString("F2"); }));

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get home page overview err,chainId:{chainId}", req.ChainId);
        }

        return overviewResp;
    }


    public void SetSearchAddress(SearchResponseDto searchResponseDto, SearchRequestDto requestDto,
        SearchTypes searchType, AddressType addressType)
    {
        var accounts = new List<string>();
        var contracts = new List<SearchContract>();
        searchResponseDto.Accounts = new List<SearchAccount>();
        searchResponseDto.Contracts = contracts;
        try
        {
            if (requestDto.Keyword.Length <= 2)
            {
                return;
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<AddressIndex>, QueryContainer>>();


            mustQuery.Add(q => q.Term(t => t.Field(t => t.AddressType).Value(addressType)));
            if (searchType == SearchTypes.ExactSearch)
            {
                if (CommomHelper.IsValidAddress(requestDto.Keyword))
                {
                    mustQuery.Add(mu => mu.Term(t => t.Field(f => f.LowerAddress).Value(requestDto.Keyword)));
                }
                else
                {
                    mustQuery.Add(mu => mu.Term(t => t.Field(f => f.LowerName).Value(requestDto.Keyword)));
                }
            }

            if (searchType == SearchTypes.FuzzySearch)
            {
                if (requestDto.Keyword.Length > 9)
                {
                    mustQuery.Add(q => q.Bool(b =>
                        b.Should(
                            sh => sh.Wildcard(
                                w => w.Field(f => f.LowerAddress)
                                    .Value($"*{requestDto.Keyword}*")),
                            sh => sh.Wildcard(w => w.Field(f => f.LowerName).Value($"*{requestDto.Keyword}*"))
                        )));
                }
                else
                {
                    mustQuery.Add(q => q.Bool(b =>
                        b.Should(
                            sh => sh.Wildcard(w => w.Field(f => f.LowerName).Value($"*{requestDto.Keyword}*"))
                        )));
                }
            }


            QueryContainer Filter(QueryContainerDescriptor<AddressIndex> f) => f.Bool(b => b.Filter(mustQuery));
            var result = _addressIndexRepository.GetListAsync(Filter, skip: 0, limit: 20,
                index: BlockChainIndexNameHelper.GenerateAddressIndexName(requestDto.ChainId)).Result;
            result.Item2.ForEach(addressIndex =>
            {
                if (addressIndex.AddressType == AddressType.EoaAddress)
                {
                    accounts.Add(addressIndex.Address);
                }
                else
                {
                    var searchContract = new SearchContract();
                    searchContract.Address = addressIndex.Address;
                    searchContract.Name = addressIndex.Name;
                    contracts.Add(searchContract);
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "set search address err");
        }
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


    public async Task<BlocksResponseDto> GetLatestBlocksAsync(LatestBlocksRequestDto requestDto)
    {
        var result = new BlocksResponseDto() { };
        if (!_globalOptions.CurrentValue.ChainIds.Exists(s => s == requestDto.ChainId) ||
            requestDto.MaxResultCount <= 0 ||
            requestDto.MaxResultCount > _globalOptions.CurrentValue.MaxResultCount)
        {
            return result;
        }


        try
        {
            var aElfClient = new AElfClient(_globalOptions.CurrentValue.ChainNodeHosts[requestDto.ChainId]);
            var blockHeightAsync = await aElfClient.GetBlockHeightAsync();


            var blockList = await _aelfIndexerProvider.GetLatestBlocksAsync(requestDto.ChainId,
                blockHeightAsync - requestDto.MaxResultCount,
                blockHeightAsync);


            // var blockList = await _aelfIndexerProvider.GetLatestBlocksAsync(requestDto.ChainId,
            //     100,
            //     300);
            result.Blocks = new List<BlockResponseDto>();
            result.Total = blockList.Count;

            for (var i = blockList.Count - 1; i > 0; i--)
            {
                var indexerBlockDto = blockList[i];
                var latestBlockDto = new BlockResponseDto();

                latestBlockDto.BlockHeight = indexerBlockDto.BlockHeight;
                latestBlockDto.Timestamp = DateTimeHelper.GetTotalSeconds(indexerBlockDto.BlockTime);
                latestBlockDto.TransactionCount = indexerBlockDto.TransactionIds.Count;

                latestBlockDto.TimeSpan = (Convert.ToDouble(0 < blockList.Count
                    ? DateTimeHelper.GetTotalMilliseconds(indexerBlockDto.BlockTime) -
                      DateTimeHelper.GetTotalMilliseconds(blockList[i - 1].BlockTime)
                    : 0) / 1000).ToString("0.0");
                //todo
                latestBlockDto.Reward = "";
                result.Blocks.Add(latestBlockDto);
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetLatestBlocksAsync error");
        }

        return result;
    }


    public async Task<LatestTransactionsResponseSto> GetLatestTransactionsAsync(LatestTransactionsReq req)
    {
        var result = new LatestTransactionsResponseSto();


        result.Transactions = new List<TransactionResponseDto>();
        try
        {
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetLatestTransactionsAsync error");
        }

        return result;
    }

    private CommonAddressDto ConvertAddress(string address) => new() { Address = address };
}