using AElf;
using AElfScanServer.Common.Commons;
using AElfScanServer.Common.Helper;
using NUglify.Helpers;

namespace AElfScanServer.Common.Dtos.Input;

public class NftInventoryInput : BaseInput
{
    public string Search { get; set; } = "";
    
    public string CollectionSymbol { get; set; }
    
 
        
    public bool IsSearchAddress()
    {
        return !Search.IsNullOrWhiteSpace() && CommonAddressHelper.IsAddress(Search);
    }
}