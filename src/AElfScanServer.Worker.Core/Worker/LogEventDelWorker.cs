using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Options;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
namespace AElfScanServer.Worker.Core.Worker;
public class LogEventDelWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<LogEventDelWorker> _logger;
    private const string WorkerName = "LogEventDelWorker";
    public LogEventDelWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<LogEventDelWorker> logger, ITransactionService transactionService, IOptionsMonitor<WorkerOptions> workerOptions) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = workerOptions.CurrentValue.GetWorkerPeriodMinutes(WorkerName) * 60 * 1000;
        _logger = logger;
        _transactionService = transactionService;
    }
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _transactionService.DelLogEventTask();
    }
}
