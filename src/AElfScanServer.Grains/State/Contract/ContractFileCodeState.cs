using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Grains.State.Contract;

public class ContractFileCodeState
{
    public string ChainId { get; set; }

    public string Address { get; set; }
    public string ContractName { get; set; }
    public string ContractVersion { get; set; }

    public long LastBlockHeight { get; set; }
    public List<DecompilerContractFileDto> ContractSourceCode { get; set; }
    public bool IsVerify { get; set; }
}