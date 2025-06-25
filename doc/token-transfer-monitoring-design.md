# Token Transfer Monitoring System Design

## Overview
This document outlines the design for a comprehensive token transfer monitoring system for AElfScan. The system monitors blockchain transfer events using time-based incremental scanning and sends metrics to Prometheus for alerting and analysis.

## Architecture

### Core Components
1. **TokenTransferMonitoringWorker** - Scheduled background worker with startup delay
2. **TokenTransferMonitoringService** - Business logic and time-based data processing
3. **OpenTelemetry Integration** - Metrics collection and transmission
4. **Prometheus** - Metrics storage and alerting

### Data Flow
```
Blockchain → AElfScan Indexer → TokenTransferMonitoringWorker → TokenTransferMonitoringService → OpenTelemetry → Prometheus → Alerting
```

### Key Features
- **Time-based incremental scanning** (not block height based)
- **System contract filtering** using existing GlobalOptions.ContractNames
- **Simplified address classification** (Normal, Blacklist only)
- **30-second startup delay** to avoid system startup overload
- **UTC time handling** with Redis-based scan time tracking
- **Single simplified metric** with essential dimensions

## Prometheus Metrics Design

### Single Unified Metric
We use one simplified histogram metric that captures essential transfer event dimensions:

```prometheus
# HELP aelf_transfer_events Token transfer events with amount and metadata
# TYPE aelf_transfer_events histogram
aelf_transfer_events{
    chain_id="AELF",
    symbol="ELF", 
    transfer_type="Transfer",
    from_address="2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz...",
    to_address="2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz...",
    from_address_type="Normal",
    to_address_type="Blacklist",
    transaction_id="abc123..."
}
```

### Metric Dimensions

| Label | Values | Description |
|-------|--------|-------------|
| `chain_id` | AELF, tDVV, tDVW | Blockchain identifier |
| `symbol` | ELF, USDT, BTC, ETH, etc. | Token symbol |
| `transfer_type` | Transfer, Burn, CrossChainTransfer, CrossChainReceive | Transfer operation type |
| `from_address` | Address string | Source address of the transfer |
| `to_address` | Address string | Destination address of the transfer |
| `from_address_type` | Normal, Blacklist | Source address classification |
| `to_address_type` | Normal, Blacklist | Destination address classification |
| `transaction_id` | Transaction hash | Unique transaction identifier for tracking |

### Histogram Buckets
Amount distribution tracking with 4 buckets for clear categorization:
- **10**: Micro transfers (≤10)
- **1000**: Small transfers (10-1000)  
- **100000**: Large transfers (1000-100000)
- **Infinity**: Massive transfers (>100000)

## PromQL Query Examples

### 1. Large Amount Transfers
```promql
# Transfers over 100,000 ELF in the last hour
increase(aelf_transfer_events_sum{symbol="ELF"}[1h]) 
/ increase(aelf_transfer_events_count{symbol="ELF"}[1h]) > 100000

# Total large transfers by from address
sum by (from_address) (
  increase(aelf_transfer_events_sum{symbol="ELF"}[1h])
) > 500000
```

### 2. High-Frequency Trading
```promql
# Addresses with more than 100 transfers in the last hour
sum by (from_address) (
  increase(aelf_transfer_events_count[1h])
) > 100

# High-frequency between specific addresses
sum by (from_address, to_address) (
  increase(aelf_transfer_events_count[1h])
) > 50
```

### 3. Blacklist Address Monitoring
```promql
# All transfers from blacklist addresses
increase(aelf_transfer_events_count{from_address_type="Blacklist"}[1h])

# All transfers to blacklist addresses
increase(aelf_transfer_events_count{to_address_type="Blacklist"}[1h])

# Large amounts involving blacklist addresses
increase(aelf_transfer_events_sum{
  from_address_type="Blacklist" OR to_address_type="Blacklist"
}[1h])
```

### 4. Cross-Chain Activity
```promql
# Cross-chain transfer volume
sum by (chain_id) (
  increase(aelf_transfer_events_sum{transfer_type="CrossChainTransfer"}[1h])
)

# Cross-chain transfer frequency
sum by (chain_id) (
  increase(aelf_transfer_events_count{transfer_type="CrossChainTransfer"}[1h])
)
```

### 5. Transaction Tracking
```promql
# Specific transaction monitoring
aelf_transfer_events_count{transaction_id="abc123..."}

# Transactions involving specific addresses
aelf_transfer_events_count{
  from_address="2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz..." OR
  to_address="2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz..."
}
```

## Alert Rules Configuration

### 1. Large Amount Alerts
```yaml
groups:
- name: large_transfers
  rules:
  - alert: LargeELFTransfer
    expr: |
      increase(aelf_transfer_events_sum{symbol="ELF"}[5m]) 
      / increase(aelf_transfer_events_count{symbol="ELF"}[5m]) > 100000
    for: 0m
    labels:
      severity: warning
    annotations:
      summary: "Large ELF transfer detected"
      description: "Transfer of {{ $value }} ELF detected from {{ $labels.from_address }}"

  - alert: MassiveTransferVolume
    expr: |
      sum by (from_address) (
        increase(aelf_transfer_events_sum[1h])
      ) > 1000000
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "Massive transfer volume from {{ $labels.from_address }}"
```

### 2. High Frequency Alerts
```yaml
- name: high_frequency
  rules:
  - alert: HighFrequencyTrading
    expr: |
      sum by (from_address) (
        increase(aelf_transfer_events_count[1h])
      ) > 100
    for: 10m
    labels:
      severity: warning
    annotations:
      summary: "High frequency trading detected from {{ $labels.from_address }}"

  - alert: TransferBurst
    expr: |
      sum by (from_address) (
        increase(aelf_transfer_events_count[5m])
      ) > 20
    for: 0m
    labels:
      severity: critical
    annotations:
      summary: "Transfer burst detected from {{ $labels.from_address }}"
```

### 3. Blacklist Alerts
```yaml
- name: blacklist_monitoring
  rules:
  - alert: BlacklistActivity
    expr: |
      increase(aelf_transfer_events_count{
        from_address_type="Blacklist" OR to_address_type="Blacklist"
      }[1m]) > 0
    for: 0m
    labels:
      severity: critical
    annotations:
      summary: "Blacklist address activity detected"
      description: "Transfer involving blacklist address: from={{ $labels.from_address }}, to={{ $labels.to_address }}"
```

## Configuration Management

### Application Configuration
```json
{
  "TokenTransferMonitoring": {
    "EnableMonitoring": true,
    "EnableSystemContractFilter": true,
    "BlacklistAddresses": [
      "2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz1",
      "2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz2"
    ],
    "MonitoredTokens": ["ELF", "USDT", "BTC", "ETH"],
    "ScanConfig": {
      "ChainIds": ["AELF", "tDVV", "tDVW"],
      "IntervalSeconds": 30,
      "BatchSize": 1000,
      "RedisKeyPrefix": "token_transfer_monitoring"
    },
    "HistogramBuckets": [10, 1000, 100000, "Infinity"]
  }
}
```

### System Contract Filtering
The system uses existing `GlobalOptions.ContractNames` configuration for system contract filtering:
- No additional configuration needed
- Leverages existing contract address mappings
- Can be disabled via `EnableSystemContractFilter: false`

## Implementation Details

### Time-Based Scanning
- **Incremental scanning** based on block time, not block height
- **Default scan window**: 60 minutes backward from current time
- **Redis state management**: Stores last scan time per chain
- **UTC time handling**: Ensures consistent time processing across systems

### Worker Startup Strategy
- **30-second startup delay** to avoid system startup overload
- **No immediate execution** (RunOnStart = false)
- **Graceful startup** with other system Workers

### Error Handling
- **Chain-level isolation**: Failure in one chain doesn't affect others
- **Comprehensive logging**: Detailed error tracking and performance metrics
- **Graceful degradation**: Continues operation even with partial failures

### Performance Optimizations
- **Batch processing**: Configurable batch sizes for efficient data processing
- **Safety limits**: 10,000 record limit to prevent memory issues
- **Incremental updates**: Only processes new data since last scan
- **Efficient Redis operations**: Minimal Redis calls with optimized key management

## Monitoring and Observability

### Logs
- Worker startup and configuration
- Scan progress and timing
- Transfer processing statistics
- Error conditions and recovery

### Metrics
- Transfer volume and frequency
- Processing performance
- System contract filtering effectiveness
- Blacklist address activity

### Health Checks
- Redis connectivity
- Indexer API availability  
- Metric transmission success
- Configuration validation