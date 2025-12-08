using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class NftInventoryDto
{
    public TokenBaseInfo Item { get; set; } = new();
    public decimal LastSalePriceInUsd { get; set; }

    public decimal LastSalePrice { get; set; }
    public decimal LastSaleAmount { get; set; }

    public string LastSaleAmountSymbol { get; set; }

    public string LastTransactionId { get; set; }

    public List<string> ChainIds { get; set; } = new();
    public long BlockHeight { get; set; }
}

public class NftInventorysDto : ListResponseDto<NftInventoryDto>
{
    public bool IsAddress { get; set; }

    public List<HolderInfo> Items { get; set; }
}