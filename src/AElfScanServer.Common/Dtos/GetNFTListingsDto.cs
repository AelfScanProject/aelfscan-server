namespace AElfScanServer.Common.Dtos;

public class GetNFTListingsDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    
    public string CollectionSymbol { get; set; }

    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}