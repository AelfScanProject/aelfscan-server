using System;
using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class TransactionIndexWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ITransactionService _transactionService;

    private readonly ILogger<TransactionIndexWorker> _logger;

    public TransactionIndexWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<TransactionIndexWorker> logger, ITransactionService transactionService) : base(timer,
        serviceScopeFactory)
    {
        timer.Period = 1000 * 60 * 60;
        _logger = logger;
        _transactionService = transactionService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _transactionService.BatchPullTransactionTask();
    }
}