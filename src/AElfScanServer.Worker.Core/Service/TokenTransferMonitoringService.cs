using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Globalization;
using AElf.OpenTelemetry;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
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
    private const decimal MinUsdValueThreshold = 0m;
    
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly ILogger<TokenTransferMonitoringService> _logger;
    private readonly TokenTransferMonitoringOptions _options;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly Histogram<double> _transferUSDEventsHistogram;
    private readonly Counter<long> _transferCountsCounter;
    private readonly HashSet<string> _blacklistAddresses;
    private readonly HashSet<string> _toOnlyMonitoredAddresses;
    private readonly HashSet<string> _largeAmountOnlyAddresses;
    private readonly IOptionsMonitor<TokenTransferMonitoringOptions> _optionsMonitor;

    public TokenTransferMonitoringService(
        ITokenIndexerProvider tokenIndexerProvider,
        IDistributedCache<string> distributedCache,
        ILogger<TokenTransferMonitoringService> logger,
        IOptions<TokenTransferMonitoringOptions> options,
        IOptionsMonitor<GlobalOptions> globalOptions,
        IOptionsMonitor<TokenTransferMonitoringOptions> optionsMonitor,
        IInstrumentationProvider instrumentationProvider,
        ITokenPriceService tokenPriceService)
    {
        _tokenIndexerProvider = tokenIndexerProvider;
        _distributedCache = distributedCache;
        _logger = logger;
        _options = options.Value;
        _globalOptions = globalOptions;
        _optionsMonitor = optionsMonitor;
        _tokenPriceService = tokenPriceService;
        
        // Initialize address sets for fast lookup
        _blacklistAddresses = new HashSet<string>(_options.BlacklistAddresses, StringComparer.OrdinalIgnoreCase);
        _toOnlyMonitoredAddresses = new HashSet<string>(_options.ToOnlyMonitoredAddresses, StringComparer.OrdinalIgnoreCase);
        _largeAmountOnlyAddresses = new HashSet<string>(_options.LargeAmountOnlyAddresses, StringComparer.OrdinalIgnoreCase);
        // Initialize histogram with configured buckets
        _transferUSDEventsHistogram = instrumentationProvider.Meter.CreateHistogram<double>(
            "aelf_transfer_usd_value",
            "ms",
            "Token transfer events with amount distribution");
            
        // Initialize counter for transfer counts
        _transferCountsCounter = instrumentationProvider.Meter.CreateCounter<long>(
            "aelf_transfer_counts",
            "counts",
            "Token transfer counts by various dimensions");
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

        var filteredList = result.List
            .Where(item => _options.MonitoredTokens.Contains(item.Symbol))
            .ToList();

        if (!filteredList.Any())
        {
            return (new List<TransferEventDto>(), 0, false, null);
        }

        // Get unique symbols for price lookup
        var uniqueSymbols = filteredList.Select(x => x.Symbol).Distinct().ToList();
        var priceDict = new Dictionary<string, decimal>();
        
        // Fetch prices for all unique symbols
        foreach (var symbol in uniqueSymbols)
        {
            try
            {
                var priceDto = await _tokenPriceService.GetTokenPriceAsync(symbol, CurrencyConstant.UsdCurrency);
                priceDict[symbol] = priceDto.Price;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get price for symbol {Symbol}, using 0", symbol);
                priceDict[symbol] = 0m;
            }
        }

        // Convert all transfers and calculate USD value
        var transfers = new List<TransferEventDto>();
        
        foreach (var item in filteredList)
        {
            var transfer = ConvertToTransferEventDto(item);
            
            // Calculate USD value
            if (priceDict.TryGetValue(item.Symbol, out var price))
            {
                transfer.UsdValue = Math.Round(transfer.Amount * price, CommonConstant.UsdValueDecimals);
            }
            
            // Reclassify addresses with USD value context
            transfer.FromAddressType = ClassifyAddress(transfer.FromAddress, false, transfer.UsdValue);
            transfer.ToAddressType = ClassifyAddress(transfer.ToAddress, true, transfer.UsdValue);
            
            // Add all transfers (filtering will be done in SendTransferMetrics)
            transfers.Add(transfer);
        }

        var maxHeight = result.List.Max(x => x.BlockHeight);
        
        // Track the latest block time from the data
        var latestBlockTime = result.List.Max(x => x.DateTime);
        
        // Return all transfers for processing
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
            // Filter system contract transfers if enabled
            var options = _optionsMonitor.CurrentValue;
            if (options.EnableSystemContractFilter && IsSystemContractTransfer(transfer.FromAddress))
            {
                _logger.LogInformation("Skipping system contract transfer from {FromAddress}", transfer.FromAddress);
                return;
            }

            SendTransferMetrics(transfer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transfer: {TransferId}", transfer.TransactionId);
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
            // Determine if this is a high-value transfer once
            var isHighValue = transfer.UsdValue >= MinUsdValueThreshold;
            
            // Record outbound transaction (from perspective)
            var outboundTags = new KeyValuePair<string, object?>[]
            {
                new("chain_id", transfer.ChainId),
                new("symbol", transfer.Symbol),
                new("transfer_type", transfer.Type.ToString()),
                new("address", transfer.FromAddress),
                new("direction", "outbound"),
                new("address_type", transfer.FromAddressType.ToString()),
            };

            // Record inbound transaction (to perspective)
            var inboundTags = new KeyValuePair<string, object?>[]
            {
                new("chain_id", transfer.ChainId),
                new("symbol", transfer.Symbol),
                new("transfer_type", transfer.Type.ToString()),
                new("address", transfer.ToAddress),
                new("direction", "inbound"),
                new("address_type", transfer.ToAddressType.ToString()),
            };

            // Always record counter (for all transfers)
            _transferCountsCounter.Add(1, outboundTags);
            _transferCountsCounter.Add(1, inboundTags);
            
            // Only record histogram for high-value transfers
            if (isHighValue)
            {
                _transferUSDEventsHistogram.Record((double)transfer.UsdValue, outboundTags);
                _transferUSDEventsHistogram.Record((double)transfer.UsdValue, inboundTags);
                _logger.LogInformation("Sent transfer metrics for transaction {TransactionId}, amount {Amount} {Symbol}, USD value {UsdValue}", 
                    transfer.TransactionId, transfer.Amount, transfer.Symbol, transfer.UsdValue);
            }
            else
            {
                _logger.LogInformation("Sent counter metrics only for transaction {TransactionId}, amount {Amount} {Symbol}, USD value {UsdValue} below histogram threshold", 
                    transfer.TransactionId, transfer.Amount, transfer.Symbol, transfer.UsdValue);
            }
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
            FromAddressType = ClassifyAddress(dto.From?.Address ?? "", false, 0m),
            ToAddressType = ClassifyAddress(dto.To?.Address ?? "", true, 0m)
        };
    }

    private AddressClassification ClassifyAddress(string address, bool isToAddress = false, decimal usdValue = 0m)
    {
        if (string.IsNullOrEmpty(address))
            return AddressClassification.Normal;

        // Check blacklist first (highest priority)
        if (_blacklistAddresses.Contains(address))
            return AddressClassification.Blacklist;

        // Check ToOnlyMonitored addresses (only when it's a recipient address)
        if (isToAddress && _toOnlyMonitoredAddresses.Contains(address))
            return AddressClassification.ToOnlyMonitored;

        // Check LargeAmountOnly addresses (only for large transfers)
        if (_largeAmountOnlyAddresses.Contains(address) )
            return AddressClassification.LargeAmountOnly;

        return AddressClassification.Normal;
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