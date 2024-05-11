using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.BlockChain;
using AElfScanServer.Token.Dtos;
using AElfScanServer.Constant;
using AElfScanServer.Dtos;
using AElfScanServer.Dtos.Indexer;
using AElfScanServer.Helper;
using AElfScanServer.Options;
using AElfScanServer.Token;
using AElfScanServer.Token.Provider;
using AElfScanServer.TokenDataFunction.Dtos.Indexer;
using AElfScanServer.TokenDataFunction.Dtos.Input;
using AElfScanServer.TokenDataFunction.Provider;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

using TokenPriceDto = AElfScanServer.Dtos.TokenPriceDto;
using TokenTransferInfoDto = AElfScanServer.Token.Dtos.TokenTransferInfoDto;

namespace AElfScanServer.TokenDataFunction.Service;

public interface ITokenService
{
    public Task<ListResponseDto<TokenCommonDto>> GetTokenListAsync(TokenListInput input);
    public Task<TokenDetailDto> GetTokenDetailAsync(string symbol, string chainId);
    public Task<TokenTransferInfosDto> GetTokenTransferInfosAsync(TokenTransferInput input);
    public Task<ListResponseDto<TokenHolderInfoDto>> GetTokenHolderInfosAsync(TokenHolderInput input);
    Task<TokenPriceDto> GetTokenPriceInfoAsync(CurrencyDto input);
    Task<IndexerTokenInfoDto> GetTokenBaseInfoAsync(string symbol, string chainId);
}

public class TokenService : ITokenService, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockChainProvider _blockChainProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly ITokenHolderPercentProvider _tokenHolderPercentProvider;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptions;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    

    public TokenService(ITokenIndexerProvider tokenIndexerProvider, IBlockChainProvider blockChainProvider,
        ITokenHolderPercentProvider tokenHolderPercentProvider, IObjectMapper objectMapper,
        IOptionsMonitor<ChainOptions> chainOptions, ITokenPriceService tokenPriceService, 
        IOptionsMonitor<TokenInfoOptions> tokenInfoOptions, ITokenInfoProvider tokenInfoProvider)
    {
        _objectMapper = objectMapper;
        _chainOptions = chainOptions;
        _tokenPriceService = tokenPriceService;
        _tokenInfoOptions = tokenInfoOptions;
        _tokenInfoProvider = tokenInfoProvider;
        _blockChainProvider = blockChainProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenHolderPercentProvider = tokenHolderPercentProvider;
    }

    public async Task<ListResponseDto<TokenCommonDto>> GetTokenListAsync(TokenListInput input)
    { 
        input.SetDefaultSort();
        
        var indexerTokenListDto = await _tokenIndexerProvider.GetTokenListAsync(input);

        if (indexerTokenListDto.Items.IsNullOrEmpty())
        {
            return new ListResponseDto<TokenCommonDto>();
        }

        var list = await ConvertIndexerTokenDtoAsync(indexerTokenListDto.Items, input.ChainId);

        return new ListResponseDto<TokenCommonDto>
        {
            Total = indexerTokenListDto.TotalCount,
            List = list
        };
    }

    public async Task<TokenDetailDto> GetTokenDetailAsync(string symbol, string chainId)
    {
        var indexerTokenList = await _tokenIndexerProvider.GetTokenDetailAsync(chainId, symbol);

        AssertHelper.NotEmpty(indexerTokenList, "this token not exist");

        var list = await ConvertIndexerTokenDtoAsync(indexerTokenList, chainId);

        var tokenInfo = list[0];
        
        var tokenDetailDto = _objectMapper.Map<TokenCommonDto, TokenDetailDto>(tokenInfo);
        tokenDetailDto.TokenContractAddress = _chainOptions.CurrentValue.GetChainInfo(chainId)?.TokenContractAddress;
        if (_tokenInfoOptions.CurrentValue.NonResourceSymbols.Contains(symbol))
        { 
            //set others
            var priceDto = await _tokenPriceService.GetTokenPriceAsync(symbol, CurrencyConstant.UsdCurrency);
            var timestamp = TimeHelper.GetTimeStampFromDateTime(DateTime.Today);
            var priceHisDto = await _tokenPriceService.GetTokenHistoryPriceAsync(symbol, CurrencyConstant.UsdCurrency, timestamp);
            tokenDetailDto.Price = Math.Round(priceDto.Price, CommonConstant.UsdValueDecimals);
            if (priceHisDto.Price > 0)
            {
                tokenDetailDto.PricePercentChange24h = (double)Math.Round((priceDto.Price - priceHisDto.Price) / priceHisDto.Price  * 100, 
                    CommonConstant.PercentageValueDecimals);
            }
        }
        return tokenDetailDto;
    }

    public async Task<TokenTransferInfosDto> GetTokenTransferInfosAsync(TokenTransferInput input)
    {
        var indexerTokenTransfer = await _tokenIndexerProvider.GetTokenTransferInfoAsync(input);
        if (indexerTokenTransfer.Items.IsNullOrEmpty())
        {
            return new TokenTransferInfosDto();
        }
        var list = await ConvertIndexerTokenTransferDtoAsync(indexerTokenTransfer.Items, input.ChainId);
        var result = new TokenTransferInfosDto
        {
            Total = indexerTokenTransfer.TotalCount,
            List = list
        };
        if (input.IsSearchAddress())
        {
            result.IsAddress = true;
            var holderInfo = await _tokenIndexerProvider.GetHolderInfoAsync(input.ChainId, input.Symbol, input.Search);
            result.Balance = holderInfo.Balance;
            var priceDto = await _tokenPriceService.GetTokenPriceAsync(input.Symbol, CurrencyConstant.UsdCurrency);
            result.Value = Math.Round(result.Balance * priceDto.Price, CommonConstant.UsdValueDecimals);
        }
        return result;
    }

    public async Task<ListResponseDto<TokenHolderInfoDto>> GetTokenHolderInfosAsync(TokenHolderInput input)
    {
        input.SetDefaultSort();
        
        var indexerTokenHolderInfo = await _tokenIndexerProvider.GetTokenHolderInfoAsync(input);

        var list = await ConvertIndexerTokenHolderInfoDtoAsync(indexerTokenHolderInfo.Items, input.ChainId, input.Symbol);

        return new ListResponseDto<TokenHolderInfoDto>
        {
            Total = indexerTokenHolderInfo.TotalCount,
            List = list
        };
    }

    public async Task<TokenPriceDto> GetTokenPriceInfoAsync(CurrencyDto input)
    {
        return await _tokenPriceService.GetTokenPriceAsync(input.BaseCurrency, input.QuoteCurrency);
    }

    public async Task<IndexerTokenInfoDto> GetTokenBaseInfoAsync(string symbol, string chainId)
    {
        var indexerTokenList = await _tokenIndexerProvider.GetTokenDetailAsync(chainId, symbol);

        AssertHelper.NotEmpty(indexerTokenList, "this token not exist");

        return indexerTokenList[0];
    }

    private async Task<List<TokenHolderInfoDto>> ConvertIndexerTokenHolderInfoDtoAsync(
        List<IndexerTokenHolderInfoDto> indexerTokenHolderInfo, string chainId, string symbol)
    {
        var indexerTokenList = await _tokenIndexerProvider.GetTokenDetailAsync(chainId, symbol);

        var list = new List<TokenHolderInfoDto>();

        if (indexerTokenList.IsNullOrEmpty())
        {
            return list;
        }

        var tokenSupply = indexerTokenList[0].Supply;
        
        var priceDto = await _tokenPriceService.GetTokenPriceAsync(symbol, CurrencyConstant.UsdCurrency);
        
        foreach (var indexerTokenHolderInfoDto in indexerTokenHolderInfo)
        {
            var tokenHolderInfoDto =
                _objectMapper.Map<IndexerTokenHolderInfoDto, TokenHolderInfoDto>(indexerTokenHolderInfoDto);

            if (!indexerTokenHolderInfoDto.Address.IsNullOrEmpty())
            {
                tokenHolderInfoDto.Address = new CommonAddressDto
                {
                    Address = indexerTokenHolderInfoDto.Address
                };
            }
            if (tokenSupply != 0)
            {
                tokenHolderInfoDto.Percentage =
                    Math.Round((decimal)indexerTokenHolderInfoDto.Amount / tokenSupply * 100, CommonConstant.PercentageValueDecimals);
            }

            tokenHolderInfoDto.Value = Math.Round(tokenHolderInfoDto.Quantity * priceDto.Price, CommonConstant.UsdValueDecimals);
            list.Add(tokenHolderInfoDto);
        }
        return list;
    }
    
    private async Task<List<TokenTransferInfoDto>> ConvertIndexerTokenTransferDtoAsync(
        List<IndexerTransferInfoDto> indexerTokenTransfer, string chainId)
    {
        var list = new List<TokenTransferInfoDto>();
        var priceDict = new Dictionary<string, TokenPriceDto>();
        foreach (var indexerTransferInfoDto in indexerTokenTransfer)
        {
            var tokenTransferDto =
                _objectMapper.Map<IndexerTransferInfoDto, TokenTransferInfoDto>(indexerTransferInfoDto);
            tokenTransferDto.TransactionFeeList = await _tokenInfoProvider.ConvertTransactionFeeAsync(priceDict, indexerTransferInfoDto.ExtraProperties);
            if (!indexerTransferInfoDto.From.IsNullOrEmpty())
            {
                tokenTransferDto.From = new CommonAddressDto
                {
                    Address = indexerTransferInfoDto.From
                };
            }
            if (!indexerTransferInfoDto.To.IsNullOrEmpty())
            {
                tokenTransferDto.From = new CommonAddressDto
                {
                    Address = indexerTransferInfoDto.To
                };
            }

            list.Add(tokenTransferDto);
        }

        return list;
    }

    private async Task<List<TokenCommonDto>> ConvertIndexerTokenDtoAsync(List<IndexerTokenInfoDto> indexerTokenList,
        string chainId)
    {
        var tokenHolderCountDic =
            await _tokenHolderPercentProvider.GetTokenHolderCount(chainId, DateTime.Now.ToString("yyyyMMdd"));

        var list = new List<TokenCommonDto>();
        foreach (var indexerTokenInfoDto in indexerTokenList)
        {
            var tokenListDto = _objectMapper.Map<IndexerTokenInfoDto, TokenCommonDto>(indexerTokenInfoDto);
            tokenListDto.TotalSupply = DecimalHelper.Divide(tokenListDto.TotalSupply, indexerTokenInfoDto.Decimals);
            tokenListDto.CirculatingSupply = DecimalHelper.Divide(tokenListDto.CirculatingSupply, indexerTokenInfoDto.Decimals);
            //handle image url
            tokenListDto.Token.ImageUrl = TokenInfoHelper.GetImageUrl(indexerTokenInfoDto.ExternalInfo,
                () => _tokenInfoProvider.BuildImageUrl(indexerTokenInfoDto.Symbol));
            if (tokenHolderCountDic.TryGetValue(indexerTokenInfoDto.Symbol, out var beforeCount) && beforeCount != 0)
            {
                tokenListDto.HolderPercentChange24H = Math.Round(
                    (double)(tokenListDto.Holders - beforeCount) / beforeCount * 100, CommonConstant.PercentageValueDecimals);
            }

            list.Add(tokenListDto);
        }

        return list;
    }

    private async Task<Dictionary<string, TokenCommonDto>> GetTokenDicAsync(List<string> symbols, string chainId)
    {
        var input = new TokenListInput()
        {
            ChainId = chainId,
            Symbols = symbols
        };
        var indexerTokenListDto = await _tokenIndexerProvider.GetTokenListAsync(input);
        var tokenInfoDtoList = _objectMapper.Map<List<IndexerTokenInfoDto>, List<TokenCommonDto>>(indexerTokenListDto.Items);
        return tokenInfoDtoList.ToDictionary(token => token.Token.Symbol, token => token);
    }
}