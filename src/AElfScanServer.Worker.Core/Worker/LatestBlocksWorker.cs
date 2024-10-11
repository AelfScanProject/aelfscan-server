using System;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.DataStrategy;
using AElfScanServer.Common.Options;
using AElfScanServer.DataStrategy;
using AElfScanServer.HttpApi.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class LatestBlocksWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<LatestBlocksWorker> _logger;
    private readonly IOptionsMonitor<PullTransactionChainIdsOptions> _workerOptions;

    private readonly DataStrategyContext<string, BlocksResponseDto> _latestBlockssDataStrategy;

    public LatestBlocksWorker(AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<LatestBlocksWorker> logger,
        LatestBlocksDataStrategy latestBlocksDataStrategy,
        IOptionsMonitor<PullTransactionChainIdsOptions> workerOptions
    ) : base(timer,
        serviceScopeFactory)
    {
        _logger = logger;
        timer.Period = 1000 * 2;
        _latestBlockssDataStrategy =
            new DataStrategyContext<string, BlocksResponseDto>(latestBlocksDataStrategy);
        _workerOptions = workerOptions;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        foreach (var chainId in _workerOptions.CurrentValue.ChainIds)
        {
            _logger.LogInformation("Start to load latest blocks for chain {chainId}", chainId);
            try
            {
                await _latestBlockssDataStrategy.LoadData(chainId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
    }
}