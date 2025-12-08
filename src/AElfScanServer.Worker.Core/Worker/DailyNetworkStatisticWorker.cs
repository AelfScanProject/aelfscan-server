using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class DailyNetworkStatisticWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ITransactionService _transactionService;

    private readonly ILogger<DailyNetworkStatisticWorker> _logger;


    public DailyNetworkStatisticWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<DailyNetworkStatisticWorker> logger, ITransactionService transactionService) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = 1000 * 60 * 60;
        _logger = logger;
        _transactionService = transactionService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await Task.Delay(1000  * 5);
        await _transactionService.UpdateDailyNetwork();
    }
}