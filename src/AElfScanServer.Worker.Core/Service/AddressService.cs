using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Helper;
using Elasticsearch.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
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

    public Task DeleteMergeBlock();
    
    Task FixTokenHolderAsync();
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

    public async Task FixTokenHolderAsync()
    {
        var redisValue = await _cache.GetAsync(RedisKeyHelper.FixTokenHolder());
        if (redisValue.IsNullOrEmpty())
        {
            _logger.LogInformation("No fixTokenHolder data");
            return;
        }

        var fixTokenHolderInput = JsonConvert.DeserializeObject<FixTokenHolderInput>(redisValue);
        foreach (var symbol in fixTokenHolderInput.SymbolList)
        {
            await SaveTokenHolderAsync(symbol, new List<string>());
        }
        await Task.Delay(3000);
        await SaveMergeTokenList(fixTokenHolderInput.SymbolList);

        await _cache.RemoveAsync(RedisKeyHelper.FixTokenHolder());
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
        var key = "token_transfer_change_time";
        var beginTime = await GetBeginTime(key);
        _logger.LogInformation("PullTokenInfo begin {Time}",
            TimeHelper.GetTimeStampFromDateTimeInSeconds(beginTime).ToString());
        await AddCreatedTokenList(beginTime);
        Dictionary<string, List<string>> symbolMap;
        (symbolMap, beginTime) = await GetChangeSymbolList(beginTime);

        if (beginTime != default && symbolMap.IsNullOrEmpty())
        {
            return;
        }

        await SaveHolderList(beginTime, symbolMap);
        await Task.Delay(3000);
        await SaveMergeTokenList(symbolMap.Keys.ToList());
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

    private async Task<long> GetHolderCountAsync(string symbol)
    {
        var searchResponse = await _elasticClient.CountAsync<AccountTokenIndex>(s => s
            .Index("accounttokenindex")
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Term("token.symbol", symbol),
                        m => m.Range(r => r.Field(f => f.FormatAmount).GreaterThan(0))
                    )
                )
            )
        );
        return searchResponse.Count;
    }

    private async Task<(Dictionary<string, List<string>> symbolList, DateTime beginTime)> GetChangeSymbolList(
        DateTime beginTime)
    {
        DateTime endTime = beginTime;
        var symbolDictionary = new Dictionary<string, List<string>>();
        long skipCount = 0;
        long maxResultCount = 300;
        long queryCount = 0;

        do
        {
            var tokenTransferInput = new TokenTransferInput();
            tokenTransferInput.BeginBlockTime = beginTime;
            tokenTransferInput.OrderInfos = new List<OrderInfo>
            {
                new OrderInfo()
                {
                    Sort = "Asc",
                    OrderBy = "BlockTime"
                }
            };
            tokenTransferInput.SkipCount = skipCount;
            tokenTransferInput.MaxResultCount = maxResultCount;
            tokenTransferInput.Types = new() { SymbolType.Token, SymbolType.Nft };
            var tokenTransferListDto = await _tokenIndexerProvider.GetTokenTransferInfoAsync(tokenTransferInput);


            foreach (var indexerTransferInfoDto in tokenTransferListDto.Items)
            {
                SetSymbolMap(symbolDictionary, indexerTransferInfoDto, indexerTransferInfoDto.Token.Symbol);
                if (SymbolType.Nft == TokenSymbolHelper.GetSymbolType(indexerTransferInfoDto.Token.Symbol))
                {
                    SetSymbolMap(symbolDictionary, indexerTransferInfoDto,
                        TokenSymbolHelper.GetCollectionSymbol(indexerTransferInfoDto.Token.Symbol));
                }
            }

            if (!tokenTransferListDto.Items.IsNullOrEmpty())
            {
                endTime = tokenTransferListDto.Items.Last().Metadata.Block.BlockTime;
            }
            queryCount = tokenTransferListDto.Items.Count;
            skipCount += maxResultCount;

        } while (queryCount == maxResultCount);

        return (symbolDictionary, endTime);
    }

    private static void SetSymbolMap(Dictionary<string, List<string>> symbolList,
        IndexerTransferInfoDto indexerTransferInfoDto, string symbol)
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

    [ExceptionHandler(typeof(Exception),
        Message = "AddCreatedTokenList err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["beginBlockTime"])]
    public virtual async Task AddCreatedTokenList(DateTime beginBlockTime)
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

    
    [ExceptionHandler(typeof(Exception),
        Message = "SaveMergeTokenList err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["symbolList"])]
    public virtual async Task SaveMergeTokenList(List<string> symbolList)
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
                            var holderCount = await GetHolderCountAsync(tokenInfoIndex.Symbol);
                            value.HolderCount = holderCount;
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
                _logger.LogInformation("tokenInfoIndices queryCount:{count},saveCount:{saveCount}", tokenInfoList.Count(),dic.Values.Count);
                skip += maxResultCount;
            }
     
    }


    public virtual async Task SaveHolderList(DateTime beginTime, Dictionary<string, List<string>> symbolMap)
    {
        foreach (var keyValuePair in symbolMap)
        {
            await SaveTokenHolderAsync(keyValuePair.Key, keyValuePair.Value);
        }
    }

    [ExceptionHandler(typeof(Exception),
        Message = "SaveTokenHolderAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["symbol","addressList"])]
    public virtual async Task SaveTokenHolderAsync(string symbol, List<string> addressList)
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
                OrderBy = "Address",
                AmountGreaterThanZero = false
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
                        var flag = value.ChainIds.AddIfNotContains(tokenInfoIndex.ChainId);
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
                _logger.LogInformation(
                    "accountTokenInfoIndices Symbol:{Symbol},queryCount:{count},saveCount:{saveCount}", symbol,
                    tokenInfoList.Count(), dic.Values.Count);
                skip += maxResultCount;
            } while (queryCount == maxResultCount);
       
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
}