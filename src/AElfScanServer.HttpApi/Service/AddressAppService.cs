using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common.Address.Provider;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.HttpApi.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;
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


    public AddressAppService(IObjectMapper objectMapper, ILogger<AddressAppService> logger,
        IIndexerGenesisProvider indexerGenesisProvider,
        ITokenIndexerProvider tokenIndexerProvider, ITokenPriceService tokenPriceService,
        ITokenInfoProvider tokenInfoProvider, IOptionsMonitor<TokenInfoOptions> tokenInfoOptions,
        IOptionsSnapshot<GlobalOptions> globalOptions, ITokenAssetProvider tokenAssetProvider,
        IAddressInfoProvider addressInfoProvider, IGenesisPluginProvider genesisPluginProvider,
        IBlockChainIndexerProvider blockChainIndexerProvider,
        IAddressTypeService addressTypeService)
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

        var tokenHolderInfoTask = _tokenIndexerProvider.GetTokenHolderInfoAsync(holderInput);
        var tokenDetailTask = _tokenIndexerProvider.GetTokenDetailAsync(input.ChainId, CurrencyConstant.ElfCurrency);

        await Task.WhenAll(tokenHolderInfoTask, tokenDetailTask);

        var tokenHolderAccountlist = await tokenHolderInfoTask;
        var indexerTokenList = await tokenDetailTask;
        var tokenInfo = indexerTokenList[0];

        var result = new GetAddressListResultDto
        {
            Total = tokenHolderAccountlist.TotalCount,
            TotalBalance = DecimalHelper.Divide(indexerTokenList.Sum(c => c.Supply), tokenInfo.Decimals)
        };


        var contractInfosDict =
            await _indexerGenesisProvider.GetContractListAsync(input.ChainId,
                tokenHolderAccountlist.Items.Select(address => address.Address).ToList());


        var addressList = new List<GetAddressInfoResultDto>();
        foreach (var info in tokenHolderAccountlist.Items)
        {
            var addressResult = _objectMapper.Map<IndexerTokenHolderInfoDto, GetAddressInfoResultDto>(info);
            addressResult.ChainIds = new List<string>() { info.Metadata.ChainId };
            addressResult.Percentage = Math.Round((decimal)info.Amount / tokenInfo.Supply * 100,
                CommonConstant.LargerPercentageValueDecimals);
            addressResult.AddressType = contractInfosDict.TryGetValue(info.Address + info.Metadata.ChainId, out var addressInfo)
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
        var priceDtoTask =
            _tokenPriceService.GetTokenPriceAsync(CurrencyConstant.ElfCurrency, CurrencyConstant.UsdCurrency);
        var timestamp = TimeHelper.GetTimeStampFromDateTime(DateTime.Today);
        var priceHisDtoTask = _tokenPriceService.GetTokenHistoryPriceAsync(CurrencyConstant.ElfCurrency,
            CurrencyConstant.UsdCurrency, timestamp);
        var curAddressAssetTask = _tokenAssetProvider.GetTokenValuesAsync(input.ChainId, input.Address);
        var dailyAddressAssetTask =
            _addressInfoProvider.GetAddressAssetAsync(AddressAssetType.Daily, input.ChainId, input.Address);
        var holderInfoTask =
            _tokenIndexerProvider.GetHolderInfoAsync(input.ChainId, CurrencyConstant.ElfCurrency, input.Address);
        var holderInfosTask = _tokenIndexerProvider.GetHolderInfoAsync(input.ChainId, input.Address,
            new List<SymbolType> { SymbolType.Token, SymbolType.Nft });
        var contractInfoTask = _indexerGenesisProvider.GetContractListAsync(input.ChainId, 0, 1, "", "", input.Address);
        var transferInput = new TokenTransferInput { ChainId = input.ChainId, Address = input.Address };
        transferInput.OfOrderInfos((SortField.BlockHeight, SortDirection.Desc));
        var addressTypeTask = _addressTypeService.GetAddressTypeList(input.ChainId, input.Address);

        var firstTransactionInput = new TransactionsRequestDto()
        {
            ChainId = input.ChainId,
            Address = input.Address,
            SkipCount = 0,
            MaxResultCount = 1,
        };
        firstTransactionInput.SetFirstTransactionSort();
        var firstTransactionTask = _blockChainIndexerProvider.GetTransactionsAsync(firstTransactionInput);

        var lastTransactionInput = new TransactionsRequestDto()
        {
            ChainId = input.ChainId,
            Address = input.Address,
            SkipCount = 0,
            MaxResultCount = 1,
        };

        lastTransactionInput.SetLastTransactionSort();
        var lastTransactionTask = _blockChainIndexerProvider.GetTransactionsAsync(lastTransactionInput);


        await Task.WhenAll(priceDtoTask, priceHisDtoTask, holderInfoTask, curAddressAssetTask, dailyAddressAssetTask,
            holderInfosTask, contractInfoTask, firstTransactionTask,
            lastTransactionTask);

        var holderInfo = await holderInfoTask;
        var priceDto = await priceDtoTask;
        var priceHisDto = await priceHisDtoTask;
        var curAddressAsset = await curAddressAssetTask;
        var dailyAddressAsset = await dailyAddressAssetTask;
        var holderInfos = await holderInfosTask;


        var firstTransaction = await firstTransactionTask;
        var lastTransaction = await lastTransactionTask;
        var contractInfo = await contractInfoTask;
        var addressTypeList = await addressTypeTask;

        _logger.LogInformation("GetAddressDetail chainId: {chainId}, dailyAddressAsset: {dailyAddressAsset}",
            input.ChainId, JsonConvert.SerializeObject(dailyAddressAsset));
        //of result info
        var result = new GetAddressDetailResultDto();
        if (contractInfo != null && contractInfo.ContractList != null && contractInfo.ContractList.Items != null &&
            contractInfo.ContractList.Items.Count > 0)
        {
            result = _objectMapper.Map<ContractInfoDto, GetAddressDetailResultDto>(contractInfo.ContractList.Items[0]);
            result.ContractName = _globalOptions.GetContractName(input.ChainId, input.Address);
            result.Author = contractInfo.ContractList.Items[0].Author;
            result.CodeHash = contractInfo.ContractList.Items[0].CodeHash;
            result.AddressType = AddressType.ContractAddress;
        }


        result.ElfBalance = holderInfo.Balance;
        result.ElfPriceInUsd = Math.Round(priceDto.Price, CommonConstant.UsdValueDecimals);
        result.ElfBalanceOfUsd = Math.Round(holderInfo.Balance * priceDto.Price, CommonConstant.UsdValueDecimals);
        result.TotalValueOfElf = new decimal(curAddressAsset.GetTotalValueOfElf());
        result.TotalValueOfUsd = Math.Round(result.TotalValueOfElf * priceDto.Price, CommonConstant.UsdValueDecimals);
        result.AddressTypeList = addressTypeList;
        if (dailyAddressAsset != null && dailyAddressAsset.GetTotalValueOfElf() != 0 && priceHisDto.Price > 0)
        {
            var dailyTotalValueOfUsd = (decimal)dailyAddressAsset.GetTotalValueOfElf() * priceHisDto.Price;
            var curTotalValueOfUsd = (decimal)curAddressAsset.GetTotalValueOfElf() * priceDto.Price;
            result.TotalValueOfUsdChangeRate =
                Math.Round((curTotalValueOfUsd - dailyTotalValueOfUsd) / dailyTotalValueOfUsd * 100,
                    CommonConstant.PercentageValueDecimals);
        }


        result.TokenHoldings = holderInfos.Count;

        if (!lastTransaction.Items.IsNullOrEmpty())
        {
            result.LastTransactionSend = OfTransactionInfo(lastTransaction.Items.First());
        }

        if (!firstTransaction.Items.IsNullOrEmpty())
        {
            result.FirstTransactionSend = OfTransactionInfo(firstTransaction.Items.First());
        }

        return result;
    }

    public async Task<GetAddressTokenListResultDto> GetAddressTokenListAsync(
        GetAddressTokenListInput input)
    {
        input.SetDefaultSort();
        Dictionary<string, IndexerTokenInfoDto> tokenDict;
        IndexerTokenHolderInfoListDto holderInfos;
        //search token name or symbol
        if (!input.Search.IsNullOrWhiteSpace())
        {
            var tokenListInput = _objectMapper.Map<GetAddressTokenListInput, TokenListInput>(input);
            var tokenInfos = await _tokenIndexerProvider.GetAllTokenInfosAsync(tokenListInput);
            if (tokenInfos.IsNullOrEmpty())
            {
                return new GetAddressTokenListResultDto();
            }

            tokenDict = tokenInfos.ToDictionary(i => i.Symbol, i => i);
            holderInfos = await GetTokenHolderInfosAsync(input, searchSymbols: tokenDict.Keys.ToList());
            if (holderInfos.Items.IsNullOrEmpty())
            {
                return new GetAddressTokenListResultDto();
            }
        }
        else
        {
            holderInfos = await GetTokenHolderInfosAsync(input);
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
        if (!input.Search.IsNullOrWhiteSpace())
        {
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

            if (tokenDict.TryGetValue(symbol, out var tokenInfo))
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

            if (tokenDict.TryGetValue(symbol, out var tokenInfo))
            {
                tokenHolderInfo.Token = _tokenInfoProvider.OfTokenBaseInfo(tokenInfo);
            }

            if (tokenDict.TryGetValue(collectionSymbol, out var collectionInfo))
            {
                tokenHolderInfo.NftCollection = _tokenInfoProvider.OfTokenBaseInfo(collectionInfo);
            }

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
            BlockHeight = transferInfoDto.Metadata.Block.BlockHeight,
            BlockTime = transferInfoDto.Metadata.Block.BlockTime
        };
    }
}