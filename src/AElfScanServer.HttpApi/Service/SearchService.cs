using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.NodeProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Service;

public interface ISearchService
{
    public Task<SearchResponseDto> SearchAsync(SearchRequestDto request);
}

[Ump]
public class SearchService : ISearchService, ISingletonDependency
{
    private readonly ILogger<SearchService> _logger;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptions;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly INftInfoProvider _nftInfoProvider;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IIndexerGenesisProvider _indexerGenesisProvider;
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;

    public SearchService(ILogger<SearchService> logger, ITokenIndexerProvider tokenIndexerProvider,
        IOptionsMonitor<GlobalOptions> globalOptions, INftInfoProvider nftInfoProvider,
        ITokenPriceService tokenPriceService, ITokenInfoProvider tokenInfoProvider,
        IOptionsMonitor<TokenInfoOptions> tokenInfoOptions,
        IIndexerGenesisProvider indexerGenesisProvider,
        AELFIndexerProvider aelfIndexerProvider,
        IBlockchainClientFactory<AElfClient> blockchainClientFactory)
    {
        _logger = logger;
        _tokenIndexerProvider = tokenIndexerProvider;
        _globalOptions = globalOptions;
        _nftInfoProvider = nftInfoProvider;
        _tokenPriceService = tokenPriceService;
        _tokenInfoProvider = tokenInfoProvider;
        _tokenInfoOptions = tokenInfoOptions;
        _aelfIndexerProvider = aelfIndexerProvider;
        _blockchainClientFactory = blockchainClientFactory;
        _indexerGenesisProvider = indexerGenesisProvider;
    }

    public async Task<SearchResponseDto> SearchAsync(SearchRequestDto request)
    {
        var searchResp = new SearchResponseDto();
        try
        {
            //Step 1: check param
            if (!ValidParam(request.ChainId, request.Keyword))
            {
                return searchResp;
            }
            //Step 2: convert 

            //Step 3: execute query
            switch (request.FilterType)
            {
                case FilterTypes.Accounts:
                    await AssemblySearchAddressAsync(searchResp, request);
                    break;
                case FilterTypes.Contracts:
                    await AssemblySearchAddressAsync(searchResp, request);
                    break;
                case FilterTypes.Tokens:
                    await AssemblySearchTokenAsync(searchResp, request, new List<SymbolType> { SymbolType.Token });
                    break;
                case FilterTypes.Nfts:
                    await AssemblySearchTokenAsync(searchResp, request,
                        new List<SymbolType> { SymbolType.Nft, SymbolType.Nft_Collection });
                    break;
                case FilterTypes.AllFilter:
                    var tokenTask =
                        AssemblySearchTokenAsync(searchResp, request, new List<SymbolType> { SymbolType.Token });
                    var nftTask = AssemblySearchTokenAsync(searchResp, request,
                        new List<SymbolType> { SymbolType.Nft, SymbolType.Nft_Collection });
                    var addressTask = AssemblySearchAddressAsync(searchResp, request);
                    // var contractAddressTask = AssemblySearchContractAddressAsync(searchResp, request);
                    var txTask = AssemblySearchTransactionAsync(searchResp, request);
                    var blockTask = AssemblySearchBlockAsync(searchResp, request);
                    await Task.WhenAll(tokenTask, nftTask, addressTask, txTask, blockTask);
                    break;
            }

            return searchResp;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Execute search error.");
            return searchResp;
        }
    }

    private bool ValidParam(string chainId, string keyword)
    {
        if (string.IsNullOrEmpty(chainId))
        {
            return !Regex.IsMatch(keyword, CommonConstant.SearchKeyPattern);
        }
        else
        {
            return (_globalOptions.CurrentValue.ChainIds.Exists(s => s == chainId)
                    && !Regex.IsMatch(keyword, CommonConstant.SearchKeyPattern));
        }
    }


    private async Task AssemblySearchAddressAsync(SearchResponseDto searchResponseDto, SearchRequestDto request)
    {
        try
        {
            AElf.Types.Address.FromBase58(request.Keyword);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "address is invalid,{keyword}", request.Keyword);
            return;
        }


        var contractAddressList = await FindContractAddress(request.ChainId, request.Keyword);


        if (!contractAddressList.IsNullOrEmpty())
        {
            foreach (var contractInfoDto in contractAddressList)
            {
                searchResponseDto.Contracts.Add(new SearchContract
                {
                    Address = request.Keyword,
                    Name = BlockHelper.GetContractName(_globalOptions.CurrentValue, contractInfoDto.Metadata.ChainId,
                        request.Keyword),
                    ChainIds = new List<string>() { contractInfoDto.Metadata.ChainId }
                });
            }
        }
        else
        {
            if (request.ChainId.IsNullOrEmpty())
            {
                var findEoaAddress = await FindEoaAddress(request.Keyword);
                searchResponseDto.Accounts = findEoaAddress;
            }
            else
            {
                searchResponseDto.Accounts.Add(new SearchAccount()
                {
                    Address = request.Keyword,
                    ChainIds = new List<string>() { request.ChainId }
                });
            }
        }
    }

    public async Task<List<ContractInfoDto>> FindContractAddress(string chainId, string contractAddress)
    {
        var contractListAsync =
            await _indexerGenesisProvider.GetContractListAsync(chainId, 0, 1, "", "",
                contractAddress);

        if (!contractListAsync.ContractList.Items.IsNullOrEmpty())
        {
            return contractListAsync.ContractList.Items;
        }

        return new List<ContractInfoDto>();
    }

    public async Task<List<SearchAccount>> FindEoaAddress(string address)
    {
        var result = new List<SearchAccount>();
        var holderInput = new TokenHolderInput { ChainId = "", Address = address };
        holderInput.SetDefaultSort();

        var tokenHolderInfos = await _tokenIndexerProvider.GetTokenHolderInfoAsync(holderInput);
        var dic = new Dictionary<string, SearchAccount>();

        foreach (var indexerTokenHolderInfoDto in tokenHolderInfos.Items)
        {
            if (dic.TryGetValue(indexerTokenHolderInfoDto.Address, out var v))
            {
                v.ChainIds.Add(indexerTokenHolderInfoDto.Metadata.ChainId);
            }
            else
            {
                dic.Add(indexerTokenHolderInfoDto.Address, new SearchAccount()
                {
                    Address = indexerTokenHolderInfoDto.Address,
                    ChainIds = new List<string>() { indexerTokenHolderInfoDto.Metadata.ChainId }
                });
            }
        }

        result.AddRange(dic.Values);
        return result;
    }


    private async Task AssemblySearchTokenAsync(SearchResponseDto searchResponseDto, SearchRequestDto request,
        List<SymbolType> types)
    {
        var input = new TokenListInput { ChainId = request.ChainId, Types = types };
        if (request.SearchType == SearchTypes.ExactSearch)
        {
            input.ExactSearch = request.Keyword;
        }
        else
        {
            input.FuzzySearch = request.Keyword.ToLower();
        }

        input.SetDefaultSort();
        var indexerTokenInfoList = await _tokenIndexerProvider.GetTokenListAsync(input);
        if (indexerTokenInfoList.Items.IsNullOrEmpty())
        {
            return;
        }

        var priceDict = new Dictionary<string, CommonTokenPriceDto>();
        var symbols = indexerTokenInfoList.Items.Select(i => i.Symbol).Distinct().ToList();
        //batch query nft price
        var lastSaleInfoDict = new Dictionary<string, NftActivityItem>();
        if (types.Contains(SymbolType.Nft))
        {
            lastSaleInfoDict = await _nftInfoProvider.GetLatestPriceAsync(request.ChainId, symbols);
        }

        var searchTokensDic = new Dictionary<string, SearchToken>();
        var searchTNftsDic = new Dictionary<string, SearchToken>();

        var elfOfUsdPriceTask = GetTokenOfUsdPriceAsync(priceDict, CurrencyConstant.ElfCurrency);
        foreach (var tokenInfo in indexerTokenInfoList.Items)
        {
            var searchToken = new SearchToken
            {
                Name = tokenInfo.TokenName, Symbol = tokenInfo.Symbol, Type = tokenInfo.Type,
                Image = await _tokenIndexerProvider.GetTokenImageAsync(tokenInfo.Symbol, tokenInfo.IssueChainId,
                    tokenInfo.ExternalInfo)
            };
            switch (tokenInfo.Type)
            {
                case SymbolType.Token:
                {
                    if (searchTokensDic.TryGetValue(tokenInfo.Symbol, out var v))
                    {
                        v.ChainIds.Add(tokenInfo.Metadata.ChainId);
                    }
                    else
                    {
                        if (_tokenInfoOptions.CurrentValue.NonResourceSymbols.Contains(tokenInfo.Symbol))
                        {
                            var price = await GetTokenOfUsdPriceAsync(priceDict, tokenInfo.Symbol);
                            searchToken.Price = Math.Round(price, CommonConstant.UsdPriceValueDecimals);
                        }

                        searchToken.ChainIds.Add(tokenInfo.Metadata.ChainId);

                        searchTokensDic[tokenInfo.Symbol] = searchToken;
                    }

                    break;
                }
                case SymbolType.Nft:
                {
                    if (searchTNftsDic.TryGetValue(tokenInfo.Symbol, out var v))
                    {
                        v.ChainIds.Add(tokenInfo.Metadata.ChainId);
                    }
                    else
                    {
                        var elfOfUsdPrice = await elfOfUsdPriceTask;
                        var elfPrice = lastSaleInfoDict.TryGetValue(tokenInfo.Symbol, out var priceDto)
                            ? priceDto.Price
                            : 0;
                        searchToken.Price = Math.Round(elfPrice * elfOfUsdPrice, CommonConstant.UsdPriceValueDecimals);
                        searchToken.ChainIds.Add(tokenInfo.Metadata.ChainId);
                        searchTNftsDic[tokenInfo.Symbol] = searchToken;
                    }

                    break;
                }
                case SymbolType.Nft_Collection:
                {
                    if (searchTNftsDic.TryGetValue(tokenInfo.Symbol, out var v))
                    {
                        v.ChainIds.Add(tokenInfo.Metadata.ChainId);
                    }
                    else
                    {
                        searchToken.ChainIds.Add(tokenInfo.Metadata.ChainId);
                        searchTNftsDic[tokenInfo.Symbol] = searchToken;
                    }

                    break;
                }
            }
        }

        searchResponseDto.Tokens.AddRange(searchTokensDic.Values);
        searchResponseDto.Nfts.AddRange(searchTNftsDic.Values);
    }

    private async Task SearchMergeBlockAsync(SearchResponseDto searchResponseDto, SearchRequestDto request)
    {
        if (!BlockHelper.IsBlockHeight(request.Keyword))
        {
            return;
        }

        var mainBlockList = new List<IndexerBlockDto>();
        var sideBlockList = new List<IndexerBlockDto>();
        var tasks = new List<Task>();

        var blockHeight = long.Parse(request.Keyword);

        tasks.Add(_aelfIndexerProvider.GetLatestBlocksAsync("AELF", blockHeight, blockHeight).ContinueWith(
            task => { mainBlockList.AddRange(task.Result); }));

        tasks.Add(_aelfIndexerProvider
            .GetLatestBlocksAsync(_globalOptions.CurrentValue.SideChainId, blockHeight, blockHeight).ContinueWith(
                task => { sideBlockList.AddRange(task.Result); }));

        await tasks.WhenAll();

        if (!mainBlockList.IsNullOrEmpty())
        {
            var blockDto = mainBlockList[0];
            searchResponseDto.Blocks.Add(new SearchBlock
            {
                BlockHash = blockDto.BlockHash,
                BlockHeight = blockDto.BlockHeight
            });
        }

        if (!sideBlockList.IsNullOrEmpty())
        {
            var blockDto = sideBlockList[0];
            searchResponseDto.Blocks.Add(new SearchBlock
            {
                BlockHash = blockDto.BlockHash,
                BlockHeight = blockDto.BlockHeight
            });
        }
    }

    private async Task AssemblySearchBlockAsync(SearchResponseDto searchResponseDto, SearchRequestDto request)
    {
        if (request.ChainId.IsNullOrEmpty())
        {
            await SearchMergeBlockAsync(searchResponseDto, request);
            return;
        }

        if (!BlockHelper.IsBlockHeight(request.Keyword))
        {
            return;
        }

        var blockHeight = long.Parse(request.Keyword);
        var blockDtos = await _aelfIndexerProvider.GetLatestBlocksAsync(request.ChainId, blockHeight, blockHeight);

        if (!blockDtos.IsNullOrEmpty())
        {
            var blockDto = blockDtos[0];
            searchResponseDto.Blocks.Add(new SearchBlock
            {
                BlockHash = blockDto.BlockHash,
                BlockHeight = blockDto.BlockHeight
            });
        }
    }

    private async Task AssemblySearchTransactionAsync(SearchResponseDto searchResponseDto, SearchRequestDto request)
    {
        if (request.ChainId.IsNullOrEmpty())
        {
            await SearchMergeTransaction(searchResponseDto, request);
            return;
        }

        if (!BlockHelper.IsTxHash(request.Keyword))
        {
            return;
        }


        var transactionResult = await _blockchainClientFactory.GetClient(request.ChainId)
            .GetTransactionResultAsync(request.Keyword);

        if (!transactionResult.TransactionId.IsNullOrEmpty() && transactionResult.Status is "MINED" or "PENDING")
        {
            searchResponseDto.Transaction = new SearchTransaction
            {
                TransactionId = transactionResult.TransactionId,
                BlockHash = transactionResult.BlockHash,
                BlockHeight = transactionResult.BlockNumber,
                ChainIds = new List<string> { request.ChainId }
            };
        }
    }

    private async Task SearchMergeTransaction(SearchResponseDto searchResponseDto, SearchRequestDto request)
    {
        if (!BlockHelper.IsTxHash(request.Keyword))
        {
            return;
        }

        var mainChainTxn = new TransactionResultDto();
        var sideChainTxn = new TransactionResultDto();


        var tasks = new List<Task>();

        tasks.Add(_blockchainClientFactory.GetClient("AELF")
            .GetTransactionResultAsync(request.Keyword).ContinueWith(task => { mainChainTxn = task.Result; }));

        tasks.Add(_blockchainClientFactory.GetClient(_globalOptions.CurrentValue.SideChainId)
            .GetTransactionResultAsync(request.Keyword).ContinueWith(task => { sideChainTxn = task.Result; }));

        await tasks.WhenAll();

        if (!mainChainTxn.TransactionId.IsNullOrEmpty() && mainChainTxn.Status is "MINED" or "PENDING")
        {
            searchResponseDto.Transaction = new SearchTransaction
            {
                TransactionId = mainChainTxn.TransactionId,
                BlockHash = mainChainTxn.BlockHash,
                BlockHeight = mainChainTxn.BlockNumber,
                ChainIds = new List<string>
                {
                    "AELF"
                }
            };
        }

        if (!sideChainTxn.TransactionId.IsNullOrEmpty() && sideChainTxn.Status is "MINED" or "PENDING")
        {
            searchResponseDto.Transaction = new SearchTransaction
            {
                TransactionId = mainChainTxn.TransactionId,
                BlockHash = mainChainTxn.BlockHash,
                BlockHeight = mainChainTxn.BlockNumber,
                ChainIds = new List<string>
                {
                    _globalOptions.CurrentValue.SideChainId
                }
            };
        }
    }

    private async Task<decimal> GetTokenOfUsdPriceAsync(Dictionary<string, CommonTokenPriceDto> priceDict,
        string symbol)
    {
        if (priceDict.TryGetValue(symbol, out var priceDto))
        {
            return priceDto.Price;
        }

        priceDto = await _tokenPriceService.GetTokenPriceAsync(symbol,
            CurrencyConstant.UsdCurrency);
        priceDict[symbol] = priceDto;

        return priceDto.Price;
    }

    private Dictionary<string, string> MergeDict(string chainId, Dictionary<string, string>? contractNameDict,
        Dictionary<string, ContractInfoDto>? contractInfoDict)
    {
        var result = new Dictionary<string, string>();
        if (contractNameDict != null)
        {
            foreach (var kvp in contractNameDict)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        if (contractInfoDict != null)
        {
            foreach (var kvp in contractInfoDict)
            {
                if (!result.ContainsKey(kvp.Key))
                {
                    var name = _globalOptions.CurrentValue.GetContractName(chainId, kvp.Key);
                    result[kvp.Key] = name;
                }
            }
        }

        return result;
    }
}