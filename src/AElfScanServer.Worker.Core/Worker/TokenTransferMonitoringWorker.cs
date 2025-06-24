using System;
using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Options;
using AElfScanServer.Worker.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class TokenTransferMonitoringWorker : AsyncPeriodicBackgroundWorkerBase
{
    private const string WorkerName = "TokenTransferMonitoringWorker";
    private readonly ILogger<TokenTransferMonitoringWorker> _logger;
    private readonly IOptionsMonitor<TokenTransferMonitoringOptions> _optionsMonitor;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;

    public TokenTransferMonitoringWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TokenTransferMonitoringWorker> logger,
        IOptionsMonitor<TokenTransferMonitoringOptions> optionsMonitor,
        IOptionsMonitor<WorkerOptions> workerOptions) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _workerOptions = workerOptions;
        
        var intervalSeconds = _optionsMonitor.CurrentValue.ScanConfig.IntervalSeconds;
        timer.Period = intervalSeconds * 1000;
        
        _logger.LogInformation("TokenTransferMonitoringWorker initialized with interval: {Interval}s", intervalSeconds);
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var options = _optionsMonitor.CurrentValue;
        
        if (!options.EnableMonitoring)
        {
            _logger.LogDebug("Token transfer monitoring is disabled");
            return;
        }

        _logger.LogInformation("Starting token transfer monitoring scan...");
        
        using var scope = ServiceScopeFactory.CreateScope();
        var monitoringService = scope.ServiceProvider.GetRequiredService<ITokenTransferMonitoringService>();

        var chainIds = options.ScanConfig.ChainIds;
        var batchSize = options.ScanConfig.BatchSize;

        foreach (var chainId in chainIds)
        {
            try
            {
                await ProcessChainTransfers(monitoringService, chainId, batchSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process transfers for chain {ChainId}", chainId);
            }
        }

        _logger.LogInformation("Token transfer monitoring scan completed");
    }

    private async Task ProcessChainTransfers(ITokenTransferMonitoringService monitoringService, 
        string chainId, int batchSize)
    {
        _logger.LogDebug("Processing transfers for chain {ChainId}", chainId);

        _logger.LogInformation("Starting to process transfers for chain {ChainId}", chainId);

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Get transfer events based on time scanning
            var transfers = await monitoringService.GetTransfersAsync(chainId);
            
            // Process transfers and send metrics
            if (transfers.Count > 0)
            {
                monitoringService.ProcessTransfers(transfers);
                _logger.LogDebug("Processed {Count} transfers for chain {ChainId}", 
                    transfers.Count, chainId);
            }
            else
            {
                _logger.LogDebug("No new transfers found for chain {ChainId}", chainId);
            }
            
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Completed processing chain {ChainId}: {Count} transfers in {Duration}ms", 
                chainId, transfers.Count, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transfers for chain {ChainId}", chainId);
        }
    }
} 