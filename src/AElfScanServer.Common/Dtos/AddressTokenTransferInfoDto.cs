using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class AddressTokenTransferInfoDto
{
    public TransferStatusType TransferStatus { get; set; }
    public decimal Amount { get; set; }
    public TokenBaseInfo Asset { set; get; }
    public SymbolType Type { set; get; }

    public string Symbol { set; get; }
    public string SymbolName { set; get; }

    public string TransactionHash { get; set; }
    public string Method { get; set; }
    public string BlockHeight { get; set; }
    public long Timestamp { get; set; }
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
}

public class TokenTransferInfoListDto : ListResponseDto<AddressTokenTransferInfoDto>
{
    public double AssetInUsd { set; get; }
}