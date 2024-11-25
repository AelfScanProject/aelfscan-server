using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common.Address.Provider;
using AElfScanServer.Common.Commons;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.HttpApi.Provider;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Volo.Abp.ObjectMapping;
using MetadataDto = AElfScanServer.Common.Dtos.MetadataDto;
using TokenInfoDto = AElfScanServer.Common.Dtos.TokenInfoDto;

namespace AElfScanServer.HttpApi.Service;

public interface IAddressAppService
{
    Task<GetAddressListResultDto> GetAddressListAsync(GetListInputInput input);
    Task<GetAddressDetailResultDto> GetAddressDetailAsync(GetAddressDetailInput input);
    Task<GetAddressTokenListResultDto> GetAddressTokenListAsync(GetAddressTokenListInput input);
    Task<GetAddressNftListResultDto> GetAddressNftListAsync(GetAddressTokenListInput input);
    Task<GetTransferListResultDto> GetTransferListAsync(GetTransferListInput input);
}

[Ump]
public class AddressAppService : IAddressAppService
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AddressAppService> _logger;
    private readonly IIndexerGenesisProvider _indexerGenesisProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptions;
    private readonly GlobalOptions _globalOptions;
    private readonly ITokenAssetProvider _tokenAssetProvider;
    private readonly IAddressInfoProvider _addressInfoProvider;
    private readonly IGenesisPluginProvider _genesisPluginProvider;
    private readonly IBlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly IAddressTypeService _addressTypeService;
    private readonly IElasticClient _elasticClient;


    public AddressAppService(IObjectMapper objectMapper, ILogger<AddressAppService> logger,
        IIndexerGenesisProvider indexerGenesisProvider,
        ITokenIndexerProvider tokenIndexerProvider, ITokenPriceService tokenPriceService,
        ITokenInfoProvider tokenInfoProvider, IOptionsMonitor<TokenInfoOptions> tokenInfoOptions,
        IOptionsSnapshot<GlobalOptions> globalOptions, ITokenAssetProvider tokenAssetProvider,
        IAddressInfoProvider addressInfoProvider, IGenesisPluginProvider genesisPluginProvider,
        IBlockChainIndexerProvider blockChainIndexerProvider,
        IAddressTypeService addressTypeService, IOptionsMonitor<ElasticsearchOptions> options)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _indexerGenesisProvider = indexerGenesisProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenPriceService = tokenPriceService;
        _tokenInfoProvider = tokenInfoProvider;
        _tokenInfoOptions = tokenInfoOptions;
        _tokenAssetProvider = tokenAssetProvider;
        _addressInfoProvider = addressInfoProvider;
        _genesisPluginProvider = genesisPluginProvider;
        _globalOptions = globalOptions.Value;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _addressTypeService = addressTypeService;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        _elasticClient = new ElasticClient(settings);
        EsIndex.SetElasticClient(_elasticClient);
    }

    public async Task<GetAddressListResultDto> GetAddressListAsync(GetListInputInput input)
    {
        var holderInput = new TokenHolderInput
        {
            ChainId = input.ChainId, Symbol = CurrencyConstant.ElfCurrency,
            SkipCount = input.SkipCount, MaxResultCount = input.MaxResultCount,
            OrderBy = input.OrderBy,
            OrderInfos = input.OrderInfos,
            SearchAfter = input.SearchAfter
        };

        return await GetMergeAddressListAsync(holderInput);
    }

    public async Task<GetAddressListResultDto> GetMergeAddressListAsync(TokenHolderInput input)
    {
        var tasks = new List<Task>();
        List<AccountTokenIndex> accountList = new List<AccountTokenIndex>();
        List<IndexerTokenInfoDto> tokenInfoList = new List<IndexerTokenInfoDto>();
        long totalCount = 0;

        tasks.Add(EsIndex.SearchAccountList(input).ContinueWith(task =>
        {
            accountList.AddRange(task.Result.list);
            totalCount = task.Result.totalCount;
        }));

        tasks.Add(_tokenIndexerProvider.GetTokenDetailAsync(input.ChainId, CurrencyConstant.ElfCurrency).ContinueWith(
            task => { tokenInfoList.AddRange(task.Result); }));

        await tasks.WhenAll();


        var result = new GetAddressListResultDto
        {
            Total = totalCount,
            TotalBalance = DecimalHelper.Divide(tokenInfoList.Sum(c => c.Supply), tokenInfoList[0].Decimals)
        };


        var sum = tokenInfoList.Sum(c => c.Supply);
        var contractInfosDict =
            await _indexerGenesisProvider.GetContractListAsync("",
                accountList.Select(address => address.Address).ToList());


        var addressList = new List<GetAddressInfoResultDto>();
        foreach (var info in accountList)
        {
            var addressResult = _objectMapper.Map<AccountTokenIndex, GetAddressInfoResultDto>(info);
            addressResult.Balance = info.FormatAmount;
            addressResult.TransactionCount = info.TransferCount;
            addressResult.Percentage = Math.Round((decimal)info.Amount / sum * 100,
                CommonConstant.LargerPercentageValueDecimals);


            var dic = new Dictionary<string, MergeAddressType>();

            foreach (var c in info.ChainIds)
            {
                dic[c] = new MergeAddressType()
                {
                    ChainId = c
                };

                if (contractInfosDict.TryGetValue(info.Address + info.ChainId, out var v))
                {
                    dic[c].AddressType = AddressType.ContractAddress;
                }
            }

            addressResult.MergeAddressType.AddRange(dic.Values.OrderByDescending(c => c.ChainId));
            addressResult.AddressType =
                contractInfosDict.TryGetValue(info.Address + info.ChainId, out var addressInfo)
                    ? AddressType.ContractAddress
                    : AddressType.EoaAddress;
            addressList.Add(addressResult);
        }

        //add sort 
        addressList = addressList.OrderByDescending(item => item.Balance)
            .ThenByDescending(item => item.TransactionCount)
            .ToList();
        result.List = addressList;
        return result;
    }


    public async Task<GetAddressDetailResultDto> GetAddressDetailAsync(GetAddressDetailInput input)
    {
        var contractInfo =
            await _indexerGenesisProvider.GetContractListAsync(input.ChainId, 0, 1, "", "", input.Address);

        var contractChainId = "";
        var author = "";
        if (contractInfo != null && contractInfo.ContractList != null && contractInfo.ContractList.Items != null &&
            contractInfo.ContractList.Items.Count > 0)
        {
            contractChainId = contractInfo.ContractList.Items.First().Metadata.ChainId;
            author = contractInfo.ContractList.Items.First().Author;
        }


        return await GetMergeAddressDetailAsync(input, author, contractChainId);
    }


    public async Task<string> GetAccountChainIdAsync(string address, string chainId)
    {
        var holderInput = new TokenHolderInput { ChainId = chainId, Address = address, MaxResultCount = 1 };

        var tokenHolderInfos = await _tokenIndexerProvider.GetTokenHolderInfoAsync(holderInput);

        if (tokenHolderInfos != null && tokenHolderInfos.Items != null && tokenHolderInfos.Items.Count > 0)
        {
            return chainId;
        }

        return "";
    }


    public async Task<GetAddressDetailResultDto> GetMergeAddressDetailAsync(GetAddressDetailInput input,
        string author, string contractChainId)
    {
        var hashSet = new HashSet<string>();

        var mainChainIdTask = GetAccountChainIdAsync(input.Address, "AELF");
        var sideChainIdTask = GetAccountChainIdAsync(input.Address, _globalOptions.SideChainId);
        var priceDtoTask =
            _tokenPriceService.GetTokenPriceAsync(CurrencyConstant.ElfCurrency, CurrencyConstant.UsdCurrency);

        var addressTypeTask = _addressTypeService.GetAddressTypeList(input.ChainId, input.Address);


        var mainChainCurAddressAssetTokenTask =
            _tokenAssetProvider.GetTokenValuesAsync("AELF", input.Address, new List<SymbolType>() { SymbolType.Token });

        var sideChainCurAddressAssetTokenTask =
            _tokenAssetProvider.GetTokenValuesAsync(_globalOptions.SideChainId, input.Address, new List<SymbolType>()
            {
                SymbolType.Token
            });

        var mainChainCurAddressAssetNftTask =
            _tokenAssetProvider.GetTokenValuesAsync("AELF", input.Address, new List<SymbolType>() { SymbolType.Nft });

        var sideChainCurAddressAssetNftTask =
            _tokenAssetProvider.GetTokenValuesAsync(_globalOptions.SideChainId, input.Address, new List<SymbolType>()
            {
                SymbolType.Nft
            });


        await Task.WhenAll(mainChainCurAddressAssetTokenTask, priceDtoTask,
            sideChainCurAddressAssetTokenTask, mainChainCurAddressAssetNftTask,
            sideChainCurAddressAssetNftTask, mainChainIdTask, sideChainIdTask);

        var addressTypeList = await addressTypeTask;

        var mainChainCurAddressAssetToken = await mainChainCurAddressAssetTokenTask;
        var sideChainCurAddressAssetToken = await sideChainCurAddressAssetTokenTask;


        var mainChainCurAddressAssetNft = await mainChainCurAddressAssetNftTask;
        var sideChainCurAddressAssetNft = await sideChainCurAddressAssetNftTask;
        var mainChainId = await mainChainIdTask;
        var sideChainId = await sideChainIdTask;

        if (!mainChainId.IsNullOrEmpty())
        {
            hashSet.Add(mainChainId);
        }

        if (!sideChainId.IsNullOrEmpty())
        {
            hashSet.Add(sideChainId);
        }

        if (!contractChainId.IsNullOrEmpty())
        {
            hashSet = new HashSet<string>() { contractChainId };
        }


        var priceDto = await priceDtoTask;
        var result = new GetAddressDetailResultDto();
        result.AddressTypeList = addressTypeList;

        if (!contractChainId.IsNullOrEmpty())
        {
            result.AddressType = AddressType.ContractAddress;
        }

        _logger.LogInformation(
            $"GetMergeAddressDetailAsync: mainChainCurAddressAssetToken:{mainChainCurAddressAssetToken.GetTotalValueOfElf()}," +
            $"token count {mainChainCurAddressAssetToken.Count}," +
            $"sideChainCurAddressAssetToken:{sideChainCurAddressAssetToken.GetTotalValueOfElf()}," +
            $"count {sideChainCurAddressAssetToken.Count}," +
            $"mainChainCurAddressAssetNft:{mainChainCurAddressAssetNft.GetTotalValueOfElf()}" +
            $"count {mainChainCurAddressAssetNft.Count} " +
            $"sideChainCurAddressAssetNft:{sideChainCurAddressAssetNft.GetTotalValueOfElf()} " +
            $"count {sideChainCurAddressAssetNft.Count}");

        result.Portfolio.MainTokenValue = Math.Round(
            new decimal(mainChainCurAddressAssetToken.GetTotalValueOfElf()) * priceDto.Price,
            CommonConstant.UsdValueDecimals);
        result.Portfolio.SideTokenValue = Math.Round(
            new decimal(sideChainCurAddressAssetToken.GetTotalValueOfElf()) * priceDto.Price,
            CommonConstant.UsdValueDecimals);

        result.Portfolio.MainTokenValueOfElf = Math.Round(
            new decimal(mainChainCurAddressAssetToken.GetTotalValueOfElf()));
        result.Portfolio.SideTokenValueOfElf = Math.Round(
            new decimal(sideChainCurAddressAssetToken.GetTotalValueOfElf()));

        result.Portfolio.MainNftCount = mainChainCurAddressAssetNft.Count;
        result.Portfolio.SideNftCount = sideChainCurAddressAssetNft.Count;

        result.Portfolio.MainTokenCount = mainChainCurAddressAssetToken.Count;
        result.Portfolio.SideTokenCount = sideChainCurAddressAssetToken.Count;

        result.Author = author;
        result.ContractName = _globalOptions.GetContractName(contractChainId, input.Address);

        mainChainCurAddressAssetNft.SymbolSet.UnionWith(sideChainCurAddressAssetNft.SymbolSet);
        mainChainCurAddressAssetToken.SymbolSet.UnionWith(sideChainCurAddressAssetToken.SymbolSet);

        result.Portfolio.TotalNftCount = mainChainCurAddressAssetNft.SymbolSet.Count;
        result.Portfolio.TotalTokenCount = mainChainCurAddressAssetToken.SymbolSet.Count;

        result.ChainIds = hashSet.OrderByDescending(c => c).ToList();

        return result;
    }

    public async Task<GetAddressTokenListResultDto> GetAddressTokenListAsync(
        GetAddressTokenListInput input)
    {
        var types = new List<SymbolType> { SymbolType.Token };
        input.SetDefaultSort();
        Dictionary<string, IndexerTokenInfoDto> tokenDict;
        IndexerTokenHolderInfoListDto holderInfos;
        //search token name or symbol
        if (!input.FuzzySearch.IsNullOrWhiteSpace())
        {
            input.FuzzySearch = input.FuzzySearch.ToLower();
            var tokenListInput = _objectMapper.Map<GetAddressTokenListInput, TokenListInput>(input);
            tokenListInput.Types = types;
            var tokenInfos = await _tokenIndexerProvider.GetAllTokenInfosAsync(tokenListInput);
            if (tokenInfos.IsNullOrEmpty())
            {
                return new GetAddressTokenListResultDto();
            }

            tokenDict = tokenInfos.ToDictionary(i => i.Symbol + i.Metadata.ChainId, i => i);
            var symbolSet = new HashSet<string>(tokenDict.Values.Select(i => i.Symbol));

            holderInfos = await GetTokenHolderInfosAsync(input, searchSymbols: symbolSet.ToList());
            if (holderInfos.Items.IsNullOrEmpty())
            {
                return new GetAddressTokenListResultDto();
            }
        }
        else
        {
            holderInfos = await GetTokenHolderInfosAsync(input, types);
            if (holderInfos.Items.IsNullOrEmpty())
            {
                return new GetAddressTokenListResultDto();
            }

            tokenDict = await _tokenIndexerProvider.GetTokenDictAsync(input.ChainId,
                holderInfos.Items.Select(i => i.Token.Symbol).ToList());
        }

        var elfPriceDto =
            await _tokenPriceService.GetTokenPriceAsync(CurrencyConstant.ElfCurrency, CurrencyConstant.UsdCurrency);

        var tokenInfoList = await GetTokenInfoListAsync(holderInfos.Items, tokenDict, elfPriceDto);

        return new GetAddressTokenListResultDto
        {
            AssetInUsd = tokenInfoList.Sum(i => i.ValueOfUsd),
            AssetInElf = tokenInfoList.Sum(i => i.ValueOfElf),
            Total = holderInfos.TotalCount,
            List = tokenInfoList
        };
    }


    public async Task<GetAddressNftListResultDto> GetAddressNftListAsync(GetAddressTokenListInput input)
    {
        IndexerTokenHolderInfoListDto holderInfos;
        var types = new List<SymbolType> { SymbolType.Nft };

        if (!input.FuzzySearch.IsNullOrWhiteSpace())
        {
            input.FuzzySearch = input.FuzzySearch.ToLower();
            var tokenListInputNft = _objectMapper.Map<GetAddressTokenListInput, TokenListInput>(input);
            tokenListInputNft.Types = types;

            var tokenListInputCollection = _objectMapper.Map<GetAddressTokenListInput, TokenListInput>(input);
            tokenListInputCollection.Types = new List<SymbolType> { SymbolType.Nft_Collection };

            var nftInfosTask = _tokenIndexerProvider.GetAllTokenInfosAsync(tokenListInputNft);
            var collectionInfosTask = _tokenIndexerProvider.GetAllTokenInfosAsync(tokenListInputCollection);
            await Task.WhenAll(nftInfosTask, collectionInfosTask);

            var nftInfos = nftInfosTask.Result;
            var collectionInfos = collectionInfosTask.Result;

            if (nftInfos.IsNullOrEmpty() && collectionInfos.IsNullOrEmpty())
            {
                return new GetAddressNftListResultDto();
            }

            var searchSymbols = new List<string>(nftInfos.Select(i => i.Symbol).ToHashSet());
            ;
            var searchCollectionSymbols = new List<string>(collectionInfos.Select(i => i.Symbol).ToHashSet());
            searchSymbols.AddRange(searchCollectionSymbols);
            holderInfos = await GetTokenHolderInfosAsync(input, types, searchSymbols: searchSymbols);
            if (holderInfos.Items.IsNullOrEmpty())
            {
                return new GetAddressNftListResultDto();
            }
        }
        else
        {
            holderInfos = await GetTokenHolderInfosAsync(input, types);
            if (holderInfos.Items.IsNullOrEmpty())
            {
                return new GetAddressNftListResultDto();
            }
        }

        var collectionSymbols = new List<string>(holderInfos.Items.Select(i => i.Token.CollectionSymbol).ToHashSet());
        var symbols = new List<string>(holderInfos.Items.Select(i => i.Token.Symbol).ToHashSet());
        symbols.AddRange(collectionSymbols);
        var tokenDict = await _tokenIndexerProvider.GetTokenDictAsync(input.ChainId, symbols);
        var list = await CreateNftInfoListAsync(holderInfos.Items, tokenDict);
        var result = new GetAddressNftListResultDto
        {
            Total = holderInfos.TotalCount,
            List = list
        };
        return result;
    }

    public async Task<GetTransferListResultDto> GetTransferListAsync(GetTransferListInput input)
    {
        var tokenTransferInput = _objectMapper.Map<GetTransferListInput, TokenTransferInput>(input);
        tokenTransferInput.Types = new List<SymbolType> { input.TokenType };
        tokenTransferInput.SetDefaultSort();
        var tokenTransferInfos = await _tokenIndexerProvider.GetTokenTransfersAsync(tokenTransferInput);
        return new GetTransferListResultDto
        {
            Total = tokenTransferInfos.Total,
            List = tokenTransferInfos.List
        };
    }


    private async Task<IndexerTokenHolderInfoListDto> GetTokenHolderInfosAsync(GetAddressTokenListInput input,
        List<SymbolType> types = null,
        List<string> searchSymbols = null, bool ignoreSearch = true)
    {
        var tokenHolderInput = _objectMapper.Map<GetAddressTokenListInput, TokenHolderInput>(input);
        tokenHolderInput.SetDefaultSort();
        if (types != null)
        {
            tokenHolderInput.Types = types;
        }

        if (searchSymbols != null)
        {
            tokenHolderInput.SearchSymbols = searchSymbols;
        }

        if (ignoreSearch)
        {
            tokenHolderInput.Search = "";
        }

        return await _tokenIndexerProvider.GetTokenHolderInfoAsync(tokenHolderInput);
    }

    private async Task<List<TokenInfoDto>> GetTokenInfoListAsync(IEnumerable<IndexerTokenHolderInfoDto> holderInfos,
        Dictionary<string, IndexerTokenInfoDto> tokenDict, CommonTokenPriceDto elfPriceDto)
    {
        var list = new List<TokenInfoDto>();

        var tasks = holderInfos.Select(async holderInfo =>
        {
            var tokenHolderInfo = _objectMapper.Map<IndexerTokenHolderInfoDto, TokenInfoDto>(holderInfo);
            var symbol = holderInfo.Token.Symbol;
            tokenHolderInfo.Type = holderInfo.Token.Type;
            if (tokenDict.TryGetValue(symbol + holderInfo.Metadata.ChainId, out var tokenInfo))
            {
                // handle image url
                tokenHolderInfo.Token.Name = tokenInfo.TokenName;
                tokenHolderInfo.Token.ImageUrl =
                    await _tokenIndexerProvider.GetTokenImageAsync(tokenInfo.Symbol, tokenInfo.IssueChainId,
                        tokenInfo.ExternalInfo);
            }

            if (_tokenInfoOptions.CurrentValue.NonResourceSymbols.Contains(symbol))
            {
                var priceDto = await _tokenPriceService.GetTokenPriceAsync(symbol, CurrencyConstant.UsdCurrency);
                var timestamp = TimeHelper.GetTimeStampFromDateTime(DateTime.Today);
                var priceHisDto =
                    await _tokenPriceService.GetTokenHistoryPriceAsync(symbol, CurrencyConstant.UsdCurrency, timestamp);

                tokenHolderInfo.PriceOfUsd = Math.Round(priceDto.Price, CommonConstant.UsdValueDecimals);
                tokenHolderInfo.ValueOfUsd = Math.Round(tokenHolderInfo.Quantity * priceDto.Price,
                    CommonConstant.UsdValueDecimals);
                tokenHolderInfo.PriceOfElf =
                    Math.Round(priceDto.Price / elfPriceDto.Price, CommonConstant.ElfValueDecimals);
                tokenHolderInfo.ValueOfElf = Math.Round(tokenHolderInfo.Quantity * priceDto.Price / elfPriceDto.Price,
                    CommonConstant.ElfValueDecimals);

                if (priceHisDto.Price > 0)
                {
                    tokenHolderInfo.PriceOfUsdPercentChange24h = (double)Math.Round(
                        (priceDto.Price - priceHisDto.Price) / priceHisDto.Price * 100,
                        CommonConstant.PercentageValueDecimals);
                }
            }

            tokenHolderInfo.ChainIds = new List<string>() { holderInfo.Metadata.ChainId };
            return tokenHolderInfo;
        }).ToList();

        list.AddRange(await Task.WhenAll(tasks));
        return list;
    }

    private async Task<List<AddressNftInfoDto>> CreateNftInfoListAsync(
        List<IndexerTokenHolderInfoDto> holderInfos, Dictionary<string, IndexerTokenInfoDto> tokenDict)
    {
        var tasks = holderInfos.Select(async holderInfo =>
        {
            var tokenHolderInfo = _objectMapper.Map<IndexerTokenHolderInfoDto, AddressNftInfoDto>(holderInfo);
            var symbol = holderInfo.Token.Symbol;
            var collectionSymbol = holderInfo.Token.CollectionSymbol;


            if (tokenDict.TryGetValue(symbol + holderInfo.Metadata.ChainId, out var tokenInfo))
            {
                tokenHolderInfo.Token = _tokenInfoProvider.OfTokenBaseInfo(tokenInfo);
            }

            if (tokenDict.TryGetValue(collectionSymbol + holderInfo.Metadata.ChainId, out var collectionInfo))
            {
                tokenHolderInfo.NftCollection = _tokenInfoProvider.OfTokenBaseInfo(collectionInfo);
            }

            tokenHolderInfo.ChainIds = new List<string>() { holderInfo.Metadata.ChainId };
            return tokenHolderInfo;
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    private static TransactionInfoDto OfTransactionInfo(IndexerTransactionInfoDto transferInfoDto)
    {
        if (transferInfoDto == null)
        {
            return null;
        }

        return new TransactionInfoDto
        {
            TransactionId = transferInfoDto.TransactionId,
            BlockTime = transferInfoDto.Metadata.Block.BlockTime
        };
    }
}