using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.Worker.Core.Service;

public interface IAddressService
{
    Task<(long, List<AddressIndex>)> GetAddressIndexAsync(string chainId, List<string> list);
    Task BulkAddOrUpdateAsync(List<AddressIndex> list);
    Task PatchAddressInfoAsync(string chainId, string address, List<AddressIndex> list);
    
    public Task PullTokenInfo();
}

public class AddressService : IAddressService, ITransientDependency
{
    private readonly ILogger<AddressService> _logger;
    private readonly INESTRepository<AddressIndex, string> _repository;
    private readonly IDistributedCache<string> _cache;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IEntityMappingRepository<TokenInfoIndex, string> _tokenInfoRepository;
    private readonly IObjectMapper _objectMapper;

    public AddressService(INESTRepository<AddressIndex, string> repository,  ITokenIndexerProvider tokenIndexerProvider,
        IDistributedCache<string> cache,
        IEntityMappingRepository<TokenInfoIndex, string> tokenInfoRepository,
        IObjectMapper objectMapper,
        ILogger<AddressService> logger)
    {
        _repository = repository;
        _logger = logger;
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenInfoRepository = tokenInfoRepository;
        _cache = cache;
        _objectMapper = objectMapper;
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
         (var symbolMap, beginTime) = await GetChangeSymbolMap(beginTime);

         if (beginTime != default && symbolMap.IsNullOrEmpty())
         {
             return;
         }
         await SaveMergeTokenList(symbolMap.Keys.ToList());
         if (beginTime == default)
         {
             beginTime = DateTime.UtcNow.AddHours(-1);
         }

         await _cache.SetAsync(key,TimeHelper.GetTimeStampFromDateTimeInSeconds(beginTime).ToString(),new DistributedCacheEntryOptions()
         {
             AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3)
         });
        _logger.LogInformation("PullTokenInfo end {Time}",TimeHelper.GetTimeStampFromDateTimeInSeconds(beginTime).ToString());
    }

     private async Task<(Dictionary<string,List<string>> symbolList, DateTime beginTime)> GetChangeSymbolMap(DateTime beginTime)
     {
         var symbolList = new Dictionary<string,List<string>>();
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
               var addresList = symbolList.GetValueOrDefault(indexerTransferInfoDto.Token.Symbol,
                   new List<string>());
               if (addresList.Exists(o=> o == indexerTransferInfoDto.From))
               {
                   addresList.Add(indexerTransferInfoDto.From);
               }
               if (addresList.Exists(o=> o == indexerTransferInfoDto.To))
               {
                   addresList.Add(indexerTransferInfoDto.To);
               }

               symbolList[indexerTransferInfoDto.Token.Symbol] = addresList;
             }
             beginTime = tokenTransferListDto.Items.Last().Metadata.Block.BlockTime;
         }

         return (symbolList, beginTime);
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
                 Types = new List<SymbolType>() { SymbolType.Token,SymbolType.Nft_Collection,SymbolType.Nft},
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
                         value.ChainIds.Add(tokenInfoIndex.ChainId);
                         value.HolderCount += tokenInfoIndex.HolderCount;
                         value.TransferCount += tokenInfoIndex.TransferCount;
                         value.TransferCount += tokenInfoIndex.TransferCount;
                         value.Supply += tokenInfoIndex.Supply;
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
     

     private async Task<DateTime> GetBeginTime(string key)
     {
        
         var datetime = await _cache.GetAsync(key);
         DateTime beginDate = default;
         if (datetime != null && long.TryParse(datetime,out var dateLong))
         {
             beginDate = DateTimeOffset.FromUnixTimeSeconds(dateLong).DateTime;
         }
         return beginDate;
     }
}