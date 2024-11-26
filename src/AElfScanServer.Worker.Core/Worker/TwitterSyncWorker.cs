using System.Threading.Tasks;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Worker.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class TwitterSyncWorker : AsyncPeriodicBackgroundWorkerBase

{
    private readonly IAdsService _adsService;

    private readonly ILogger<TwitterSyncWorker> _logger;
    private const string WorkerName = "TwitterSyncWorker";



    public TwitterSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<TwitterSyncWorker> logger, IAdsService adsService, IOptionsMonitor<WorkerOptions> workerOptions) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = workerOptions.CurrentValue.GetWorkerPeriodMinutes(WorkerName) * 60 * 1000;
        timer.RunOnStart = true;
        _logger = logger;
        _adsService = adsService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _adsService.SaveTwitterListAsync();
    }
}