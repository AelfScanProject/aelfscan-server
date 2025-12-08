using System.Collections.Generic;

namespace AElfScanServer.Common.Options;

public class ChainOptions
{
    public Dictionary<string, string> NodeApis { get; set; } = new();
    public Dictionary<string, string> AccountPrivateKey { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> ContractAddress { get; set; } = new();
    
    public Dictionary<string, ChainInfo> ChainInfos { get; set; } = new();
    
    public class ChainInfo
    {
        public string TokenContractAddress { get; set; }
    }
    
    public ChainInfo GetChainInfo(string chainId)
    {
        return ChainInfos.TryGetValue(chainId, out var chainInfo) ? chainInfo : null;
    } 
}