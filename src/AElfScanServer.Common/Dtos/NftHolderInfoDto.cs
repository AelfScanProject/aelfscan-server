using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class NftHolderInfoDto
{
    public CommonAddressDto Address { get; set; }
    public decimal Quantity { get; set; }
    public decimal Percentage { get; set; }
}