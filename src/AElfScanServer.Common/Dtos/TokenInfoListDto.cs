using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class TokenInfoListDto : ListResponseDto<TokenInfoDto>
{
    public double AssetInUsd { set; get; }
}

public class TokenInfoDto
{
    public TokenBaseInfo Token { set; get; }
    public decimal Quantity { set; get; }
    public decimal ValueOfUsd { set; get; }
    public decimal PriceOfUsd { set; get; }
    public double PriceOfUsdPercentChange24h { get; set; }
    public decimal PriceOfElf { set; get; }
    public decimal ValueOfElf { set; get; }
    public List<string> ChainIds { set; get; }
}