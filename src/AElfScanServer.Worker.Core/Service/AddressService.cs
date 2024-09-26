using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Helper;
using Elasticsearch.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using AddressIndex = AElfScanServer.HttpApi.Dtos.AddressIndex;

namespace AElfScanServer.Worker.Core.Service;

public interface IAddressService
{
    Task<(long, List<AddressIndex>)> GetAddressIndexAsync(string chainId, List<string> list);
    Task BulkAddOrUpdateAsync(List<AddressIndex> list);
    Task PatchAddressInfoAsync(string chainId, string address, List<AddressIndex> list);

    public Task PullTokenInfo();
    
    public Task SaveCollectionHolderList();

    public Task DeleteMergeBlock();
}

public class AddressService : IAddressService, ISingletonDependency
{
    private readonly ILogger<AddressService> _logger;
    private readonly INESTRepository<AddressIndex, string> _repository;
    private readonly IDistributedCache<string> _cache;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IEntityMappingRepository<TokenInfoIndex, string> _tokenInfoRepository;
    private readonly IEntityMappingRepository<AccountTokenIndex, string> _accountTokenRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IElasticClient _elasticClient;

    public AddressService(INESTRepository<AddressIndex, string> repository, ITokenIndexerProvider tokenIndexerProvider,
        IDistributedCache<string> cache,
        IEntityMappingRepository<TokenInfoIndex, string> tokenInfoRepository,
        IEntityMappingRepository<AccountTokenIndex, string> accountTokenRepository,
        IObjectMapper objectMapper, IOptionsMonitor<ElasticsearchOptions> options,
        ILogger<AddressService> logger)
    {
        _repository = repository;
        _logger = logger;
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenInfoRepository = tokenInfoRepository;
        _cache = cache;
        _objectMapper = objectMapper;
        _accountTokenRepository = accountTokenRepository;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        _elasticClient = new ElasticClient(settings);
        EsIndex.SetElasticClient(_elasticClient);
    }

    public async Task DeleteMergeBlock()
    {
        var twoDaysAgo = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds();
        var deleteResponse = _elasticClient.DeleteByQuery<BlockIndex>(del => del
            .Index("blockindex")
            .Query(q => q
                .Range(r => r
                    .Field("timestamp")
                    .LessThanOrEquals(twoDaysAgo)
                )
            )
        );

        if (deleteResponse.IsValid)
        {
            _logger.LogInformation("DeleteMergeBlock: {Count}", deleteResponse.Total);
        }
    }

    public async Task<(long, List<AddressIndex>)> GetAddressIndexAsync(string chainId, List<string> addressList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AddressIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Address).Terms(addressList)));
        mustQuery.Add(q => !q.Exists(e => e.Field(f => f.Name)));

        QueryContainer Filter(QueryContainerDescriptor<AddressIndex> f) => f.Bool(b => b.Must(mustQuery));

        var (total, result) = await _repository.GetListAsync(Filter, index: GenerateIndexName(chainId));
        return (total, result);
    }

    public async Task BulkAddOrUpdateAsync(List<AddressIndex> list) => await _repository.BulkAddOrUpdateAsync(list);

    public async Task PatchAddressInfoAsync(string id, string chainId, List<AddressIndex> list)
    {
        var addressIndex = await _repository.GetAsync(id, GenerateIndexName(chainId));

        if (addressIndex != null) return;

        list.Add(new AddressIndex
        {
            Id = id,
            Address = id,
            LowerAddress = id.ToLower()
        });
    }

    private string GenerateIndexName(string chainId) => BlockChainIndexNameHelper.GenerateAddressIndexName(chainId);


    public async Task PullTokenInfo()
    {
        var key = "token_transfer_change";
        var beginTime = await GetBeginTime(key);
        _logger.LogInformation("PullTokenInfo bengin {Time}",
            TimeHelper.GetTimeStampFromDateTimeInSeconds(beginTime).ToString());
        await AddCreatedTokenList(beginTime);
        Dictionary<string, List<string>> symbolMap;
        (symbolMap, beginTime) = await GetChangeSymbolList(beginTime);

        if (beginTime != default && symbolMap.IsNullOrEmpty())
        {
            return;
        }

        await SaveMergeTokenList(symbolMap.Keys.ToList());
        await SaveHolderList(beginTime, symbolMap);
        if (beginTime == default)
        {
            beginTime = DateTime.UtcNow.AddHours(-1);
        }

        await _cache.SetAsync(key, TimeHelper.GetTimeStampFromDateTimeInSeconds(beginTime).ToString(),
            new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3)
            });
        _logger.LogInformation("PullTokenInfo end {Time}",
            TimeHelper.GetTimeStampFromDateTimeInSeconds(beginTime).ToString());
    }

    private async Task<(Dictionary<string, List<string>> symbolList, DateTime beginTime)> GetChangeSymbolList(
        DateTime beginTime)
    {
        var symbolDictionary = new Dictionary<string, List<string>>();
        while (beginTime != default)
        {
            var tokenTransferInput = new TokenTransferInput();
            tokenTransferInput.BeginBlockTime = beginTime;
            tokenTransferInput.OrderInfos = new List<OrderInfo>()
            {
                new OrderInfo()
                {
                    Sort = "Asc",
                    OrderBy = "BlockHeight"
                }
            };
            tokenTransferInput.SkipCount = 0;
            tokenTransferInput.MaxResultCount = 1000;

            var tokenTransferListDto = await _tokenIndexerProvider.GetTokenTransferInfoAsync(tokenTransferInput);
            if (tokenTransferListDto.Items.Count == 0)
            {
                break;
            }

            foreach (var indexerTransferInfoDto in tokenTransferListDto.Items)
            {
                SetSymbolMap(symbolDictionary, indexerTransferInfoDto,indexerTransferInfoDto.Token.Symbol);
                if (SymbolType.Nft == TokenSymbolHelper.GetSymbolType(indexerTransferInfoDto.Token.Symbol))
                {
                    SetSymbolMap(symbolDictionary, indexerTransferInfoDto,TokenSymbolHelper.GetCollectionSymbol(indexerTransferInfoDto.Token.Symbol));
                }
            }

            beginTime = tokenTransferListDto.Items.Last().Metadata.Block.BlockTime;
        }

        return (symbolDictionary, beginTime);
    }

    private static void SetSymbolMap(Dictionary<string, List<string>> symbolList,
        IndexerTransferInfoDto indexerTransferInfoDto,string symbol)
    {
        var addressList = symbolList.GetValueOrDefault(symbol,
            new List<string>());
        if (!addressList.Contains(indexerTransferInfoDto.From))
        {
            addressList.Add(indexerTransferInfoDto.From);
        }

        if (!addressList.Contains(indexerTransferInfoDto.To))
        {
            addressList.Add(indexerTransferInfoDto.To);
        }

        symbolList[symbol] = addressList;
    }

    private async Task AddCreatedTokenList(DateTime beginBlockTime)
    {
        try
        {
            if (beginBlockTime == default)
            {
                return;
            }

            var skip = 0;
            var maxResultCount = 1000;
            var tokenListInput = new TokenListInput()
            {
                Types = new List<SymbolType>() { SymbolType.Token, SymbolType.Nft_Collection, SymbolType.Nft },
                BeginBlockTime = beginBlockTime,
                SkipCount = skip,
                MaxResultCount = maxResultCount,
                Sort = "Desc",
                OrderBy = "Symbol"
            };
            while (true)
            {
                tokenListInput.SkipCount = skip;
                var tokenListAsync = await _tokenIndexerProvider.GetTokenListAsync(tokenListInput);
                if (tokenListAsync.Items.Count == 0)
                {
                    break;
                }

                var tokenInfoList =
                    _objectMapper.Map<List<IndexerTokenInfoDto>, List<TokenInfoIndex>>(tokenListAsync.Items);


                foreach (var tokenInfoIndex in tokenInfoList)
                {
                    tokenInfoIndex.ChainIds.Add(tokenInfoIndex.ChainId);
                }

                await _tokenInfoRepository.AddOrUpdateManyAsync(tokenInfoList);
                _logger.LogInformation("tokenInfoIndices count:{count}", tokenInfoList.Count());
                skip += maxResultCount;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PullTokenInfo error");
        }
    }

    private async Task SaveMergeTokenList(List<string> symbolList)
    {
        try
        {
            var skip = 0;
            var maxResultCount = 1000;
            TokenInfoIndex lastTokenIndex = null;
            var tokenListInput = new TokenListInput()
            {
                Types = new List<SymbolType>() { SymbolType.Token, SymbolType.Nft_Collection, SymbolType.Nft },
                Symbols = symbolList,
                SkipCount = skip,
                MaxResultCount = maxResultCount,
                Sort = "Desc",
                OrderBy = "Symbol"
            };
            while (true)
            {
                tokenListInput.SkipCount = skip;
                var tokenListAsync = await _tokenIndexerProvider.GetTokenListAsync(tokenListInput);
                if (tokenListAsync.Items.Count == 0)
                {
                    break;
                }

                var tokenInfoList =
                    _objectMapper.Map<List<IndexerTokenInfoDto>, List<TokenInfoIndex>>(tokenListAsync.Items);

                if (lastTokenIndex != null)
                {
                    if (tokenInfoList.Exists(o => o.Symbol == lastTokenIndex.Symbol))
                    {
                        tokenInfoList.Add(lastTokenIndex);
                    }
                }

                lastTokenIndex = tokenInfoList.Last();

                var dic = new Dictionary<string, TokenInfoIndex>();
                foreach (var tokenInfoIndex in tokenInfoList)
                {
                    if (dic.TryGetValue(tokenInfoIndex.Symbol, out var value))
                    {
                        var flag = value.ChainIds.AddIfNotContains(tokenInfoIndex.ChainId);
                        if (flag)
                        {
                            value.HolderCount += tokenInfoIndex.HolderCount;
                            value.TransferCount += tokenInfoIndex.TransferCount;
                            value.ItemCount += tokenInfoIndex.ItemCount;
                            value.Supply += tokenInfoIndex.Supply;
                        }
                    }
                    else
                    {
                        tokenInfoIndex.ChainIds.Add(tokenInfoIndex.ChainId);
                        dic.Add(tokenInfoIndex.Symbol, tokenInfoIndex);
                    }
                }

                await _tokenInfoRepository.AddOrUpdateManyAsync(dic.Values.ToList());
                _logger.LogInformation("tokenInfoIndices count:{count}", tokenInfoList.Count());
                skip += maxResultCount;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PullTokenInfo error");
        }
    }

    private async Task SaveHolderList(DateTime beginTime, Dictionary<string, List<string>> symbolMap)
    {
        if (beginTime == default)
        {
            await Task.Delay(3000);
            var queryCount = 0;
            var limit = 1000;
            var skip = 0;
            do
            {
                var searchResponse = _elasticClient.Search<TokenInfoIndex>(s => s
                    .Index("tokeninfoindex")
                    .Sort(sort => sort
                        .Field(f => f
                            .Field(c => c.Symbol)
                            .Order(SortOrder.Ascending)
                        )
                    )
                    .From(skip)
                    .Size(limit)
                );
                var tokenList = searchResponse.Documents.ToList();
                foreach (var item in tokenList)
                {
                    await SaveTokenHolderAsync(item.Symbol, new List<string>());
                }

                queryCount = tokenList.Count;
                skip += limit;
            } while (queryCount == limit);
        }
        else
        {
            foreach (var keyValuePair in symbolMap)
            {
                await SaveTokenHolderAsync(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }

    private async Task SaveTokenHolderAsync(string symbol, List<string> addressList)
    {
        try
        {
            var skip = 0;
            var maxResultCount = 1000;
            var queryCount = 0;
            AccountTokenIndex lastTokenIndexIndex = null;
            var tokenHolder = new TokenHolderInput()
            {
                Types = new List<SymbolType>() { SymbolType.Token, SymbolType.Nft },
                Symbol = symbol,
                AddressList = addressList.Count > 100 ? new List<string>() : addressList,
                SkipCount = skip,
                MaxResultCount = maxResultCount,
                Sort = "Asc",
                OrderBy = "Address"
            };

            do
            {
                tokenHolder.SkipCount = skip;
                IndexerTokenHolderInfoListDto tokenListAsync = null;
                if (SymbolType.Nft_Collection == TokenSymbolHelper.GetSymbolType(tokenHolder.Symbol))
                {
                    tokenHolder.CollectionSymbol = tokenHolder.Symbol;
                    tokenListAsync = await _tokenIndexerProvider.GetCollectionHolderInfoAsync(tokenHolder);
                }
                else
                {
                    tokenListAsync = await _tokenIndexerProvider.GetTokenHolderInfoAsync(tokenHolder);
                }

                queryCount = tokenListAsync.Items.Count;
                if (queryCount == 0)
                {
                    break;
                }

                var tokenInfoList =
                    _objectMapper.Map<List<IndexerTokenHolderInfoDto>, List<AccountTokenIndex>>(tokenListAsync.Items);

                if (lastTokenIndexIndex != null)
                {
                    if (tokenInfoList.Exists(o => o.Address == lastTokenIndexIndex.Address))
                    {
                        tokenInfoList.Add(lastTokenIndexIndex);
                    }
                }

                lastTokenIndexIndex = tokenInfoList.Last();

                var dic = new Dictionary<string, AccountTokenIndex>();
                foreach (var tokenInfoIndex in tokenInfoList)
                {
                    if (dic.TryGetValue(tokenInfoIndex.Address, out var value))
                    {
                        var flag= value.ChainIds.AddIfNotContains(tokenInfoIndex.ChainId);
                        if (flag)
                        {
                            value.TransferCount += tokenInfoIndex.TransferCount;
                            value.Amount += tokenInfoIndex.Amount;
                            value.FormatAmount += tokenInfoIndex.FormatAmount;
                        }
                    }
                    else
                    {
                        tokenInfoIndex.ChainIds.AddIfNotContains(tokenInfoIndex.ChainId);
                        dic.Add(tokenInfoIndex.Address, tokenInfoIndex);
                    }
                }

                await _accountTokenRepository.AddOrUpdateManyAsync(dic.Values.ToList());
                _logger.LogInformation("accountTokenInfoIndices Symbol:{Symbol},count:{count}", symbol,
                    tokenInfoList.Count());
                skip += maxResultCount;
            } while (queryCount == maxResultCount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PullTokenInfo error");
        }
    }

    private async Task<DateTime> GetBeginTime(string key)
    {
        var datetime = await _cache.GetAsync(key);
        DateTime beginDate = default;
        if (datetime != null && long.TryParse(datetime, out var dateLong))
        {
            beginDate = DateTimeOffset.FromUnixTimeSeconds(dateLong).DateTime;
        }
        return beginDate;
    }
    
    
    public async Task SaveCollectionHolderList()
    {
            var queryCount = 0;
            var limit = 1000;
            var skip = 0;
            do
            {
                var searchResponse = _elasticClient.Search<TokenInfoIndex>(s => s
                    .Index("tokeninfoindex")
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m =>
                                {
                                    return m.Term(t => t
                                        .Field(f => f.Type).Value(SymbolType.Nft_Collection)
                                    );
                                })
                        )
                    )
                    .Sort(sort => sort
                        .Field(f => f
                            .Field(c => c.Symbol)
                            .Order(SortOrder.Ascending)
                        )
                    )
                    .From(skip)
                    .Size(limit)
                );
                var tokenList = searchResponse.Documents.ToList();
                foreach (var item in tokenList)
                {
                    await SaveTokenHolderAsync(item.Symbol, new List<string>());
                }

                queryCount = tokenList.Count;
                skip += limit;
            } while (queryCount == limit);
    }
}