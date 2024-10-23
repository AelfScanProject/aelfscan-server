using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos;

public class TokenDetailDto : TokenCommonDto
{
    public decimal Price { get; set; }

    public double PricePercentChange24h { get; set; }

    public CommonAddressDto ContractAddressDto { get; set; }
    public string TokenContractAddress { get; set; }



    public List<string> AddressTypeList { get; set; }
}