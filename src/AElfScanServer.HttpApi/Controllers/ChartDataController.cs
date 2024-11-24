using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos.ChartData;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.HttpApi.Dtos.ChartData;
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
public class ChartDataController : AbpController
{
    private readonly IChartDataService _chartDataService;

    public ChartDataController(IChartDataService chartDataService)
    {
        _chartDataService = chartDataService;
    }

    [HttpGet("dailyTransactions")]
    public async Task<DailyTransactionCountResp> GetDailyTransactionCountAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyTransactionCountAsync(request);
    }

    [HttpGet("uniqueAddresses")]
    public async Task<UniqueAddressCountResp> GetUniqueAddressesCountAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetUniqueAddressCountAsync(request);
    }


    [HttpGet("dailyActiveAddresses")]
    public async Task<ActiveAddressCountResp> GetActiveAddressesCountAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetActiveAddressCountAsync(request);
    }


    [HttpGet("monthlyActiveAddresses")]
    public async Task<MonthlyActiveAddressCountResp> GetMonthlyActiveAddressesCountAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetMonthlyActiveAddressCountAsync(request);
    }


    [HttpGet("blockProduceRate")]
    public async Task<BlockProduceRateResp> GetBlockProduceRateAsync()
    {
        return await _chartDataService.GetBlockProduceRateAsync();
    }

    [HttpGet("avgBlockDuration")]
    public async Task<AvgBlockDurationResp> GetAvgBlockDurationRespAsync()
    {
        return await _chartDataService.GetAvgBlockDurationRespAsync();
    }

    [HttpGet("cycleCount")]
    public async Task<CycleCountResp> GetCycleCountRespAsync()
    {
        return await _chartDataService.GetCycleCountRespAsync();
    }


    [HttpGet("nodeBlockProduce")]
    public async Task<NodeBlockProduceResp> GetBlockProduceRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetNodeBlockProduceRespAsync(request);
    }


    [HttpGet("dailyAvgTransactionFee")]
    public async Task<DailyAvgTransactionFeeResp> GetDailyAvgTransactionFeeRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyAvgTransactionFeeRespAsync(request);
    }

    [HttpGet("dailyTxFee")]
    public async Task<DailyTransactionFeeResp> GetTransactionFeeRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyTransactionFeeRespAsync(request);
    }


    [HttpGet("dailyTotalBurnt")]
    public async Task<DailyTotalBurntResp> GetDailyTotalBurntRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyTotalBurntRespAsync(request);
    }


    [HttpGet("dailyElfPrice")]
    public async Task<ElfPriceIndexResp> GetElfPriceIndexRespAsync()
    {
        return await _chartDataService.GetElfPriceIndexRespAsync();
    }


    [HttpGet("dailyDeployContract")]
    public async Task<DailyDeployContractResp> GetDailyDeployContractRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyDeployContractRespAsync(request);
    }


    [HttpGet("dailyBlockReward")]
    public async Task<DailyBlockRewardResp> GetDailyBlockRewardRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyBlockRewardRespAsync(request);
    }


    [HttpGet("dailyAvgBlockSize")]
    public async Task<DailyAvgBlockSizeResp> GetDailyAvgBlockSizeRespRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyAvgBlockSizeRespRespAsync(request);
    }


    [HttpGet("topContractCall")]
    public async Task<TopContractCallResp> GetTopContractCallRespRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetTopContractCallRespAsync(request);
    }


    [HttpGet("dailyContractCall")]
    public async Task<DailyTotalContractCallResp> GetDailyTotalContractCallRespRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyTotalContractCallRespRespAsync(request);
    }


    [HttpGet("dailySupplyGrowth")]
    public async Task<DailySupplyGrowthResp> GetDailySupplyGrowthRespAsync()
    {
        return await _chartDataService.GetDailySupplyGrowthRespAsync();
    }

    [HttpGet("dailyMarketCap")]
    public async Task<DailyMarketCapResp> GetDailyMarketCapRespAsync()
    {
        return await _chartDataService.GetDailyMarketCapRespAsync();
    }


    [HttpGet("dailyStaked")]
    public async Task<DailyStakedResp> GetDailyStakedRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyStakedRespAsync(request);
    }


    [HttpGet("dailyHolder")]
    public async Task<DailyHolderResp> GetDailyHolderRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyHolderRespAsync(request);
    }


    [HttpGet("dailyTvl")]
    public async Task<DailyTVLResp> GetDailyTVLRespAsync(ChartDataRequest request)
    {
        return await _chartDataService.GetDailyTVLRespAsync(request);
    }


    [HttpGet("nodeCurrentProduceInfo")]
    public async Task<NodeProduceBlockInfoResp> GetNodeProduceBlockInfoRespAsync(NodeProduceBlockRequest request)
    {
        return await _chartDataService.GetNodeProduceBlockInfoRespAsync(request);
    }


    [HttpGet("initRound")]
    public async Task<InitRoundResp> SetRoundAsync(SetRoundRequest request)
    {
        return await _chartDataService.InitDailyNetwork(request);
    }
    
    [HttpPost("fixDailyData")]
    public async Task FixDailyData(FixDailyData request)
    {
        await _chartDataService.FixDailyData(request);
    }

    [HttpPost("fixTokenHolder")]
    public async Task FixTokenHolderAsync(FixTokenHolderInput request)
    {
        await _chartDataService.FixTokenHolderAsync(request);
    }
}