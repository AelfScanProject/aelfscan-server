using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class TokenHolderInfoDto
{
    public CommonAddressDto Address { get; set; }
    public decimal Quantity { get; set; }
    public decimal Percentage { get; set; }
    public decimal Value { get; set; }
    public List<string> ChainIds { get; set; }
}

public class AccountCountInput
{
    public string ChainId { get; set; }
}

public class AccountCountResultDto
{
    public AccountCountDto AccountCount { get; set; }
}

public class AccountCountDto
{
    public int Count { get; set; }
}