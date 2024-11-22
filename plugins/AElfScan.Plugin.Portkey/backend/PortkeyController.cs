using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.HttpApi.Dtos;
using Asp.Versioning;
using CAServer.CAActivity.Provider;
using Microsoft.AspNetCore.Mvc;
using Portkey.Dtos;
using Volo.Abp;

namespace Portkey.backend;
[RemoteService]
[Area("app")]
[ControllerName("Portkey")]
[Route("api/app/portkey/")]
public class PortkeyController
{
    private readonly IPortkeyService _portkeyService;

    public PortkeyController(IPortkeyService portkeyService)
    {
        _portkeyService = portkeyService;
    }

    [HttpPost("transactions")]
    public async Task<TransactionsResponseDto> GetTransactionsAsync(GetTransactionsReq input)
    {
        return await _portkeyService.GetTransactionsAsync(input);
    }

}