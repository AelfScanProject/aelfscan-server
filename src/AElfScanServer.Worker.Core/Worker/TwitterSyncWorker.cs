using System.Threading.Tasks;
using AElfScanServer.HttpApi.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class TwitterSyncWorker : AsyncPeriodicBackgroundWorkerBase

{
    private readonly IAdsService _adsService;

    private readonly ILogger<TwitterSyncWorker> _logger;


    public TwitterSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<TwitterSyncWorker> logger, IAdsService adsService) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = 1000 * 60 * 60;
        timer.RunOnStart = true;
        _logger = logger;
        _adsService = adsService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _adsService.SaveTwitterListAsync();
    }
}