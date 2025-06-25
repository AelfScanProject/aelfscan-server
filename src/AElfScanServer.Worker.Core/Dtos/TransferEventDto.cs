using System;

namespace AElfScanServer.Worker.Core.Dtos;

public class TransferEventDto
{
    public string ChainId { get; set; }
    public string TransactionId { get; set; }
    public long BlockHeight { get; set; }
    public DateTime Timestamp { get; set; }
    public string Symbol { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public decimal Amount { get; set; }
    public decimal UsdValue { get; set; }
    public TransferType Type { get; set; }
    public AddressClassification FromAddressType { get; set; }
    public AddressClassification ToAddressType { get; set; }
}

public enum TransferType
{
    Transfer,
    Burn,
    CrossChainTransfer,
    CrossChainReceive
}

public enum AddressClassification
{
    Normal,
    Blacklist,
    ToOnlyMonitored,
    LargeAmountOnly
}

/// <summary>
/// Bidirectional transfer perspective data
/// </summary>
public class TransferDirectionDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public TransferType TransferType { get; set; }
    public string Direction { get; set; } // "out" or "in"
    public string Address { get; set; }
    public string CounterpartAddress { get; set; }
    public AddressClassification AddressType { get; set; }
    public AddressClassification CounterpartAddressType { get; set; }
    public string TransactionId { get; set; }
    public string BlockHeight { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// Transfer event data transfer object for monitoring purposes
/// </summary> 