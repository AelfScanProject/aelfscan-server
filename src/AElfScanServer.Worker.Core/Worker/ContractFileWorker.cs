using System.Threading.Tasks;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Worker.Core.Options;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class ContractFileWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IContractAppService _contractAppService;

    private readonly ILogger<ContractFileWorker> _logger;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;

    public ContractFileWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<ContractFileWorker> logger,
        IOptionsMonitor<WorkerOptions> workerOptions,
        IContractAppService contractAppService) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = 1000 * 60 * 5;
        _logger = logger;
        _workerOptions = workerOptions;
        _contractAppService = contractAppService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chainIds = _workerOptions.CurrentValue.GetChainIds();
        foreach (var chainId in chainIds)
        {
          await  _contractAppService.SaveContractFileAsync(chainId);
        }
    }
}