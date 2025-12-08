using System.Collections.Generic;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos.Input;

public class NftItemHolderInfoInput : BaseInput
{
    public string Symbol { get; set; } = "";
    
    public List<SymbolType> Types { get; set; } =  new() { SymbolType.Nft };
}