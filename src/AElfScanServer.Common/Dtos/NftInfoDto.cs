using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class NftInfoDto
{
    public TokenBaseInfo NftCollection { get; set; }
    public string Items { get; set; }
    public string MergeItems { get; set; }
    public long Holders { get; set; }
    public long MergeHolders { get; set; }
    public long TransferCount { get; set; }
    public long MergeTransferCount { get; set; }

    public string MainChainItems { get; set; }
    public long MainChainHolders { get; set; }
    public long MainChainTransferCount { get; set; }

    public string SideChainItems { get; set; }
    public long SideChainHolders { get; set; }
    public long SideChainTransferCount { get; set; }

    public List<string> ChainIds { get; set; } = new();
}