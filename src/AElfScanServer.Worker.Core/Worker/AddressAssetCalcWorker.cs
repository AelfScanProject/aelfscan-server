using System.Threading.Tasks;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Worker.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class AddressAssetCalcWorker : AsyncPeriodicBackgroundWorkerBase
{
    private const string WorkerName = "AddressAssetCalcWorker";
    private readonly ILogger<AddressAssetCalcWorker> _logger;
    private readonly ITokenAssetProvider _tokenAssetProvider;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;

    public AddressAssetCalcWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<AddressAssetCalcWorker> logger, IOptionsMonitor<WorkerOptions> workerOptions,
        ITokenAssetProvider tokenAssetProvider) : base(timer, serviceScopeFactory)
    {
        timer.Period = workerOptions.CurrentValue.GetWorkerPeriodMinutes(WorkerName) * 60 * 1000;
        _logger = logger;
        _workerOptions = workerOptions;
        _tokenAssetProvider = tokenAssetProvider;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chainIds = _workerOptions.CurrentValue.GetChainIds();
        foreach (var chainId in chainIds)
        {
            await _tokenAssetProvider.HandleDailyTokenValuesAsync(chainId);
        }
    }
}