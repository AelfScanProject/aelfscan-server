using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using GetTokenInfoInput = AElf.Client.MultiToken.GetTokenInfoInput;

namespace AElfScanServer.Common.Token.Provider;

public interface ITokenInfoProvider
{
    TokenBaseInfo OfTokenBaseInfo(IndexerTokenInfoDto tokenInfo);

    string BuildImageUrl(string symbol, bool useAssetUrl = false);

    Task<List<TransactionFeeDto>> ConvertTransactionFeeAsync(Dictionary<string, CommonTokenPriceDto> priceDict,
        List<ExternalInfoDto> externalInfos);
}

public class TokenInfoProvider : ITokenInfoProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptionsMonitor;
    private readonly IOptionsMonitor<AssetsInfoOptions> _assetsInfoOptionsMonitor;
    private readonly ITokenPriceService _tokenPriceService;


    private ILogger<TokenInfoProvider> _logger;


    public TokenInfoProvider(IOptionsMonitor<TokenInfoOptions> tokenInfoOptions,
        IOptionsMonitor<AssetsInfoOptions> assetsInfoOptions,
        ITokenPriceService tokenPriceService, ILogger<TokenInfoProvider> logger)
    {
        _tokenInfoOptionsMonitor = tokenInfoOptions;
        _assetsInfoOptionsMonitor = assetsInfoOptions;
        _tokenPriceService = tokenPriceService;

        _logger = logger;
    }

    public TokenBaseInfo OfTokenBaseInfo(IndexerTokenInfoDto tokenInfo)
    {
        return new TokenBaseInfo
        {
            Name = tokenInfo.TokenName,
            Symbol = tokenInfo.Symbol,
            Decimals = tokenInfo.Decimals,
            ImageUrl = TokenInfoHelper.GetImageUrl(tokenInfo.ExternalInfo,
                () => BuildImageUrl(tokenInfo.Symbol))
        };
    }


    public string BuildImageUrl(string symbol, bool useAssetUrl = false)
    {
        if (symbol.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        if (_tokenInfoOptionsMonitor.CurrentValue.TokenInfos.TryGetValue(symbol, out var info))
        {
            return info.ImageUrl;
        }

        if (_assetsInfoOptionsMonitor.CurrentValue.IsEmpty())
        {
            return string.Empty;
        }

        return useAssetUrl ? _assetsInfoOptionsMonitor.CurrentValue.BuildImageUrl(symbol) : string.Empty;
    }

    public async Task<List<TransactionFeeDto>> ConvertTransactionFeeAsync(
        Dictionary<string, CommonTokenPriceDto> priceDict, List<ExternalInfoDto> externalInfos)
    {
        var feeDtos = TokenInfoHelper.GetTransactionFee(externalInfos);
        foreach (var transactionFeeDto in feeDtos)
        {
            if (!priceDict.TryGetValue(transactionFeeDto.Symbol, out var priceDto))
            {
                priceDto = await _tokenPriceService.GetTokenPriceAsync(transactionFeeDto.Symbol,
                    CurrencyConstant.UsdCurrency);
                priceDict[transactionFeeDto.Symbol] = priceDto;
            }

            transactionFeeDto.AmountOfUsd = Math.Round(transactionFeeDto.Amount * priceDto.Price,
                CommonConstant.UsdValueDecimals);
        }

        return feeDtos;
    }
}