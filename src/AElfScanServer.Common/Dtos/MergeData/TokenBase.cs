using Nest;

namespace AElfScanServer.Common.Dtos.MergeData;

public class TokenBase 
{
    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    public SymbolType Type { get; set; }
    public int Decimals { get; set; }
}
