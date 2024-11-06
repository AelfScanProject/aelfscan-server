using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Dtos.ChartData;
using AElfScanServer.HttpApi.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Controllers;

[AggregateExecutionTime]
[RemoteService]
[Area("app")]
[ControllerName("contractVerify")]
[Route("api/contractfile")]
public class ContractVerifyController : AbpController
{
    private readonly IContractVerifyService _contractVerifyService;

    public ContractVerifyController(IChartDataService chartDataService, IContractVerifyService contractVerifyService)
    {
        _contractVerifyService = contractVerifyService;
    }


    [HttpPost("upload")]
    public async Task<UploadContractFileResponseDto> AddSubscriptionAsync(IFormFile file, string chainId,
        string contractAddress, string csprojPath,string dotnetVersion)
    {
        return await _contractVerifyService.UploadContractFileAsync(file, chainId, contractAddress, csprojPath,dotnetVersion);
    }
}