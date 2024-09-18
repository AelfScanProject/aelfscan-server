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

public class LatestTransactionsWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<LatestTransactionsWorker> _logger;
    private readonly IOptionsMonitor<PullTransactionChainIdsOptions> _workerOptions;

    private readonly DataStrategyContext<string, TransactionsResponseDto> _latestTransactionsDataStrategy;

    public LatestTransactionsWorker(AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<LatestTransactionsWorker> logger,
        LatestTransactionDataStrategy latestTransactionsDataStrategy,
        IOptionsMonitor<PullTransactionChainIdsOptions> workerOptions
    ) : base(timer,
        serviceScopeFactory)
    {
        _logger = logger;
        timer.Period = 1000 * 2;
        _latestTransactionsDataStrategy =
            new DataStrategyContext<string, TransactionsResponseDto>(latestTransactionsDataStrategy);
        _workerOptions = workerOptions;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _latestTransactionsDataStrategy.LoadData("");
        foreach (var chainId in _workerOptions.CurrentValue.ChainIds)
        {
            _logger.LogInformation("Start to load latest transaction for chain {chainId}", chainId);
            await _latestTransactionsDataStrategy.LoadData(chainId);
        }
    }
}