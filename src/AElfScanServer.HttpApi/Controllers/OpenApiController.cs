using AElf.OpenTelemetry.ExecutionTime;
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
[Route("v1/api/statistics")]
public class OpenApiController : AbpController
{
    private readonly IChartDataService _chartDataService;

    public OpenApiController(IChartDataService chartDataService)
    {
        _chartDataService = chartDataService;
    }


}