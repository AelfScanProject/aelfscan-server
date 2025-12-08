
namespace AElfScanServer.Grains.State.Contract;


public class SynchronizationContractState
{
    public string ChainId { get; set; }
    
    public string BizType { get; set; }
    
    public long LastBlockHeight { get; set; }
   
}