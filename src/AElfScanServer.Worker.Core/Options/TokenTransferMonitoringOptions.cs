using System.Collections.Generic;

namespace AElfScanServer.Worker.Core.Options;

public class TokenTransferMonitoringOptions
{
    /// <summary>
    /// Blacklist addresses for monitoring
    /// </summary>
    public List<string> BlacklistAddresses { get; set; } = new();

    /// <summary>
    /// List of tokens to monitor
    /// </summary>
    public List<string> MonitoredTokens { get; set; } = new() { "ELF", "USDT", "BTC", "ETH" };

    /// <summary>
    /// Scan configuration
    /// </summary>
    public ScanConfig ScanConfig { get; set; } = new();
    /// <summary>
    /// Enable monitoring flag
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;
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