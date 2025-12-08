namespace AElfScanServer.Common.Dtos;

public class NftCollectionInfoDto
{
    public string ChainId { get; set; }

    public string Symbol { get; set; }

    public decimal FloorPrice { get; set; }

    public string FloorPriceSymbol { get; set; }
}