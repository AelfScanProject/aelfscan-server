namespace AElfScanServer.Common.Dtos;

public class NftDetailDto : NftInfoDto
{
    public decimal FloorPrice { get; set; }

    public decimal MainChainFloorPrice { get; set; }

    public decimal SideChainFloorPrice { get; set; }
    public decimal? FloorPriceOfUsd { get; set; }

    public decimal? MainChainFloorPriceOfUsd { get; set; }

    public decimal? SideChainFloorPriceOfUsd { get; set; }
    public string TokenContractAddress { get; set; }
}