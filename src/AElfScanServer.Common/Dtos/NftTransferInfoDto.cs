using System.Collections.Generic;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.Common.Dtos;

public class NftTransferInfoDto
{ 
    public string TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public string Method { get; set; }
    public long BlockHeight { get; set; }
    public long BlockTime { get; set; }
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
    public decimal Value { get; set; }
    public TokenBaseInfo Item { get; set; }
    public List<TransactionFeeDto> TransactionFeeList { get; set; }
}

public class NftTransferInfosDto : ListResponseDto<NftTransferInfoDto>
{
    public bool IsAddress { get; set; }
   
    public List<HolderInfo> Items { get; set; }
}
