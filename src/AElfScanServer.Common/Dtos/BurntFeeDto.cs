using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos;

public class BurntFeeListDto
{
    public List<BlockBurnFeeDto> Items { get; set; }
}

public class BlockBurnFeeResultDto
{
    public BurntFeeListDto BlockBurnFeeInfo { get; set; }
}

public class BlockBurnFeeDto 
{
    public string Symbol { get; set; }
    public long Amount { get; set; }

    public long BlockHeight { get; set; }
}