using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Commons;
using AElf.ExceptionHandler;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.GraphQL;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token.Provider;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.Common.IndexerPluginProvider;

public interface ITokenIndexerProvider
{
    public Task<IndexerTokenInfoListDto> GetTokenListAsync(TokenListInput input);
    
    public Task<List<IndexerTokenInfoDto>> GetAllTokenInfosAsync(TokenListInput input);
    public Task<List<IndexerTokenInfoDto>> GetTokenDetailAsync(string chainId, string symbol);
    public Task<IndexerTokenTransferListDto> GetTokenTransferInfoAsync(TokenTransferInput input);

    public Task<string> GetTokenImageAsync(string symbol, string chainId,
        List<ExternalInfoDto> externalInfo = null);

    public Task<int> GetAccountCountAsync(string chainId);
    Task<IndexerTokenHolderInfoListDto> GetTokenHolderInfoAsync(TokenHolderInput input);
    Task<List<HolderInfo>> GetHolderInfoAsync(string chainId, string address, List<SymbolType> types);
    Task<HolderInfo> GetHolderInfoAsync(string chainId, string symbol, string address);
    Task<Dictionary<string, IndexerTokenInfoDto>> GetTokenDictAsync(string chainId, List<string> symbols);
    Task<TokenTransferInfosDto> GetTokenTransfersAsync(TokenTransferInput input);


    Task<List<BlockBurnFeeDto>> GetBlockBurntFeeListAsync(string chainId, long startBlockHeight, long endBlockHeight);


    Task<IndexerTokenHolderInfoListDto> GetCollectionHolderInfoAsync(TokenHolderInput input);
    Task<Dictionary<string, IndexerTokenInfoDto>> GetNftDictAsync(string chainId, List<string> symbols);
}

public class TokenIndexerProvider : ITokenIndexerProvider, ISingletonDependency
{
    private readonly IGraphQlFactory _graphQlFactory;
    private readonly IObjectMapper _objectMapper;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptionsMonitor;
    private readonly IGenesisPluginProvider _genesisPluginProvider;
    private readonly string TokenImageUrlKey = "__ft_image_uri";
    private Dictionary<string, string> _tokenImageUrlCache;
    private readonly GlobalOptions _globalOptions;


    private ILogger<TokenIndexerProvider> _logger;


    public TokenIndexerProvider(IGraphQlFactory graphQlFactory, IObjectMapper objectMapper,
        ITokenInfoProvider tokenInfoProvider, IGenesisPluginProvider genesisPluginProvider,
        IOptionsMonitor<GlobalOptions> globalOptions, IOptionsMonitor<TokenInfoOptions> tokenInfoOptions,
        ILogger<TokenIndexerProvider> logger)
    {
        _graphQlFactory = graphQlFactory;
        _objectMapper = objectMapper;
        _tokenInfoProvider = tokenInfoProvider;
        _genesisPluginProvider = genesisPluginProvider;
        _tokenImageUrlCache = new Dictionary<string, string>();
        _logger = logger;
        _globalOptions = globalOptions.CurrentValue;
        _tokenInfoOptionsMonitor = tokenInfoOptions;
    }

    public async Task<List<BlockBurnFeeDto>> GetBlockBurntFeeListAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        var list = new List<BlockBurnFeeDto>();

        var graphQlHelper = GetGraphQlHelper();
        var result = await graphQlHelper.QueryAsync<BlockBurnFeeResultDto>(
            new GraphQLRequest
            {
                Query =
                    @"query($chainId:String!,$beginBlockHeight:Long!,$endBlockHeight:Long!){
                        blockBurnFeeInfo(input: {chainId:$chainId,beginBlockHeight:$beginBlockHeight,endBlockHeight:$endBlockHeight}){
                          items {
                             
                                symbol
                                amount
                                blockHeight
                                
                              
                           }
                        }
                    }",
                Variables = new
                {
                    chainId = chainId, beginBlockHeight = startBlockHeight, endBlockHeight = endBlockHeight
                }
            });

        if (result == null || result.BlockBurnFeeInfo == null)
        {
            return list;
        }

        return result.BlockBurnFeeInfo.Items;
    }


    public async Task<int> GetAccountCountAsync(string chainId)
    {
        var graphQlHelper = GetGraphQlHelper();

        var indexerResult = await graphQlHelper.QueryAsync<AccountCountResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!){
                    accountCount(input: {chainId:$chainId})
                {
                   count
                }
            }",
            Variables = new
            {
                chainId = chainId,
            }
        });
        return indexerResult != null ? indexerResult.AccountCount.Count : 0;
    }

    public async Task<IndexerTokenInfoListDto> GetTokenListAsync(TokenListInput input)
    {
        var graphQlHelper = GetGraphQlHelper();

        var indexerResult = await graphQlHelper.QueryAsync<IndexerTokenInfosDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$skipCount:Int!,$maxResultCount:Int!,$search:String,
                        $types:[SymbolType!],$symbols:[String!],$collectionSymbols:[String!],$beginBlockTime:DateTime,
                        $sort:String,$orderBy:String,$exactSearch:String,$fuzzySearch:String,$searchAfter:[String]){
                    tokenInfo(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,search:$search,types:$types,
                        symbols:$symbols,collectionSymbols:$collectionSymbols,beginBlockTime:$beginBlockTime,sort:$sort,orderBy:$orderBy,
                        exactSearch:$exactSearch,fuzzySearch:$fuzzySearch,searchAfter:$searchAfter})
                {
                   totalCount,
                   items{
                        tokenName,
                        symbol,
                        collectionSymbol,
    					type,
                        decimals,
                        totalSupply,
                        supply,
                        issued,
    					issuer,
                        metadata{chainId,block{blockHash,blockTime,blockHeight}},
                        owner,
                        isPrimaryToken
                        isBurnable,
                        issueChainId,
                        externalInfo { key, value },
                        holderCount,
                        transferCount,
                        itemCount,
                        type
                  }
                }
            }",
            Variables = new
            {
                chainId = input.ChainId, types = input.Types, symbols = input.Symbols, skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount, collectionSymbols = input.CollectionSymbols,
                beginBlockTime = input.BeginBlockTime,
                search = input.Search, sort = input.Sort, orderBy = input.OrderBy,
                exactSearch = input.ExactSearch, fuzzySearch = input.FuzzySearch, searchAfter = input.SearchAfter
            }
        });
        return indexerResult?.TokenInfo ?? new IndexerTokenInfoListDto();
    }
    
    

    public async Task<List<IndexerTokenInfoDto>> GetTokenDetailAsync(string chainId, string symbol)
    {
            var graphQlHelper = GetGraphQlHelper();
        var indexerResult = await graphQlHelper.QueryAsync<IndexerTokenInfosDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$symbols:[String!],$skipCount:Int!,$maxResultCount:Int!){
                    tokenInfo(input: {chainId:$chainId,symbols:$symbols,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                       totalCount,
                       items{
                        tokenName,
                        symbol,
                        collectionSymbol,
                        metadata{chainId,block{blockHash,blockTime,blockHeight}},
    					type,
                        decimals,
                        totalSupply,
                        supply,
                        issued,
    					issuer,
                        owner,
                        isPrimaryToken
                        isBurnable,
                        issueChainId,
                        externalInfo { key, value },
                        holderCount,
                        transferCount,
                        itemCount
                  }
                }
            }",
            Variables = new
            {
                chainId=chainId, symbols = new ArrayList { symbol }, skipCount = 0, maxResultCount = 10
            }
        });
        return indexerResult.TokenInfo?.Items;
    }

    public async Task<IndexerTokenTransferListDto> GetTokenTransferInfoAsync(TokenTransferInput input)
    {
        var graphQlHelper = GetGraphQlHelper();
        var indexerResult = await graphQlHelper.QueryAsync<IndexerTokenTransfersDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$symbol:String!,$address:String,$collectionSymbol:String,
                    $search:String,$skipCount:Int!,$maxResultCount:Int!,$types:[SymbolType!],$beginBlockTime:DateTime,
                    $fuzzySearch:String,$sort:String,$orderBy:String,$searchAfter:[String],$orderInfos:[OrderInfo]){
                    transferInfo(input: {chainId:$chainId,symbol:$symbol,collectionSymbol:$collectionSymbol,address:$address,types:$types,beginBlockTime:$beginBlockTime,search:$search,
                    skipCount:$skipCount,maxResultCount:$maxResultCount,fuzzySearch:$fuzzySearch,sort:$sort,orderBy:$orderBy,searchAfter:$searchAfter,orderInfos:$orderInfos}){     
                    totalCount,
                    items{
                        id,
                        metadata{chainId,block{blockHash,blockTime,blockHeight}},
                        transactionId,
                        from,
                        to,
                        method,
                        amount,
                        formatAmount
                        token {symbol, collectionSymbol, type, decimals}
                        status,
                        extraProperties{ key, value }
                  }                     
                }
            }",
            Variables = new
            {
                chainId = input.ChainId, symbol = input.Symbol, address = input.Address, search = input.Search,
                skipCount = input.SkipCount, maxResultCount = input.MaxResultCount,
                collectionSymbol = input.CollectionSymbol, types = input.Types,
                sort = input.Sort, orderBy = input.OrderBy, fuzzySearch = input.FuzzySearch,
                orderInfos = input.OrderInfos, searchAfter = input.SearchAfter, beginBlockTime = input.BeginBlockTime
            }
        });
        return indexerResult == null ? new IndexerTokenTransferListDto() : indexerResult.TransferInfo;
    }

    public async Task<IndexerTokenHolderInfoListDto> GetTokenHolderInfoAsync(TokenHolderInput input)
    {
        var graphQlHelper = GetGraphQlHelper();
        var indexerResult = await graphQlHelper.QueryAsync<IndexerTokenHolderInfosDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$symbol:String!,$collectionSymbol:String,$skipCount:Int!,$maxResultCount:Int!,$address:String,$addressList:[String],
                    $search:String,$types:[SymbolType!],$symbols:[String],$searchSymbols:[String],
                    $fuzzySearch:String,$sort:String,$orderBy:String,$searchAfter:[String],$orderInfos:[OrderInfo],$amountGreaterThanZero:Boolean){
                    accountToken(input: {chainId:$chainId,symbol:$symbol,collectionSymbol:$collectionSymbol,skipCount:$skipCount,types:$types,
                    search:$search,symbols:$symbols,searchSymbols:$searchSymbols,maxResultCount:$maxResultCount,address:$address,addressList:$addressList,
                    fuzzySearch:$fuzzySearch,sort:$sort,orderBy:$orderBy,searchAfter:$searchAfter,orderInfos:$orderInfos,amountGreaterThanZero:$amountGreaterThanZero}){
                    totalCount,
                    items{
                        id,
                        address,
                        token {
                            symbol,
                            collectionSymbol,
                            type,
                            decimals
                        },
                        metadata{chainId,block{blockHash,blockTime,blockHeight}},
                        amount,
                        formatAmount,
                        transferCount,
                        firstNftTransactionId,
                        firstNftTime                        
                    }
                }
            }",
            Variables = new
            {
                chainId = input.ChainId, symbol = input.Symbol, collectionSymbol = input.CollectionSymbol,
                skipCount = input.SkipCount, maxResultCount = input.MaxResultCount, address = input.Address,
                addressList = input.AddressList,
                types = input.Types, symbols = input.Symbols, searchSymbols = input.SearchSymbols,
                search = input.Search, sort = input.Sort, orderBy = input.OrderBy, fuzzySearch = input.FuzzySearch,
                orderInfos = input.OrderInfos, searchAfter = input.SearchAfter,
                amountGreaterThanZero = input.AmountGreaterThanZero
            }
        });
        return indexerResult == null ? new IndexerTokenHolderInfoListDto() : indexerResult.AccountToken;
    }

    public async Task<IndexerTokenHolderInfoListDto> GetCollectionHolderInfoAsync(TokenHolderInput input)
    {
        var graphQlHelper = GetGraphQlHelper();
        var indexerResult = await graphQlHelper.QueryAsync<IndexerCollectionHolderInfosDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$symbol:String!,$skipCount:Int!,$maxResultCount:Int!,$address:String,$addressList:[String],
                   $sort:String,$orderBy:String,$searchAfter:[String],$orderInfos:[OrderInfo],$amountGreaterThanZero:Boolean){
                    accountCollection(input: {chainId:$chainId,symbol:$symbol,skipCount:$skipCount,
                   maxResultCount:$maxResultCount,address:$address,addressList:$addressList,sort:$sort,orderBy:$orderBy,searchAfter:$searchAfter,orderInfos:$orderInfos,
                   amountGreaterThanZero:$amountGreaterThanZero}){
                    totalCount,
                    items{
                        id,
                        address,
                        token {
                            symbol,
                            type,
                            decimals
                        },
                        metadata{chainId,block{blockHash,blockTime,blockHeight}},
                        formatAmount,
                        transferCount
                    }
                }
            }",
            Variables = new
            {
                chainId = input.ChainId, symbol = input.CollectionSymbol,
                skipCount = input.SkipCount, maxResultCount = input.MaxResultCount, address = input.Address,
                sort = input.Sort, orderBy = input.OrderBy,
                orderInfos = input.OrderInfos, searchAfter = input.SearchAfter, addressList = input.AddressList,
                amountGreaterThanZero = input.AmountGreaterThanZero
            }
        });
        return indexerResult == null ? new IndexerTokenHolderInfoListDto() : indexerResult.AccountCollection;
    }

    [ExceptionHandler(typeof(IOException),typeof(TimeoutException),typeof(Exception), Message = "GetTokenImageAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGetTokenImageAsync), LogTargets = ["symbol","chainId"])]
    public virtual async Task<string> GetTokenImageAsync(string symbol, string chainId,
        List<ExternalInfoDto> externalInfo = null)
    {
       
            var imageUrl = "";
            if (_tokenImageUrlCache.TryGetValue(symbol, out imageUrl))
            {
                return imageUrl;
            }

            if (externalInfo == null)
            {
                var input = new TokenListInput
                {
                    ChainId = chainId,
                    Symbols = new List<string>() { symbol },
                    MaxResultCount = 1,
                    Types = new List<SymbolType>() { SymbolType.Token, SymbolType.Nft }
                };

                var indexerTokenListDto = await GetTokenListAsync(input);
                externalInfo = indexerTokenListDto.Items.First().ExternalInfo;
            }


            var list = externalInfo.Where(e => e.Key == TokenImageUrlKey).Select(e => e.Value).ToList();
            if (!list.IsNullOrEmpty())
            {
                imageUrl = list.First();
                _tokenImageUrlCache.Add(symbol, imageUrl);
                return imageUrl;
            }


            if (_tokenInfoOptionsMonitor.CurrentValue.TokenInfos.TryGetValue(symbol, out var tokenInfo))
            {
                _tokenImageUrlCache.Add(symbol, tokenInfo.ImageUrl);
                return tokenInfo.ImageUrl;
            }

            if (TokenSymbolHelper.GetSymbolType(symbol) == SymbolType.Nft)
            {
                imageUrl = TokenInfoHelper.GetImageUrl(externalInfo,
                    () => (_tokenInfoProvider.BuildImageUrl(symbol)));
                _tokenImageUrlCache.Add(symbol, imageUrl);
                return imageUrl;
            }


            return "";
    }

    private IGraphQlHelper GetGraphQlHelper()
    {
        return _graphQlFactory.GetGraphQlHelper(AElfIndexerConstant.TokenIndexer);
    }

    public async Task<List<HolderInfo>> GetHolderInfoAsync(string chainId, string address, List<SymbolType> types)
    {
        return await GetHolderInfosAsync(chainId, address, types);
    }

    public async Task<HolderInfo> GetHolderInfoAsync(string chainId, string symbol, string address)
    {
        var tokenHolderInput = new TokenHolderInput
        {
            ChainId = chainId,
            Address = address,
            Symbol = symbol
        };
        var indexerNftHolder = await GetTokenHolderInfoAsync(tokenHolderInput);

        var holderInfo = new HolderInfo();
        foreach (var indexerTokenHolderInfoDto in indexerNftHolder.Items)
        {
            holderInfo.Balance += indexerTokenHolderInfoDto.FormatAmount;
            holderInfo.Symbol = indexerTokenHolderInfoDto.Token.Symbol;
        }


        return holderInfo;
    }

    private async Task<List<HolderInfo>> GetHolderInfosAsync(string chainId, string address, List<SymbolType> types)
    {
        var tokenHolderInput = new TokenHolderInput
        {
            ChainId = chainId,
            Address = address,
            Types = types,
            MaxResultCount = CommonConstant.DefaultMaxResultCount
        };
        var allHolderInfos = new List<HolderInfo>();
        var skipCount = 0L;
        while (true)
        {
            tokenHolderInput.SkipCount = skipCount;
            var indexerNftHolder = await GetTokenHolderInfoAsync(tokenHolderInput);
            if (indexerNftHolder.Items.Count == 0)
            {
                break;
            }

            var holderInfos = indexerNftHolder.Items.Select(i => new HolderInfo
            {
                Balance = i.FormatAmount,
                Symbol = i.Token.Symbol,
                ChainId = i.Metadata.ChainId
            }).ToList();
            allHolderInfos.AddRange(holderInfos);
            if (indexerNftHolder.Items.Count < tokenHolderInput.MaxResultCount)
            {
                break;
            }

            skipCount += tokenHolderInput.MaxResultCount;
        }

        if (chainId.IsNullOrEmpty())
        {
            allHolderInfos = allHolderInfos.DistinctBy(c => c.Symbol).ToList();
        }


        return allHolderInfos;
    }

    public async Task<Dictionary<string, IndexerTokenInfoDto>> GetTokenDictAsync(string chainId, List<string> symbols)
    {
        var input = new TokenListInput
        {
            ChainId = chainId,
            Symbols = symbols,
            Types = EnumConverter.GetEnumValuesList<SymbolType>(),
            MaxResultCount = symbols.Count * 2
        };
        var indexerTokenListDto = await GetTokenListAsync(input);
        return indexerTokenListDto.Items.ToDictionary(token => token.Symbol + token.Metadata.ChainId, token => token);
    }


    public async Task<Dictionary<string, IndexerTokenInfoDto>> GetNftDictAsync(string chainId, List<string> symbols)
    {
        var input = new TokenListInput
        {
            ChainId = chainId,
            Symbols = symbols,
            Types = EnumConverter.GetEnumValuesList<SymbolType>(),
            MaxResultCount = symbols.Count
        };
        var indexerTokenListDto = await GetTokenListAsync(input);
        return indexerTokenListDto.Items.ToDictionary(token => token.Symbol, token => token);
    }


    public async Task<TokenTransferInfosDto> GetTokenTransfersAsync(TokenTransferInput input)
    {
        input.SetBlockTimeSort();

        var indexerTokenTransfer = await GetTokenTransferInfoAsync(input);
        if (indexerTokenTransfer.Items.IsNullOrEmpty())
        {
            return new TokenTransferInfosDto();
        }

        var list = await ConvertIndexerTokenTransferDtoAsync(indexerTokenTransfer.Items, input.ChainId);

        list = list.OrderByDescending(item => item.DateTime)
            .ThenByDescending(item => item.TransactionId)
            .ToList();


        var result = new TokenTransferInfosDto
        {
            Total = indexerTokenTransfer.TotalCount,
            List = list
        };
        return result;
    }

    private async Task<List<TokenTransferInfoDto>> ConvertIndexerTokenTransferDtoAsync(
        List<IndexerTransferInfoDto> indexerTokenTransfer, string chainId)
    {
        var list = new List<TokenTransferInfoDto>();
        var priceDict = new Dictionary<string, CommonTokenPriceDto>();
        var symbols = indexerTokenTransfer.Select(i => i.Token.Symbol).Distinct().ToList();
        var addressList = indexerTokenTransfer
            .SelectMany(c => new[] { c.From, c.To })
            .Where(value => !string.IsNullOrEmpty(value)).Distinct().ToList();
        var contractInfoDictTask = _genesisPluginProvider.GetContractListAsync(chainId, addressList);
        var tokenDictTask = GetTokenDictAsync(chainId, symbols);
        await Task.WhenAll(contractInfoDictTask, tokenDictTask);
        var contractInfoDict = await contractInfoDictTask;
        var tokenDict = await tokenDictTask;
        foreach (var indexerTransferInfoDto in indexerTokenTransfer)
        {
            var tokenTransferDto =
                _objectMapper.Map<IndexerTransferInfoDto, TokenTransferInfoDto>(indexerTransferInfoDto);
            if (tokenDict.TryGetValue(indexerTransferInfoDto.Token.Symbol + indexerTransferInfoDto.Metadata.ChainId,
                    out var tokenInfo))
            {
                tokenTransferDto.Symbol = tokenInfo.Symbol;
                tokenTransferDto.SymbolName = tokenInfo.TokenName;
                tokenTransferDto.SymbolImageUrl =
                    await GetTokenImageAsync(tokenInfo.Symbol, tokenInfo.IssueChainId, tokenInfo.ExternalInfo);
            }

         
            tokenTransferDto.TransactionFeeList =
                await _tokenInfoProvider.ConvertTransactionFeeAsync(priceDict, indexerTransferInfoDto.ExtraProperties);

            tokenTransferDto.From = CommonAddressHelper.GetCommonAddress(indexerTransferInfoDto.From,
                indexerTransferInfoDto.Metadata.ChainId, contractInfoDict, _globalOptions.ContractNames);
            tokenTransferDto.To = CommonAddressHelper.GetCommonAddress(indexerTransferInfoDto.To,
                indexerTransferInfoDto.Metadata.ChainId, contractInfoDict, _globalOptions.ContractNames);
            if (tokenTransferDto.Method.Equals(TransferMethodName.CrossChainTransfer))
            {
                tokenTransferDto.To.ChainId = GetCrossChainId(indexerTransferInfoDto);
            }
            if (tokenTransferDto.Method.Equals(TransferMethodName.CrossChainReceive))
            {
                tokenTransferDto.From.ChainId = GetCrossChainId(indexerTransferInfoDto);
            }
            tokenTransferDto.ChainIds.Add(indexerTransferInfoDto.Metadata.ChainId);
            list.Add(tokenTransferDto);
        }

        return list;
    }

    private string GetCrossChainId(IndexerTransferInfoDto indexerTransferInfoDto)
    {
        return indexerTransferInfoDto.Metadata.ChainId == CommonConstant.MainChainId ?_globalOptions.SideChainId : CommonConstant.MainChainId;
    }


    public async Task<List<IndexerTokenInfoDto>> GetAllTokenInfosAsync(TokenListInput input)
    {
        var allTokenInfos = new List<IndexerTokenInfoDto>();
        long skipCount = 0;
        const long maxResultCount = CommonConstant.DefaultMaxResultCount;
        while (true)
        {
            input.SkipCount = skipCount;
            input.MaxResultCount = maxResultCount;

            var tokenInfos = await GetTokenListAsync(input);
            if (tokenInfos.Items.IsNullOrEmpty())
            {
                break;
            }

            allTokenInfos.AddRange(tokenInfos.Items);
            if (tokenInfos.Items.Count < maxResultCount)
            {
                break;
            }

            skipCount += maxResultCount;
        }

        return allTokenInfos;
    }
}