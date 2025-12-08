using System.Threading.Tasks;
using AElfScanServer.Common.Options;
using AElfScanServer.DataStrategy;
using AElfScanServer.HttpApi.DataStrategy;
using AElfScanServer.HttpApi.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class CurrentBpProduceWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<CurrentBpProduceWorker> _logger;
    private readonly IOptionsMonitor<PullTransactionChainIdsOptions> _workerOptions;

    private readonly DataStrategyContext<string, BlockProduceInfoDto> _bpDataStrategy;

    public CurrentBpProduceWorker(AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CurrentBpProduceWorker> logger,
        CurrentBpProduceDataStrategy bpDataStrategy,
        IOptionsMonitor<PullTransactionChainIdsOptions> workerOptions
    ) : base(timer,
        serviceScopeFactory)
    {
        _logger = logger;
        timer.Period = 1000 * 2;
        _bpDataStrategy =
            new DataStrategyContext<string, BlockProduceInfoDto>(bpDataStrategy);
        _workerOptions = workerOptions;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        foreach (var chainId in _workerOptions.CurrentValue.ChainIds)
        {
            _logger.LogInformation("Start to load  bp produce info for chain {chainId}", chainId);
            await _bpDataStrategy.LoadData(chainId);
        }
    }
}