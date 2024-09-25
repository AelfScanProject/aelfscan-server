using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker.MergeDataWorker;

public class TempTokenInfoWorker : AsyncPeriodicBackgroundWorkerBase

{
    private readonly IAddressService _addressService;

    private readonly ILogger<TokenInfoWorker> _logger;


    public TempTokenInfoWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<TokenInfoWorker> logger, IAddressService addressService) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = 1000 * 60 * 60 * 24 * 5;
        timer.RunOnStart = true;
        _logger = logger;
        _addressService = addressService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _addressService.SaveCollectionHolderList();
    }
}