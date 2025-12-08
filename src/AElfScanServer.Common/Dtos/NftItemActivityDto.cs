using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.Common.Dtos;

public class NftItemActivityDto
{
    public string TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public string Action { get; set; }
    public DateTime BlockTime { get; set; }
    public long BlockHeight { get; set; }
    public decimal PriceOfUsd { get; set; }
    public decimal Price { get; set; }
    public string PriceSymbol { get; set; }
    public decimal Quantity { get; set; }
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }

    public List<string> ChainIds { get; set; } = new();
}