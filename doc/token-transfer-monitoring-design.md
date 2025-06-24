# Token Transfer Monitoring System Design

## Overview
This document outlines the design for a comprehensive token transfer monitoring system for AElfScan. The system monitors blockchain transfer events and sends metrics to Prometheus for alerting and analysis.

## Architecture

### Core Components
1. **TokenTransferMonitoringWorker** - Scheduled background worker
2. **TokenTransferMonitoringService** - Business logic and data processing
3. **OpenTelemetry Integration** - Metrics collection and transmission
4. **Prometheus** - Metrics storage and alerting

### Data Flow
```
Blockchain → AElfScan Indexer → TokenTransferMonitoringWorker → TokenTransferMonitoringService → OpenTelemetry → Prometheus → Alerting
```

## Prometheus Metrics Design

### Single Unified Metric
We use one comprehensive histogram metric that captures all transfer event dimensions:

```prometheus
# HELP aelf_transfer_events Token transfer events with amount and metadata
# TYPE aelf_transfer_events histogram
aelf_transfer_events{
    chain_id="AELF",
    symbol="ELF", 
    transfer_type="transfer",
    direction="out",
    address="2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz...",
    counterpart_address="2N6dJpBcS5TLm2Pj4GkMdj4MnLhbKu8FGDX3Mz...",
    address_type="normal",
    counterpart_address_type="blacklist"
}
```

### Metric Dimensions

| Label | Values | Description |
|-------|--------|-------------|
| `chain_id` | AELF, tDVV, tDVW | Blockchain identifier |
| `symbol` | ELF, USDT, BTC, ETH, etc. | Token symbol |
| `transfer_type` | transfer, burn, cross_chain_transfer, cross_chain_receive | Transfer operation type |
| `direction` | out, in | Transfer perspective (outbound/inbound) |
| `address` | Address string | Primary address for this record |
| `counterpart_address` | Address string | The other party in the transfer |
| `address_type` | normal, blacklist | Primary address classification |
| `counterpart_address_type` | normal, blacklist | Counterpart address classification |

### Bidirectional Recording
Each transfer A→B generates two metric records:
1. **Outbound perspective**: `direction="out"` where `address=A`, `counterpart_address=B`
2. **Inbound perspective**: `direction="in"` where `address=B`, `counterpart_address=A`

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

# Large amounts from blacklist addresses
increase(aelf_transfer_events_sum{from_address_type="Blacklist"}[1h])
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
      description: "Transfer of {{ $value }} ELF detected"

  - alert: MassiveTransferVolume
    expr: |
      sum by (address) (
        increase(aelf_transfer_events_sum{direction="out"}[1h])
      ) > 1000000
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "Massive transfer volume from {{ $labels.address }}"
```

### 2. High Frequency Alerts
```yaml
- name: high_frequency
  rules:
  - alert: HighFrequencyTrading
    expr: |
      sum by (address) (
        increase(aelf_transfer_events_count{direction="out"}[1h])
      ) > 100
    for: 10m
    labels:
      severity: warning
    annotations:
      summary: "High frequency trading detected from {{ $labels.address }}"

  - alert: TransferBurst
    expr: |
      sum by (address) (
        increase(aelf_transfer_events_count{direction="out"}[5m])
      ) > 20
    for: 0m
    labels:
      severity: critical
    annotations:
      summary: "Transfer burst detected from {{ $labels.address }}"
```

### 3. Blacklist Alerts
```yaml
- name: blacklist_monitoring
  rules:
  - alert: BlacklistActivity
    expr: |
      increase(aelf_transfer_events_count{
        address_type="blacklist" OR counterpart_address_type="blacklist"
      }[1m]) > 0
    for: 0m
    labels:
      severity: critical
    annotations:
      summary: "Blacklist address activity detected"
      description: "Transfer involving blacklist address: {{ $labels.address }}"
```

## Configuration Management

### Application Configuration
```json
{
  "TokenTransferMonitoring": {
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
    "HistogramBuckets": [10, 1000, 100000, "Infinity"],
    "EnableMonitoring": true
  }
}
```