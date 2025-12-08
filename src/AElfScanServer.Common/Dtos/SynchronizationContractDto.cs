using Orleans;

namespace AElfScanServer.Common.Dtos;
[GenerateSerializer]
public class SynchronizationContractDto
{
    [Id(0)] public string ChainId { get; set; }
    
    [Id(1)] public string BizType { get; set; }
    
    [Id(2)]public long LastBlockHeight { get; set; }
}