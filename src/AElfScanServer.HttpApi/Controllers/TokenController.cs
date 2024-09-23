using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.HttpApi.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Controllers;

[AggregateExecutionTime]
[RemoteService]
[Area("app")]
[ControllerName("Token")]
[Route("api/app/token")]
public class TokenController : AbpControllerBase
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpGet("list")]
    public async Task<ListResponseDto<TokenCommonDto>> GetTokenListAsync(TokenListInput input)
    {
        return await _tokenService.GetTokenListAsync(input);
    }

    [HttpGet("detail")]
    public async Task<TokenDetailDto> GetTokenListAsync(string symbol, string chainId="")
    {
        return await _tokenService.GetMergeTokenDetailAsync(symbol, chainId);
    }

    [HttpGet("transfers")]
    public async Task<TokenTransferInfosDto> GetTokenTransferInfosAsync(
        TokenTransferInput input)
    {
        return await _tokenService.GetTokenTransferInfosAsync(input);
    }

    [HttpGet("holders")]
    public async Task<ListResponseDto<TokenHolderInfoDto>> GetTokenTransferInfoAsync(
        TokenHolderInput input)
    {
        return await _tokenService.GetTokenHolderInfosAsync(input);
    }

    [HttpGet("price")]
    public async Task<CommonTokenPriceDto> GetTokenPriceInfoAsync(CurrencyDto input)
    {
        return await _tokenService.GetTokenPriceInfoAsync(input);
    }

    [HttpGet("info")]
    public async Task<IndexerTokenInfoDto> GetTokenBaseInfoAsync(string symbol, string chainId)
    {
        return await _tokenService.GetTokenBaseInfoAsync(symbol, chainId);
    }
}