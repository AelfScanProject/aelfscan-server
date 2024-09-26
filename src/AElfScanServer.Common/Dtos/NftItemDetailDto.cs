using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class NftItemDetailDto
{
    public TokenBaseInfo NftCollection { get; set; }
    public TokenBaseInfo Item { get; set; }
    public long Holders { get; set; }
    public List<string> Owner { get; set; }
    public List<string> Issuer { get; set; }
    public string TokenSymbol { get; set; }
    public decimal Quantity { get; set; }

    public MarketInfoDto MarketPlaces { get; set; }

    public bool IsSeed { get; set; }

    public string SymbolToCreate { get; set; }

    //seed has expire time
    public string ExpireTime { get; set; }

    public ListResponseDto<PropertyDto> Properties { get; set; }
    public string Description { get; set; }
    public List<string> ChainIds { get; set; } = new();
}

public class PropertyDto
{
    public string Title { get; set; }
    public string Value { get; set; }
}