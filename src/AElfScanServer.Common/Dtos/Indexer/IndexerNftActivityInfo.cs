using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.Common.Dtos.Indexer;

public class NftActivityItem
{
    public string NftInfoId { get; set; }
    public NftActivityType Type { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public long Amount { get; set; }
    public TokenBaseInfoDto PriceTokenInfo { get; set; }
    public decimal Price { get; set; }
    public string TransactionHash { get; set; }
    public long  BlockHeight { get; set; }
    public DateTime Timestamp { get; set; }
}

public class IndexerNftActivityInfo
{
    public long TotalCount  { get; set; }
    public List<NftActivityItem> Items { get; set; } = new();
}

public class IndexerNftActivityInfos
{
    public IndexerNftActivityInfo NftActivityList { get; set; }
}

