namespace AElfScanServer.Common.Dtos.Input;

public class GetTransferInfoListInput : BaseInput
{
    public string Address { get; set; }
    public bool IsNft { get; set; }
}