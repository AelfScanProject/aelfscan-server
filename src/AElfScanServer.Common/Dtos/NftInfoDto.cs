using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class NftInfoDto
{
    public TokenBaseInfo NftCollection { get; set; }
    public string Items { get; set; }
    public long Holders { get; set; }

    public long TransferCount { get; set; }

    public List<string> ChainIds { get; set; }
}