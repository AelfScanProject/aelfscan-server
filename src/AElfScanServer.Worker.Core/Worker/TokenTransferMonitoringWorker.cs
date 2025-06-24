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

    public TokenTransferMonitoringWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TokenTransferMonitoringWorker> logger,
        IOptionsMonitor<TokenTransferMonitoringOptions> optionsMonitor,
        IOptionsMonitor<WorkerOptions> workerOptions) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        
        // Use WorkerOptions for timer period configuration, fallback to TokenTransferMonitoringOptions
        var workerPeriodMinutes = workerOptions.CurrentValue.GetWorkerPeriodMinutes(WorkerName);
        if (workerPeriodMinutes == Options.Worker.DefaultMinutes) // If not configured in WorkerOptions, use TokenTransferMonitoringOptions
        {
            var intervalSeconds = _optionsMonitor.CurrentValue.ScanConfig.IntervalSeconds;
            timer.Period = intervalSeconds * 1000;
            _logger.LogInformation("TokenTransferMonitoringWorker initialized with TokenTransferMonitoringOptions interval: {Interval}s", intervalSeconds);
        }
        else
        {
            timer.Period = workerPeriodMinutes * 60 * 1000;
            _logger.LogInformation("TokenTransferMonitoringWorker initialized with WorkerOptions interval: {Interval} minutes", workerPeriodMinutes);
        }
        
        timer.RunOnStart = true; // Ensure the worker starts immediately
        
        _logger.LogInformation("TokenTransferMonitoringWorker configured successfully");
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        try
        {
            var options = _optionsMonitor.CurrentValue;
            
            if (!options.EnableMonitoring)
            {
                _logger.LogDebug("Token transfer monitoring is disabled");
                return;
            }

            _logger.LogInformation("Starting Token transfer monitoring scan...");
            
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
                    // Continue processing other chains even if one fails
                }
            }

            _logger.LogInformation("Token transfer monitoring scan completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in TokenTransferMonitoringWorker");
        }
    }

    private async Task ProcessChainTransfers(ITokenTransferMonitoringService monitoringService, 
        string chainId, int batchSize)
    {
        _logger.LogDebug("Processing transfers for chain {ChainId}", chainId);

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Get transfer events based on time scanning
            var transfers = await monitoringService.GetTransfersAsync(chainId);
            
            // Process transfers and send metrics
            if (transfers.Count > 0)
            {
                monitoringService.ProcessTransfers(transfers);
                _logger.LogInformation("Processed {Count} transfers for chain {ChainId}", 
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
            throw; // Re-throw to be caught by the caller
        }
    }
} 