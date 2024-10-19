using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos.ChartData;
using AElfScanServer.HttpApi.Dtos.OpenApi;
using AElfScanServer.HttpApi.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Controllers;

[AggregateExecutionTime]
[RemoteService]
[Area("app")]
[ControllerName("statistics")]
[Route("api/app/statistics")]
public class OpenApiController : AbpController
{
    private readonly IOpenApiService _openApiService;

    public OpenApiController(IOpenApiService openApiService)
    {
        _openApiService = openApiService;
    }


    [HttpGet("supply")]
    public async Task<SupplyApiResp> GetSupplyAsync()
    {
        return await _openApiService.GetSupplyAsync();
    }

    [HttpGet("dailyTransactionInfo")]
    public async Task<DailyTransactionCountApiResp> GetDailyTransactionInfoAsync(string startDate, string endDate)
    {
        return await _openApiService.GetDailyTransactionCountAsync(startDate, endDate);
    }

    [HttpGet("dailyActivityAddress")]
    public async Task<DailyActivityAddressApiResp> GetDailyActivityAddressAsync(string startDate, string endDate)
    {
        return await _openApiService.GetDailyActivityAddressAsync(startDate, endDate);
    }

    [HttpGet("currencyPrice")]
    public async Task<List<CurrencyPrice>> GetCurrencyPriceAsync()
    {
        return await _openApiService.GetCurrencyPriceAsync();
    }
}