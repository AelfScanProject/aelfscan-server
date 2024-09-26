using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class AddressNftInfoDto
{
    public TokenBaseInfo NftCollection { get; set; }
    public TokenBaseInfo Token { get; set; }
    public decimal Quantity { set; get; }
    public long TransferCount { get; set; }
    public DateTime? FirstNftTime { get; set; }

    public List<string> ChainIds { get; set; } = new();
}