using System.Globalization;
using AElf.ExceptionHandler;
using AElfScanServer.Common;
using AElfScanServer.Common.Commons;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.Domain.Shared.Common;
using Elasticsearch.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Nito.AsyncEx;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFT.backend;

public interface INftService
{
    public Task<ListResponseDto<NftInfoDto>> GetNftCollectionListAsync(TokenListInput input);
    public Task<NftDetailDto> GetNftCollectionDetailAsync(string chainId, string collectionSymbol);
    public Task<NftTransferInfosDto> GetNftCollectionTransferInfosAsync(TokenTransferInput input);
    public Task<ListResponseDto<TokenHolderInfoDto>> GetNftCollectionHolderInfosAsync(TokenHolderInput input);
    public Task<NftInventorysDto> GetNftCollectionInventoryAsync(NftInventoryInput input);
    Task<NftItemDetailDto> GetNftItemDetailAsync(string chainId, string symbol);
    Task<ListResponseDto<NftItemActivityDto>> GetNftItemActivityAsync(NftItemActivityInput input);
    Task<ListResponseDto<NftItemHolderInfoDto>> GetNftItemHoldersAsync(NftItemHolderInfoInput input);

    Task<NftDetailDto> GetMergeNftCollectionDetailAsync(string collectionSymbol, string chainId);

    Task<Dictionary<string, string>> GetCollectionSupplyAsync(string chainId, List<string> collectionSymbols);
}

public class NftService : INftService, ISingletonDependency
{
    private const int MaxResultCount = 1000;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;

    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly ILogger<NftService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INftCollectionHolderProvider _collectionHolderProvider;
    private readonly INftInfoProvider _nftInfoProvider;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IGenesisPluginProvider _genesisPluginProvider;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptionsMonitor;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IElasticClient _elasticClient;


    public NftService(ITokenIndexerProvider tokenIndexerProvider, ILogger<NftService> logger,
        IObjectMapper objectMapper,
        INftCollectionHolderProvider collectionHolderProvider, INftInfoProvider nftInfoProvider,
        ITokenPriceService tokenPriceService,
        IOptionsMonitor<ChainOptions> chainOptions, IOptionsMonitor<TokenInfoOptions> tokenInfoOptionsMonitor,
        ITokenInfoProvider tokenInfoProvider, IGenesisPluginProvider genesisPluginProvider,
        IDistributedCache<string> distributedCache, IMemoryCache memoryCache,
        IOptionsMonitor<ElasticsearchOptions> options, IOptionsMonitor<GlobalOptions> globalOptions
    )
    {
        _tokenIndexerProvider = tokenIndexerProvider;
        _logger = logger;
        _objectMapper = objectMapper;
        _collectionHolderProvider = collectionHolderProvider;
        _nftInfoProvider = nftInfoProvider;
        _tokenPriceService = tokenPriceService;
        _chainOptions = chainOptions;
        _tokenInfoOptionsMonitor = tokenInfoOptionsMonitor;
        _tokenInfoProvider = tokenInfoProvider;
        _genesisPluginProvider = genesisPluginProvider;
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        _elasticClient = new ElasticClient(settings);
        EsIndex.SetElasticClient(_elasticClient);
        _globalOptions = globalOptions;
    }


    public async Task<ListResponseDto<NftInfoDto>> GetNftCollectionListAsync(TokenListInput input)
    {
        if (input.ChainId.IsNullOrEmpty())
        {
            return await GetMergeNftCollectionListAsync(input);
        }

        input.SetDefaultSort();
        input.Types = new List<SymbolType> { SymbolType.Nft_Collection };
        var indexerNftListDto = await _tokenIndexerProvider.GetTokenListAsync(input);
        if (indexerNftListDto.Items.IsNullOrEmpty())
        {
            return new ListResponseDto<NftInfoDto>();
        }

        var collectionSymbols = indexerNftListDto.Items.Select(o => o.Symbol).ToList();
        //  var groupAndSumSupply = await GetCollectionSupplyAsync(input.ChainId, collectionSymbols);

        //get collection supply
        var list = indexerNftListDto.Items.Select(item =>
        {
            var nftInfoDto = _objectMapper.Map<IndexerTokenInfoDto, NftInfoDto>(item);
            nftInfoDto.Items = item.ItemCount.ToString(CultureInfo.InvariantCulture);

            //convert url
            nftInfoDto.NftCollection.ImageUrl = TokenInfoHelper.GetImageUrl(item.ExternalInfo,
                () => _tokenInfoProvider.BuildImageUrl(item.Symbol));
            /*nftInfoDto.Items = groupAndSumSupply.TryGetValue(item.Symbol, out var sumSupply)
                ? sumSupply
                : "0";*/
            nftInfoDto.ChainIds.Add(input.ChainId);
            return nftInfoDto;
        }).ToList();
        return new ListResponseDto<NftInfoDto>
        {
            Total = indexerNftListDto.TotalCount,
            List = list
        };
    }

    public async Task<ListResponseDto<NftInfoDto>> GetMergeNftCollectionListAsync(TokenListInput input)
    {
        var result = await EsIndex.SearchMergeTokenList(
            (int)input.SkipCount, (int)input.MaxResultCount, input.OrderBy == null ? "desc" : input.OrderBy.ToLower(),
            null, null,
            SymbolType.Nft_Collection);


        //get collection supply
        var list = result.list.Select(item =>
        {
            var nftInfoDto = _objectMapper.Map<TokenInfoIndex, NftInfoDto>(item);

            nftInfoDto.ChainIds = item.ChainIds;
            nftInfoDto.Items = item.ItemCount.ToString(CultureInfo.InvariantCulture);

            //convert url
            nftInfoDto.NftCollection.ImageUrl = TokenInfoHelper.GetImageUrl(item.ExternalInfo,
                () => _tokenInfoProvider.BuildImageUrl(item.Symbol));
            /*nftInfoDto.Items = groupAndSumSupply.TryGetValue(item.Symbol, out var sumSupply)
                ? sumSupply
                : "0";*/
            return nftInfoDto;
        }).ToList();
        return new ListResponseDto<NftInfoDto>
        {
            Total = result.totalCount,
            List = list
        };
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetNftCollectionDetailAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId","collectionSymbol"])]
    public virtual async Task<NftDetailDto> GetNftCollectionDetailAsync(string chainId, string collectionSymbol)
    {
            var getCollectionInfoTask = _tokenIndexerProvider.GetTokenDetailAsync(chainId, collectionSymbol);
            var nftCollectionInfoInput = new GetNftCollectionInfoInput
            {
                ChainId = _globalOptions.CurrentValue.SideChainId,
                CollectionSymbolList = new List<string> { collectionSymbol }
            };
            var nftCollectionInfoTask = _nftInfoProvider.GetNftCollectionInfoAsync(nftCollectionInfoInput);


            await Task.WhenAll(getCollectionInfoTask, nftCollectionInfoTask);

            var collectionInfoDtos = await getCollectionInfoTask;
            AssertHelper.NotEmpty(collectionInfoDtos, "this nft not exist");
            var collectionInfo = collectionInfoDtos[0];
            var nftDetailDto = _objectMapper.Map<IndexerTokenInfoDto, NftDetailDto>(collectionInfo);
            nftDetailDto.Items = collectionInfo.ItemCount.ToString(CultureInfo.InvariantCulture);

            nftDetailDto.TokenContractAddress = _chainOptions.CurrentValue.GetChainInfo(chainId)?.TokenContractAddress;
            nftDetailDto.ContractAddress = new CommonAddressDto()
            {
                Address = _chainOptions.CurrentValue.GetChainInfo(chainId)?.TokenContractAddress,
                Name = collectionInfo.Symbol,
                AddressType = AddressType.ContractAddress
            };

            if (_globalOptions.CurrentValue.ContractNames.TryGetValue(chainId, out var contractNames))
            {
                if (contractNames.TryGetValue(nftDetailDto.TokenContractAddress, out var contractName))
                {
                    nftDetailDto.ContractAddress.Name = contractName;
                }
            }

            //collectionInfo.Symbol is xxx-0
            nftDetailDto.NftCollection.ImageUrl = TokenInfoHelper.GetImageUrl(collectionInfo.ExternalInfo,
                () => _tokenInfoProvider.BuildImageUrl(collectionInfo.Symbol));
            /*nftDetailDto.Items = (await groupAndSumSupplyTask).TryGetValue(collectionInfo.Symbol, out var sumSupply)
                ? sumSupply
                : "0";*/
            //of floor price
            var nftCollectionInfo = await nftCollectionInfoTask;
            if (nftCollectionInfo.TryGetValue(collectionSymbol, out var nftCollection))
            {
                var priceDto =
                    await _tokenPriceService.GetTokenPriceAsync(nftCollection.FloorPriceSymbol,
                        CurrencyConstant.UsdCurrency);
                nftDetailDto.FloorPrice = nftCollection.FloorPrice;
                nftDetailDto.FloorPriceOfUsd =
                    Math.Round(nftCollection.FloorPrice * priceDto.Price, CommonConstant.UsdPriceValueDecimals);
            }
            else
            {
                nftDetailDto.FloorPrice = -1m;
            }

            return nftDetailDto;
    

    }


    public async Task<NftDetailDto> GetMergeNftCollectionDetailAsync(string collectionSymbol, string chainId)
    {
        var tasks = new List<Task>();

        var nftDetailDto = new NftDetailDto();
        var mainNftDetailDto = new NftDetailDto();
        var sideNftDetailDto = new NftDetailDto();
        var mergeHolders = 0l;

        tasks.Add(GetNftCollectionDetailAsync("AELF", collectionSymbol).ContinueWith(task =>
        {
            mainNftDetailDto = task.Result == null ? new NftDetailDto() : task.Result;
        }));

        tasks.Add(EsIndex.GetTokenHolders(collectionSymbol, "").ContinueWith(task => { mergeHolders = task.Result; }));
        tasks.Add(GetNftCollectionDetailAsync(_globalOptions.CurrentValue.SideChainId, collectionSymbol)
            .ContinueWith(task => { sideNftDetailDto = task.Result == null ? new NftDetailDto() : task.Result; }));

        await tasks.WhenAll();

        if (chainId == "AELF" || chainId.IsNullOrEmpty())
        {
            nftDetailDto = mainNftDetailDto;
        }
        else
        {
            nftDetailDto = sideNftDetailDto;
        }

        nftDetailDto.MainChainItems = mainNftDetailDto.Items;
        nftDetailDto.SideChainItems = sideNftDetailDto.Items;

        nftDetailDto.MergeItems =
            (decimal.Parse(nftDetailDto.MainChainItems.IsNullOrEmpty() ? "0" : nftDetailDto.MainChainItems) +
             decimal.Parse(nftDetailDto.SideChainItems.IsNullOrEmpty() ? "0" : nftDetailDto.SideChainItems)).ToString();
        nftDetailDto.MainChainHolders = mainNftDetailDto.Holders;
        nftDetailDto.SideChainHolders = sideNftDetailDto.Holders;


        nftDetailDto.MainChainTransferCount = mainNftDetailDto.TransferCount;
        nftDetailDto.SideChainTransferCount = sideNftDetailDto.TransferCount;
        nftDetailDto.MergeTransferCount = nftDetailDto.MainChainTransferCount + nftDetailDto.SideChainTransferCount;

        nftDetailDto.MainChainFloorPrice = mainNftDetailDto.FloorPrice;
        nftDetailDto.SideChainFloorPrice = sideNftDetailDto.FloorPrice;

        nftDetailDto.MainChainFloorPriceOfUsd = mainNftDetailDto.FloorPriceOfUsd;
        nftDetailDto.SideChainFloorPriceOfUsd = sideNftDetailDto.FloorPriceOfUsd;

        nftDetailDto.MergeHolders = mergeHolders;
        if (!mainNftDetailDto.Items.IsNullOrEmpty())
        {
            nftDetailDto.ChainIds.Add("AELF");
        }

        if (!sideNftDetailDto.Items.IsNullOrEmpty())
        {
            nftDetailDto.ChainIds.Add(_globalOptions.CurrentValue.SideChainId);
        }


        return nftDetailDto;
    }


    public async Task<NftTransferInfosDto> GetNftCollectionTransferInfosAsync(TokenTransferInput input)
    {
        var types = new List<SymbolType> { SymbolType.Nft };
        input.Types = types;
        var tokenTransferInfos = await _tokenIndexerProvider.GetTokenTransfersAsync(input);
        var result = new NftTransferInfosDto
        {
            Total = tokenTransferInfos.Total,
            List = _objectMapper.Map<List<TokenTransferInfoDto>, List<NftTransferInfoDto>>(tokenTransferInfos.List)
        };
        if (input.IsSearchAddress())
        {
            result.IsAddress = true;
            result.Items = await _tokenIndexerProvider.GetHolderInfoAsync(input.ChainId, input.Search, types);
        }

        return result;
    }

    public async Task<ListResponseDto<TokenHolderInfoDto>> GetNftCollectionHolderInfosAsync(TokenHolderInput input)
    {
        if (input.ChainId.IsNullOrEmpty())
        {
            return await GetMergeNftCollectionHolderInfosAsync(input);
        }

        input.SetDefaultSort();

        var indexerTokenHolderInfo = await _tokenIndexerProvider.GetCollectionHolderInfoAsync(input);

        var list = await ConvertIndexerNftHolderInfoDtoAsync(indexerTokenHolderInfo.Items, input.ChainId,
            input.CollectionSymbol);

        return new ListResponseDto<TokenHolderInfoDto>
        {
            Total = indexerTokenHolderInfo.TotalCount,
            List = list
        };
    }

    public async Task<ListResponseDto<TokenHolderInfoDto>> GetMergeNftCollectionHolderInfosAsync(TokenHolderInput input)
    {
        input.SetDefaultSort();

        var result = new ListResponseDto<TokenHolderInfoDto>();

        var tasks = new List<Task>();
        input.Symbol = input.CollectionSymbol;
        var accountTokenIndices = new List<AccountTokenIndex>();
        var list = new List<TokenHolderInfoDto>();
        var totalCount = 0L;

        tasks.Add(EsIndex.SearchAccountList(input).ContinueWith(task =>
        {
            accountTokenIndices = task.Result.list;
            totalCount = task.Result.totalCount;
        }));


        var indexerTokenList = new List<IndexerTokenInfoDto>();

        tasks.Add(_tokenIndexerProvider.GetTokenDetailAsync("", input.CollectionSymbol).ContinueWith(task =>
        {
            indexerTokenList = task.Result;
        }));


        await tasks.WhenAll();

        if (indexerTokenList.IsNullOrEmpty())
        {
            return result;
        }

        var tokenSupply = indexerTokenList.Sum(c => c.ItemCount);
        _logger.LogInformation("nft supply :{supply},{symbol}", tokenSupply, input.CollectionSymbol);
        var addressList = accountTokenIndices
            .Where(value => !string.IsNullOrEmpty(value.Address))
            .Select(value => value.Address).Distinct().ToList();

        var priceDtoTask = _tokenPriceService.GetTokenPriceAsync(input.Symbol, CurrencyConstant.UsdCurrency);
        var contractInfoDictTask = _genesisPluginProvider.GetContractListAsync("", addressList);

        await Task.WhenAll(priceDtoTask, contractInfoDictTask);


        var priceDto = await priceDtoTask;
        var contractInfoDict = await contractInfoDictTask;
        foreach (var indexerTokenHolderInfoDto in accountTokenIndices)
        {
            var tokenHolderInfoDto = new TokenHolderInfoDto();
            tokenHolderInfoDto.Quantity = indexerTokenHolderInfoDto.FormatAmount;

            tokenHolderInfoDto.Address = CommonAddressHelper.GetCommonAddress(indexerTokenHolderInfoDto.Address,
                indexerTokenHolderInfoDto.ChainId, contractInfoDict, _globalOptions.CurrentValue.ContractNames);


            if (tokenSupply != 0)
            {
                tokenHolderInfoDto.Percentage =
                    Math.Round((decimal)indexerTokenHolderInfoDto.FormatAmount / tokenSupply * 100,
                        CommonConstant.PercentageValueDecimals);
            }

            tokenHolderInfoDto.Value =
                Math.Round(indexerTokenHolderInfoDto.FormatAmount * priceDto.Price, CommonConstant.UsdValueDecimals);
            tokenHolderInfoDto.ChainIds = indexerTokenHolderInfoDto.ChainIds;
            _logger.LogInformation("nft holder info chainIds :{chainIds}", indexerTokenHolderInfoDto.ChainIds);
            list.Add(tokenHolderInfoDto);
        }


        return new ListResponseDto<TokenHolderInfoDto>
        {
            Total = totalCount,
            List = list
        };
    }

    public async Task<NftInventorysDto> GetNftCollectionInventoryAsync(NftInventoryInput input)
    {
        if (input.ChainId.IsNullOrEmpty())
        {
            return await GetMergeNftCollectionInventoryAsync(input);
        }

        var result = new NftInventorysDto();
        List<IndexerTokenInfoDto> indexerTokenInfoList;
        long totalCount;
        if (input.IsSearchAddress())
        {
            var tokenHolderInput = _objectMapper.Map<NftInventoryInput, TokenHolderInput>(input);
            tokenHolderInput.Types = new List<SymbolType> { SymbolType.Nft };
            var tokenHolderInfos = await _tokenIndexerProvider.GetTokenHolderInfoAsync(tokenHolderInput);
            var symbols = tokenHolderInfos.Items.Select(i => i.Token.Symbol).ToList();
            var tokenListInput = new TokenListInput()
            {
                ChainId = input.ChainId,
                Symbols = symbols,
                Types = new List<SymbolType> { SymbolType.Nft }
            };
            tokenListInput.OfOrderInfos((SortField.BlockHeight, SortDirection.Desc));
            indexerTokenInfoList = await _tokenIndexerProvider.GetAllTokenInfosAsync(tokenListInput);
            result.IsAddress = true;
            result.Items = tokenHolderInfos.Items.Select(i => new HolderInfo
            {
                Balance = i.FormatAmount, Symbol = i.Token.Symbol
            }).ToList();
            totalCount = tokenHolderInfos.TotalCount;
        }
        else
        {
            var tokenListInput = _objectMapper.Map<NftInventoryInput, TokenListInput>(input);
            tokenListInput.CollectionSymbols = new List<string> { input.CollectionSymbol };
            tokenListInput.Types = new List<SymbolType> { SymbolType.Nft };
            tokenListInput.OfOrderInfos((SortField.BlockHeight, SortDirection.Desc));
            tokenListInput.Search = "";
            tokenListInput.ExactSearch = input.Search;
            var indexerTokenInfoListDto = await _tokenIndexerProvider.GetTokenListAsync(tokenListInput);
            totalCount = indexerTokenInfoListDto.TotalCount;
            indexerTokenInfoList = indexerTokenInfoListDto.Items;
        }

        var list = await ConvertIndexerNftInventoryDtoAsync(indexerTokenInfoList, input.ChainId);
        result.Total = totalCount;
        result.List = list;
        return result;
    }


    public async Task<NftInventorysDto> GetMergeNftCollectionInventoryAsync(NftInventoryInput input)
    {
        var result = new NftInventorysDto();

        var tokenListInput = _objectMapper.Map<NftInventoryInput, TokenListInput>(input);
        tokenListInput.CollectionSymbols = new List<string> { input.CollectionSymbol };
        tokenListInput.Types = new List<SymbolType> { SymbolType.Nft };
        tokenListInput.OfOrderInfos((SortField.BlockHeight, SortDirection.Desc));
        tokenListInput.Search = "";
        tokenListInput.ExactSearch = input.Search;

        var tokenInfoResult = await EsIndex.SearchMergeTokenList((int)input.SkipCount, (int)input.MaxResultCount,
            "desc", input.Search.IsNullOrEmpty() ? new List<string>() { } : new List<string>() { input.Search }, null,
            SymbolType.Nft, input.CollectionSymbol);


        var list = await ConvertMergeNftInventoryDtoAsync(tokenInfoResult.list);
        result.Total = tokenInfoResult.totalCount;
        result.List = list;
        return result;
    }

    public async Task<NftItemDetailDto> GetNftItemDetailAsync(string chainId, string symbol)
    {
        if (chainId.IsNullOrEmpty())
        {
            return await GetMergeNftItemDetailAsync(symbol);
        }

        var nftItems = await _tokenIndexerProvider.GetTokenDetailAsync(chainId, symbol);

        AssertHelper.NotEmpty(nftItems, "this nft item not exist");
        var nftItem = nftItems[0];
        var collectionInfos = await _tokenIndexerProvider.GetTokenDetailAsync(chainId, nftItem.CollectionSymbol);
        AssertHelper.NotEmpty(collectionInfos, "this nft collection not exist");
        var collectionInfo = collectionInfos[0];
        var nftItemDetailDto = _objectMapper.Map<IndexerTokenInfoDto, NftItemDetailDto>(nftItem);
        nftItemDetailDto.Quantity = DecimalHelper.Divide(nftItem.Supply, nftItem.Decimals);

        nftItemDetailDto.Item.ImageUrl = TokenInfoHelper.GetImageUrl(nftItem.ExternalInfo,
            () => _tokenInfoProvider.BuildImageUrl(nftItem.Symbol));
        var marketInfo = _tokenInfoOptionsMonitor.CurrentValue.GetMarketInfo(CommonConstant.DefaultMarket);
        marketInfo.MarketUrl = string.Format(marketInfo.MarketUrl, symbol);
        nftItemDetailDto.MarketPlaces = marketInfo;
        nftItemDetailDto.NftCollection = new TokenBaseInfo
        {
            Name = collectionInfo.TokenName,
            Symbol = collectionInfo.Symbol,
            Decimals = collectionInfo.Decimals,
            ImageUrl = TokenInfoHelper.GetImageUrl(collectionInfo.ExternalInfo,
                () => _tokenInfoProvider.BuildImageUrl(collectionInfo.Symbol))
        };

        nftItemDetailDto.ChainIds.Add(nftItem.Metadata.ChainId);
        return nftItemDetailDto;
    }

    public async Task<NftItemDetailDto> GetMergeNftItemDetailAsync(string symbol)
    {
        var searchTokenDetail = new TokenInfoIndex();
        var tokenHolders = 0l;
        var tasks = new List<Task>();
        tasks.Add(EsIndex.SearchTokenDetail(symbol).ContinueWith(task => { searchTokenDetail = task.Result; }));
        tasks.Add(EsIndex.GetTokenHolders(symbol, "").ContinueWith(task => { tokenHolders = task.Result; }));


        await tasks.WhenAll();

        var collectionInfos = await _tokenIndexerProvider.GetTokenDetailAsync("", searchTokenDetail.CollectionSymbol);
        AssertHelper.NotEmpty(collectionInfos, "this nft collection not exist");
        var collectionInfo = collectionInfos[0];
        var nftItemDetailDto = _objectMapper.Map<TokenInfoIndex, NftItemDetailDto>(searchTokenDetail);

        nftItemDetailDto.Quantity = DecimalHelper.Divide(searchTokenDetail.Supply, searchTokenDetail.Decimals);
        nftItemDetailDto.Item.ImageUrl = TokenInfoHelper.GetImageUrl(searchTokenDetail.ExternalInfo,
            () => _tokenInfoProvider.BuildImageUrl(searchTokenDetail.Symbol));
        var marketInfo = _tokenInfoOptionsMonitor.CurrentValue.GetMarketInfo(CommonConstant.DefaultMarket);
        marketInfo.MarketUrl = string.Format(marketInfo.MarketUrl, symbol);
        nftItemDetailDto.MarketPlaces = marketInfo;
        nftItemDetailDto.NftCollection = new TokenBaseInfo
        {
            Name = collectionInfo.TokenName,
            Symbol = collectionInfo.Symbol,
            Decimals = collectionInfo.Decimals,
            ImageUrl = TokenInfoHelper.GetImageUrl(collectionInfo.ExternalInfo,
                () => _tokenInfoProvider.BuildImageUrl(collectionInfo.Symbol))
        };

        nftItemDetailDto.Holders = tokenHolders;
        nftItemDetailDto.ChainIds = searchTokenDetail.ChainIds;


        return nftItemDetailDto;
    }


    public async Task<ListResponseDto<NftItemActivityDto>> GetNftItemActivityAsync(NftItemActivityInput input)
    {
        if (input.ChainId.IsNullOrEmpty())
        {
            input.ChainId = _globalOptions.CurrentValue.SideChainId;
        }

        var activitiesInput = _objectMapper.Map<NftItemActivityInput, GetActivitiesInput>(input);
        activitiesInput.Types = _tokenInfoOptionsMonitor.CurrentValue.ActivityTypes;
        activitiesInput.NftInfoId = IdGeneratorHelper.GetNftInfoId(input.ChainId, input.Symbol);

        var nftActivityInfo = await _nftInfoProvider.GetNftActivityListAsync(activitiesInput);

        if (nftActivityInfo.Items.IsNullOrEmpty())
        {
            return new ListResponseDto<NftItemActivityDto>();
        }

        var list = await ConvertNftItemActivityAsync(input.ChainId, nftActivityInfo.Items);

        return new ListResponseDto<NftItemActivityDto>
        {
            Total = nftActivityInfo.TotalCount,
            List = list
        };
    }

    public async Task<ListResponseDto<NftItemHolderInfoDto>> GetNftItemHoldersAsync(NftItemHolderInfoInput input)
    {
        if (input.ChainId.IsNullOrEmpty())
        {
            return await GetMergeNftItemHoldersAsync(input);
        }

        var tokenHolderInput = _objectMapper.Map<NftItemHolderInfoInput, TokenHolderInput>(input);
        tokenHolderInput.SetDefaultSort();
        var tokenHolderInfoTask = _tokenIndexerProvider.GetTokenHolderInfoAsync(tokenHolderInput);
        var tokenDetailTask = _tokenIndexerProvider.GetTokenDetailAsync(input.ChainId, input.Symbol);
        await Task.WhenAll(tokenHolderInfoTask, tokenDetailTask);
        var nftItemHolderInfos = await tokenHolderInfoTask;
        var nftItemList = await tokenDetailTask;
        AssertHelper.NotEmpty(nftItemList, "this nft not exist");
        var supply = nftItemList[0].Supply;

        var addressList = nftItemHolderInfos.Items
            .Where(value => !string.IsNullOrEmpty(value.Address))
            .Select(value => value.Address).Distinct().ToList();
        var contractInfoDict = await _genesisPluginProvider.GetContractListAsync(input.ChainId, addressList);

        var list = new List<NftItemHolderInfoDto>();
        foreach (var nftCollectionHolderInfoIndex in nftItemHolderInfos.Items)
        {
            var nftItemHolderInfoDto = new NftItemHolderInfoDto()
            {
                Quantity = nftCollectionHolderInfoIndex.FormatAmount
            };

            nftItemHolderInfoDto.Address = CommonAddressHelper.GetCommonAddress(nftCollectionHolderInfoIndex.Address,
                nftCollectionHolderInfoIndex.Metadata.ChainId, contractInfoDict,
                _globalOptions.CurrentValue.ContractNames);
            if (supply > 0)
            {
                nftItemHolderInfoDto.Percentage =
                    Math.Round((decimal)nftCollectionHolderInfoIndex.Amount / supply * 100,
                        CommonConstant.PercentageValueDecimals);
            }

            nftItemHolderInfoDto.ChainIds = new List<string>() { input.ChainId };
            list.Add(nftItemHolderInfoDto);
        }

        return new ListResponseDto<NftItemHolderInfoDto>()
        {
            Total = nftItemHolderInfos.TotalCount,
            List = list
        };
    }

    public async Task<ListResponseDto<NftItemHolderInfoDto>> GetMergeNftItemHoldersAsync(NftItemHolderInfoInput input)
    {
        var tokenHolderInput = _objectMapper.Map<NftItemHolderInfoInput, TokenHolderInput>(input);
        tokenHolderInput.SetDefaultSort();

        List<AccountTokenIndex> accountTokenIndexList = new();
        var total = 0l;
        TokenInfoIndex tokenInfoIndex = new();
        var tasks = new List<Task>();

        tasks.Add(EsIndex.SearchAccountList(tokenHolderInput).ContinueWith(task =>
        {
            accountTokenIndexList = task.Result.list;
            total = task.Result.totalCount;
        }));


        tasks.Add(EsIndex.SearchTokenDetail(input.Symbol).ContinueWith(task => { tokenInfoIndex = task.Result; }));

        await tasks.WhenAll();


        var supply = tokenInfoIndex.Supply;

        var addressList = accountTokenIndexList
            .Where(value => !string.IsNullOrEmpty(value.Address))
            .Select(value => value.Address).Distinct().ToList();
        var contractInfoDict = await _genesisPluginProvider.GetContractListAsync("", addressList);

        var list = new List<NftItemHolderInfoDto>();
        foreach (var tokenInfo in accountTokenIndexList)
        {
            var nftItemHolderInfoDto = new NftItemHolderInfoDto()
            {
                Quantity = tokenInfo.FormatAmount
            };

            foreach (var chainId in tokenInfo.ChainIds)
            {
                nftItemHolderInfoDto.Address = CommonAddressHelper.GetCommonAddress(tokenInfo.Address,
                    chainId, contractInfoDict, _globalOptions.CurrentValue.ContractNames);
            }

            if (supply > 0)
            {
                nftItemHolderInfoDto.Percentage =
                    Math.Round((decimal)tokenInfo.Amount / supply * 100,
                        CommonConstant.PercentageValueDecimals);
            }

            nftItemHolderInfoDto.ChainIds = tokenInfo.ChainIds;
            list.Add(nftItemHolderInfoDto);
        }

        return new ListResponseDto<NftItemHolderInfoDto>()
        {
            Total = total,
            List = list
        };
    }

    private async Task<List<NftItemActivityDto>> ConvertNftItemActivityAsync(string chainId,
        List<NftActivityItem> items)
    {
        var list = new List<NftItemActivityDto>();
        var priceDict = new Dictionary<string, CommonTokenPriceDto>();
        var addressList = items
            .SelectMany(c => new[] { c.From, c.To })
            .Where(value => !string.IsNullOrEmpty(value)).Distinct().ToList();
        var contractInfoDict = await _genesisPluginProvider.GetContractListAsync(chainId, addressList);
        foreach (var item in items)
        {
            var activityDto = _objectMapper.Map<NftActivityItem, NftItemActivityDto>(item);

            activityDto.From = CommonAddressHelper.GetCommonAddress(item.From,
                _globalOptions.CurrentValue.SideChainId, contractInfoDict, _globalOptions.CurrentValue.ContractNames);

            activityDto.To = CommonAddressHelper.GetCommonAddress(item.To,
                _globalOptions.CurrentValue.SideChainId, contractInfoDict, _globalOptions.CurrentValue.ContractNames);
            activityDto.Status = TransactionStatus.Mined;
            var priceSymbol = activityDto.PriceSymbol;
            if (!priceSymbol.IsNullOrEmpty())
            {
                if (!priceDict.TryGetValue(priceSymbol, out var priceDto))
                {
                    priceDto = await _tokenPriceService.GetTokenPriceAsync(priceSymbol,
                        CurrencyConstant.UsdCurrency);
                    priceDict[priceSymbol] = priceDto;
                }

                activityDto.PriceOfUsd =
                    Math.Round(activityDto.Price * priceDto.Price, CommonConstant.UsdPriceValueDecimals);
            }

            activityDto.ChainIds = new List<string>() { chainId };
            list.Add(activityDto);
        }

        return list;
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "ConvertIndexerNftInventoryDtoAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["tokenInfos","chainId"])]
    public virtual async Task<List<NftInventoryDto>> ConvertIndexerNftInventoryDtoAsync(
        List<IndexerTokenInfoDto> tokenInfos, string chainId)
    {
        var list = new List<NftInventoryDto>();
        if (tokenInfos.IsNullOrEmpty())
        {
            return list;
        }
        
            var priceDict = new Dictionary<string, CommonTokenPriceDto>();
            var symbols = tokenInfos.Select(i => i.Symbol).Distinct().ToList();
            var itemInfosDict = tokenInfos.ToDictionary(i => i.Symbol, i => i);
            //batch query symbol last sale info
            var lastSaleInfoDict = await _nftInfoProvider.GetLatestPriceAsync(chainId, symbols);
            foreach (var tokenInfo in tokenInfos)
            {
                var nftInventoryDto =
                    _objectMapper.Map<IndexerTokenInfoDto, NftInventoryDto>(tokenInfo);
                var symbol = nftInventoryDto.Item.Symbol;
                if (itemInfosDict.TryGetValue(symbol, out var itemInfo))
                {
                    //handle image url
                    nftInventoryDto.Item.ImageUrl = TokenInfoHelper.GetImageUrl(itemInfo.ExternalInfo,
                        () => _tokenInfoProvider.BuildImageUrl(symbol));
                }

                if (lastSaleInfoDict.TryGetValue(symbol, out var lastSaleInfo))
                {
                    var saleAmountSymbol = BaseConverter.OfSymbol(lastSaleInfo.PriceTokenInfo);
                    nftInventoryDto.LastTransactionId = lastSaleInfo.TransactionHash;
                    nftInventoryDto.BlockHeight = lastSaleInfo.BlockHeight;
                    //single price
                    nftInventoryDto.LastSalePrice = lastSaleInfo.Price;
                    nftInventoryDto.LastSaleAmount = lastSaleInfo.Amount;
                    nftInventoryDto.LastSaleAmountSymbol = saleAmountSymbol;
                    if (!saleAmountSymbol.IsNullOrEmpty())
                    {
                        if (!priceDict.TryGetValue(saleAmountSymbol, out var priceDto))
                        {
                            priceDto = await _tokenPriceService.GetTokenPriceAsync(saleAmountSymbol,
                                CurrencyConstant.UsdCurrency);
                            priceDict[saleAmountSymbol] = priceDto;
                        }

                        nftInventoryDto.LastSalePriceInUsd = Math.Round(nftInventoryDto.LastSalePrice * priceDto.Price,
                            CommonConstant.UsdPriceValueDecimals);
                    }
                }

                nftInventoryDto.ChainIds = new List<string>() { _globalOptions.CurrentValue.SideChainId };
                list.Add(nftInventoryDto);
            }

            return list;
            
    }


    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "ConvertMergeNftInventoryDtoAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["tokenInfos"])]
    public virtual async Task<List<NftInventoryDto>> ConvertMergeNftInventoryDtoAsync(
        List<TokenInfoIndex> tokenInfos)
    {
        var list = new List<NftInventoryDto>();
        if (tokenInfos.IsNullOrEmpty())
        {
            return list;
        }
        
            var priceDict = new Dictionary<string, CommonTokenPriceDto>();
            var symbols = tokenInfos.Select(i => i.Symbol).Distinct().ToList();
            var itemInfosDict = tokenInfos.ToDictionary(i => i.Symbol, i => i);
            //batch query symbol last sale info
            var lastSaleInfoDict =
                await _nftInfoProvider.GetLatestPriceAsync(_globalOptions.CurrentValue.SideChainId, symbols);
            foreach (var tokenInfo in tokenInfos)
            {
                var nftInventoryDto = new NftInventoryDto();
                var symbol = tokenInfo.Symbol;
                if (itemInfosDict.TryGetValue(symbol, out var itemInfo))
                {
                    nftInventoryDto.Item.ImageUrl = TokenInfoHelper.GetImageUrl(itemInfo.ExternalInfo,
                        () => _tokenInfoProvider.BuildImageUrl(symbol));
                }

                nftInventoryDto.Item.Symbol = tokenInfo.Symbol;
                nftInventoryDto.Item.Name = tokenInfo.TokenName;
                if (lastSaleInfoDict.TryGetValue(symbol, out var lastSaleInfo))
                {
                    var saleAmountSymbol = BaseConverter.OfSymbol(lastSaleInfo.PriceTokenInfo);
                    nftInventoryDto.LastTransactionId = lastSaleInfo.TransactionHash;
                    nftInventoryDto.BlockHeight = lastSaleInfo.BlockHeight;
                    //single price
                    nftInventoryDto.LastSalePrice = lastSaleInfo.Price;
                    nftInventoryDto.LastSaleAmount = lastSaleInfo.Amount;
                    nftInventoryDto.LastSaleAmountSymbol = saleAmountSymbol;
                    if (!saleAmountSymbol.IsNullOrEmpty())
                    {
                        if (!priceDict.TryGetValue(saleAmountSymbol, out var priceDto))
                        {
                            priceDto = await _tokenPriceService.GetTokenPriceAsync(saleAmountSymbol,
                                CurrencyConstant.UsdCurrency);
                            priceDict[saleAmountSymbol] = priceDto;
                        }

                        nftInventoryDto.LastSalePriceInUsd = Math.Round(nftInventoryDto.LastSalePrice * priceDto.Price,
                            CommonConstant.UsdPriceValueDecimals);
                    }
                }

                nftInventoryDto.ChainIds = new List<string>() { _globalOptions.CurrentValue.SideChainId };
                list.Add(nftInventoryDto);
            }


        return list;
    }

    private async Task<Dictionary<string, TokenCommonDto>> GetTokenDicAsync(List<string> symbols, string chainId)
    {
        var input = new TokenListInput
        {
            ChainId = chainId,
            Symbols = symbols
        };
        var indexerTokenListDto = await _tokenIndexerProvider.GetTokenListAsync(input);
        var tokenInfoDtoList =
            _objectMapper.Map<List<IndexerTokenInfoDto>, List<TokenCommonDto>>(indexerTokenListDto.Items);
        return tokenInfoDtoList.ToDictionary(token => token.Token.Symbol, token => token);
    }

    private async Task<List<TokenHolderInfoDto>> ConvertIndexerNftHolderInfoDtoAsync(
        List<IndexerTokenHolderInfoDto> indexerTokenHolderInfo, string chainId, string collectionSymbol)
    {
        var addressList = indexerTokenHolderInfo
            .Where(value => !string.IsNullOrEmpty(value.Address))
            .Select(value => value.Address).Distinct().ToList();
        var getCollectionInfoTask = _tokenIndexerProvider.GetTokenDetailAsync(chainId, collectionSymbol);

        var contractInfoDictTask = _genesisPluginProvider.GetContractListAsync(chainId, addressList);
        await Task.WhenAll(getCollectionInfoTask, contractInfoDictTask);

        var list = new List<TokenHolderInfoDto>();
        var contractInfoDict = await contractInfoDictTask;
        var tokenSupply = (await getCollectionInfoTask)[0].ItemCount;

        foreach (var indexerTokenHolderInfoDto in indexerTokenHolderInfo)
        {
            var tokenHolderInfoDto =
                _objectMapper.Map<IndexerTokenHolderInfoDto, TokenHolderInfoDto>(indexerTokenHolderInfoDto);

            tokenHolderInfoDto.Address =
                CommonAddressHelper.GetCommonAddress(indexerTokenHolderInfoDto.Address,
                    indexerTokenHolderInfoDto.Metadata.ChainId, contractInfoDict,
                    _globalOptions.CurrentValue.ContractNames);

            if (tokenSupply != 0)
            {
                tokenHolderInfoDto.Percentage =
                    Math.Round(indexerTokenHolderInfoDto.FormatAmount / tokenSupply * 100,
                        CommonConstant.PercentageValueDecimals);
            }

            tokenHolderInfoDto.ChainIds = new List<string>() { indexerTokenHolderInfoDto.Metadata.ChainId };
            list.Add(tokenHolderInfoDto);
        }

        return list;
    }

    public async Task<Dictionary<string, string>> GetCollectionSupplyAsync(string chainId,
        List<string> collectionSymbols)
    {
        var keyList = collectionSymbols.Select(o => GetCollectionItemsKey(chainId, o)).ToList();
        var keyValuePairs = await _distributedCache.GetManyAsync(keyList);
        foreach (var collectionSymbol in collectionSymbols)
        {
            _ = SetCollectionItemAsync(chainId, collectionSymbol);
        }

        return keyValuePairs.ToDictionary(o => o.Key.Split("_")[2], o => o.Value ?? "0");
    }

    private async Task<List<HolderInfo>> GetHolderInfoAsync(SymbolType symbolType, string chainId, string address)
    {
        var tokenHolderInput = new TokenHolderInput
        {
            Types = new List<SymbolType> { symbolType },
            ChainId = chainId,
            Address = address
        };
        var indexerNftHolder = await _tokenIndexerProvider.GetTokenHolderInfoAsync(tokenHolderInput);
        return indexerNftHolder.Items.Select(i => new HolderInfo
        {
            Balance = i.FormatAmount,
            Symbol = i.Token.Symbol
        }).ToList();
    }

    private async Task SetCollectionItemAsync(string chainId, string symbol)
    {
        var exist = _memoryCache.TryGetValue(GetCollectionItemsTimeKey(chainId, symbol), out var time);
        if (exist)
        {
            return;
        }

        var sumSupply = await QueryCollectionItem(chainId, symbol);
        _logger.LogInformation("QueryCollectionItem {chainId} {symbol} value {exist}", chainId, symbol, exist);
        await _distributedCache.SetAsync(GetCollectionItemsKey(chainId, symbol),
            sumSupply.ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions
                { SlidingExpiration = null, AbsoluteExpiration = DateTimeOffset.MaxValue });
        await _memoryCache.GetOrCreateAsync<string>(
            GetCollectionItemsTimeKey(chainId, symbol), entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(new Random().Next(3, 6)));
                return Task.FromResult(DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }
        );
        _logger.LogInformation("GetOrCreateAsync {chainId} {symbol}", chainId, symbol);
    }


    private async Task<decimal> QueryCollectionItem(string chainId, string symbol)
    {
        decimal sumSupply = 0;
        var nftInput = new TokenListInput()
        {
            ChainId = chainId, Types =  [SymbolType.Nft],
            CollectionSymbols =  [symbol], MaxResultCount = 1000,
            OrderBy = "Symbol", Sort = "Desc"
        };
        int count;
        do
        {
            var nftListDto = await _tokenIndexerProvider.GetTokenListAsync(nftInput);
            count = nftListDto.Items.Count;
            if (!nftListDto.Items.IsNullOrEmpty())
            {
                sumSupply += nftListDto.Items.Sum(token => DecimalHelper.Divide(token.Supply, token.Decimals));
                nftInput.SearchAfter =  [nftListDto.Items [count
                -1].Symbol];
            }
        } while (count == MaxResultCount);

        return sumSupply;
    }

    private string GetCollectionItemsKey(string chainId, string symbol)
    {
        return $"explore_{chainId}_{symbol}_collection_items";
    }

    private string GetCollectionItemsTimeKey(string chainId, string symbol)
    {
        return $"explore_{chainId}_{symbol}_collection_items_time";
    }
}