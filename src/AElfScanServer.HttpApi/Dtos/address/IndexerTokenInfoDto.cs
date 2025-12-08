namespace AElfScanServer.HttpApi.Dtos.address;

public class TokenDto
{
    public string Symbol { get; set; }
    public string CollectionSymbol { get; set; }
    public int Type { get; set; }
    public int Decimals { get; set; }
}