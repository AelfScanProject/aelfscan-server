using System.Collections.Generic;

namespace AElfScanServer.Worker.Core.Options;

public class TokenTransferMonitoringOptions
{
    /// <summary>
    /// Enable or disable token transfer monitoring, default is true
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// Enable or disable system contract transfer filtering, default is true
    /// </summary>
    public bool EnableSystemContractFilter { get; set; } = true;

    /// <summary>
    /// Blacklist addresses for monitoring
    /// </summary>
    public List<string> BlacklistAddresses { get; set; } = new();

    /// <summary>
    /// Addresses that are only monitored when they are recipients (to addresses)
    /// </summary>
    public List<string> ToOnlyMonitoredAddresses { get; set; } = new();

    /// <summary>
    /// Addresses that are only monitored for large amount transfers
    /// </summary>
    public List<string> LargeAmountOnlyAddresses { get; set; } = new();

    /// <summary>
    /// Addresses to ignore completely - no metrics will be recorded for these addresses
    /// </summary>
    public List<string> IgnoreAddresses { get; set; } = new();

    /// <summary>
    /// Minimum USD value threshold for histogram recording, default is 0
    /// </summary>
    public decimal MinUsdValueThreshold { get; set; } = 0m;

    /// <summary>
    /// List of tokens to monitor
    /// </summary>
    public List<string> MonitoredTokens { get; set; } = new() { "ELF", "USDT", "BTC", "ETH" };

    /// <summary>
    /// Scan configuration
    /// </summary>
    public ScanConfig ScanConfig { get; set; } = new();
}

public class ScanConfig
{
    /// <summary>
    /// Chain IDs to monitor
    /// </summary>
    public List<string> ChainIds { get; set; } = new() { "AELF", "tDVV" };

    /// <summary>
    /// Scan interval in seconds
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Batch size for each scan
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Redis key prefix for storing scan progress
    /// </summary>
    public string RedisKeyPrefix { get; set; } = "token_transfer_monitoring";
} 