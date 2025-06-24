using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker.MergeDataWorker;

public class DeleteMergeBlocksWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IAddressService _addressService;

    private readonly ILogger<DeleteMergeBlocksWorker> _logger;


    public DeleteMergeBlocksWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<DeleteMergeBlocksWorker> logger, IAddressService addressService) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = 1000 * 60 * 60;
        _logger = logger;
        _addressService = addressService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _addressService.DeleteMergeBlock();
        
    }
}