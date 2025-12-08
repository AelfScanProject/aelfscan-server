namespace AElfScanServer.HttpApi.Dtos.address;

public class TokenBaseDto : GraphQLDto
{
    public string Symbol { get; set; }
    public string CollectionSymbol { get; set; }
    public string Type { get; set; }
    public int Decimals { get; set; }
}

// public enum SymbolType
// {
//     Token,
//     Nft,
//     Nft_Collection
// }