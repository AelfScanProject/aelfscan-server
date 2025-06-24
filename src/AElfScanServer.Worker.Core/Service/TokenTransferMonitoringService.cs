using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Globalization;
using AElf.OpenTelemetry;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Worker.Core.Dtos;
using AElfScanServer.Worker.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Worker.Core.Service;

public class TokenTransferMonitoringService : ITokenTransferMonitoringService, ISingletonDependency
{
    private const int DefaultMaxResultCount = 1000;
    private const int SafetyRecordLimit = 10000;
    private const int DefaultScanTimeMinutes = -60;
    private const string LastScanTimeKey = "last_scan_time";
    
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly ILogger<TokenTransferMonitoringService> _logger;
    private readonly TokenTransferMonitoringOptions _options;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly Histogram<double> _transferEventsHistogram;
    private readonly HashSet<string> _blacklistAddresses;

    public TokenTransferMonitoringService(
        ITokenIndexerProvider tokenIndexerProvider,
        IDistributedCache<string> distributedCache,
        ILogger<TokenTransferMonitoringService> logger,
        IOptions<TokenTransferMonitoringOptions> options,
        IOptionsMonitor<GlobalOptions> globalOptions)
    {
        _tokenIndexerProvider = tokenIndexerProvider;
        _distributedCache = distributedCache;
        _logger = logger;
        _options = options.Value;
        _globalOptions = globalOptions;
        
        // Initialize address sets for fast lookup
        _blacklistAddresses = new HashSet<string>(_options.BlacklistAddresses, StringComparer.OrdinalIgnoreCase);
        
        // Initialize histogram with configured buckets
        var meter = new Meter("AElfScan.TokenTransfer");
        _transferEventsHistogram = meter.CreateHistogram<double>(
            "aelf_transfer_events",
            "Token transfer events with amount distribution",
            "ELF");
    }

    public async Task<List<TransferEventDto>> GetTransfersAsync(string chainId)
    {
        var transfers = new List<TransferEventDto>();
        DateTime? latestBlockTime = null;
        
        try
        {
            var beginBlockTime = await GetLastScanTimeAsync(chainId) ?? DateTime.UtcNow.AddMinutes(DefaultScanTimeMinutes);
            var skip = 0;
            
            while (skip < SafetyRecordLimit)
            {
                var batchResult = await GetTransferBatchAsync(chainId, beginBlockTime, skip);
                if (!batchResult.hasData)
                    break;

                transfers.AddRange(batchResult.transfers);
                
                // Track the latest block time from the data
                if (batchResult.latestBlockTime.HasValue)
                {
                    latestBlockTime = latestBlockTime.HasValue 
                        ? (batchResult.latestBlockTime > latestBlockTime ? batchResult.latestBlockTime : latestBlockTime)
                        : batchResult.latestBlockTime;
                }
                
                if (batchResult.transfers.Count < DefaultMaxResultCount)
                    break;

                skip += DefaultMaxResultCount;
            }

            if (skip >= SafetyRecordLimit)
            {
                _logger.LogWarning("Reached safety limit of {Limit} records for chain {ChainId}", SafetyRecordLimit, chainId);
            }

            // Only update last scan time when we actually processed data
            if (latestBlockTime.HasValue)
            {
                await UpdateLastScanTimeAsync(chainId, latestBlockTime.Value);
                _logger.LogInformation("Retrieved {Count} transfers for chain {ChainId} from time {BeginTime}, updated to latest block time: {LatestBlockTime}", 
                    transfers.Count, chainId, beginBlockTime, latestBlockTime.Value);
            }
            else
            {
                _logger.LogInformation("No transfers found for chain {ChainId} from time {BeginTime}, scan time not updated", 
                    chainId, beginBlockTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfers for chain {ChainId}", chainId);
        }

        return transfers;
    }

    private async Task<(List<TransferEventDto> transfers, long maxHeight, bool hasData, DateTime? latestBlockTime)> GetTransferBatchAsync(
        string chainId, DateTime beginBlockTime, int skip)
    {
        var input = new TokenTransferInput
        {
            ChainId = chainId,
            BeginBlockTime = beginBlockTime,
            SkipCount = skip,
            MaxResultCount = DefaultMaxResultCount,
            Types = new List<SymbolType> { SymbolType.Token }
        };

        var result = await _tokenIndexerProvider.GetTokenTransfersAsync(input);
        
        if (result?.List == null || !result.List.Any())
        {
            return (new List<TransferEventDto>(), 0, false, null);
        }

        var transfers = result.List
            .Where(item => _options.MonitoredTokens.Contains(item.Symbol))
            .Select(ConvertToTransferEventDto)
            .ToList();

        var maxHeight = result.List.Max(x => x.BlockHeight);
        
        // Track the latest block time from the data
        var latestBlockTime = result.List.Max(x => x.DateTime);
        
        return (transfers, maxHeight, true, latestBlockTime);
    }

    private async Task<DateTime?> GetLastScanTimeAsync(string chainId)
    {
        var timeString = await GetRedisValueAsync(LastScanTimeKey, chainId);
        if (!string.IsNullOrEmpty(timeString) && DateTime.TryParse(timeString, null, DateTimeStyles.RoundtripKind, out var lastTime))
        {
            // Ensure the parsed time is treated as UTC
            return lastTime.Kind == DateTimeKind.Utc ? lastTime : DateTime.SpecifyKind(lastTime, DateTimeKind.Utc);
        }
        return null;
    }

    private async Task UpdateLastScanTimeAsync(string chainId, DateTime scanTime)
    {
        // Ensure the time is UTC before saving
        var utcTime = scanTime.Kind == DateTimeKind.Utc ? scanTime : scanTime.ToUniversalTime();
        await SetRedisValueAsync(LastScanTimeKey, chainId, utcTime.ToString("O"));
    }

    private async Task<string> GetRedisValueAsync(string keyType, string chainId)
    {
        try
        {
            var key = BuildRedisKey(keyType, chainId);
            return await _distributedCache.GetAsync(key) ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis value for key type {KeyType}, chain {ChainId}", keyType, chainId);
            return "";
        }
    }

    private async Task SetRedisValueAsync(string keyType, string chainId, string value)
    {
        try
        {
            var key = BuildRedisKey(keyType, chainId);
            await _distributedCache.SetAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Redis value for key type {KeyType}, chain {ChainId}", keyType, chainId);
        }
    }

    public void ProcessTransfer(TransferEventDto transfer)
    {
        try
        {
            // Skip metrics for system contract transfers
            if (IsSystemContractTransfer(transfer.FromAddress))
            {
                _logger.LogDebug("Skipping metrics for system contract transfer from {FromAddress}", transfer.FromAddress);
                return;
            }

            SendTransferMetrics(transfer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transfer {TransactionId}", transfer.TransactionId);
        }
    }

    /// <summary>
    /// Check if the from address is a system contract address
    /// </summary>
    private bool IsSystemContractTransfer(string fromAddress)
    {
        if (string.IsNullOrEmpty(fromAddress))
            return false;

        var contractNames = _globalOptions.CurrentValue.ContractNames;
        if (contractNames == null)
            return false;

        // Check if the address exists in any chain's contract names
        return contractNames.Values.Any(chainContracts => 
            chainContracts != null && chainContracts.ContainsKey(fromAddress));
    }

    public void ProcessTransfers(List<TransferEventDto> transfers)
    {
        foreach (var transfer in transfers)
        {
            ProcessTransfer(transfer);
        }
    }

    public void SendTransferMetrics(TransferEventDto transfer)
    {
        try
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("chain_id", transfer.ChainId),
                new("symbol", transfer.Symbol),
                new("transfer_type", transfer.Type.ToString()),
                new("from_address", transfer.FromAddress),
                new("to_address", transfer.ToAddress),
                new("from_address_type", transfer.FromAddressType.ToString()),
                new("to_address_type", transfer.ToAddressType.ToString()),
                new("transaction_id", transfer.TransactionId),
            };

            _transferEventsHistogram.Record((double)transfer.Amount, tags);
            
            _logger.LogDebug("Sent transfer metrics for transaction {TransactionId}, amount {Amount} {Symbol}", 
                transfer.TransactionId, transfer.Amount, transfer.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transfer metrics for transaction {TransactionId}", 
                transfer.TransactionId);
        }
    }

    private TransferEventDto ConvertToTransferEventDto(TokenTransferInfoDto dto)
    {
        return new TransferEventDto
        {
            ChainId = dto.ChainId,
            TransactionId = dto.TransactionId,
            BlockHeight = dto.BlockHeight,
            Timestamp = dto.DateTime,
            Symbol = dto.Symbol,
            FromAddress = dto.From?.Address ?? "",
            ToAddress = dto.To?.Address ?? "",
            Amount = dto.Quantity,
            Type = ParseTransferType(dto.Method),
            FromAddressType = ClassifyAddress(dto.From?.Address ?? ""),
            ToAddressType = ClassifyAddress(dto.To?.Address ?? "")
        };
    }

    private AddressClassification ClassifyAddress(string address)
    {
        return _blacklistAddresses.Contains(address) 
            ? AddressClassification.Blacklist 
            : AddressClassification.Normal;
    }

    private static TransferType ParseTransferType(string method)
    {
        return method?.ToLower() switch
        {
            "transfer" => TransferType.Transfer,
            "burn" => TransferType.Burn,
            "crosschaintransfer" => TransferType.CrossChainTransfer,
            "crosschainreceive" => TransferType.CrossChainReceive,
            _ => TransferType.Transfer
        };
    }

    private string BuildRedisKey(string keyType, string chainId)
    {
        return $"{_options.ScanConfig.RedisKeyPrefix}:{keyType}:{chainId}";
    }
} 