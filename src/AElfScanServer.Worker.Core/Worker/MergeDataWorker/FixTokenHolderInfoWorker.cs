using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker.MergeDataWorker;

public class FixTokenHolderInfoWorker : AsyncPeriodicBackgroundWorkerBase

{
    private readonly IAddressService _addressService;

    private readonly ILogger<FixTokenHolderInfoWorker> _logger;


    public FixTokenHolderInfoWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<FixTokenHolderInfoWorker> logger, IAddressService addressService) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = 1000 * 60 * 5;
        timer.RunOnStart = true;
        _logger = logger;
        _addressService = addressService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _addressService.FixTokenHolderAsync();
    }
}