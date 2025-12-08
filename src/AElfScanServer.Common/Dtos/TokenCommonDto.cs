using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class TokenCommonDto
{
    public TokenBaseInfo Token { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal CirculatingSupply { get; set; }
    public decimal MergeCirculatingSupply { get; set; }
    public decimal MainChainCirculatingSupply { get; set; }
    public decimal SideChainCirculatingSupply { get; set; }
    public List<string> ChainIds { get; set; } = new();
    public SymbolType Type { get; set; }
    public long Holders { get; set; }
    public long MergeHolders { get; set; }
    public long MainChainHolders { get; set; }
    public long SideChainHolders { get; set; }
    public double HolderPercentChange24H { get; set; }
    public double BeforeCount { get; set; }
    public long TransferCount { get; set; }
    public long MergeTransferCount { get; set; }
    public long MainChainTransferCount { get; set; }
    public long SideChainTransferCount { get; set; }
}