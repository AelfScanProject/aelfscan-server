using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos;
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
[ControllerName("contractFile")]
[Route("api/app/contractfile")]
public class ContractUpdateController : AbpController
{
    private readonly IContractVerifyService _contractVerifyService;

    public ContractUpdateController(IChartDataService chartDataService, IContractVerifyService contractVerifyService)
    {
        _contractVerifyService = contractVerifyService;
    }


    [HttpPost("update")]
    public async Task<string> UpdateContract(string chainId, string contractAddress)
    {
        return await _contractVerifyService.UploadContractStatesAsync(chainId, contractAddress);
    }
}