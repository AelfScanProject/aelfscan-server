using System;
using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos;

public class IndexerNftListingInfo
{
    public string Id { get; set; }
    public long Quantity { get; set; }
    public string Symbol { get; set; }
    public string Owner { get; set; }
    public string ChainId { get; set; }
    public decimal Prices { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime PublicTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public IndexerTokenInfo PurchaseToken { get; set; }
    public long RealQuantity { get; set; }
}

public class IndexerTokenInfo
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
}

public class IndexerNftListingInfoDto
{
    public long TotalCount  { get; set; }
    public List<IndexerNftListingInfo> Items { get; set; } = new();
}

public class IndexerNftListingInfos
{
    public IndexerNftListingInfoDto NftListingInfo { get; set; }
}
