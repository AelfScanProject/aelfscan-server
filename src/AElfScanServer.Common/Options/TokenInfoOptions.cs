using System.Collections.Generic;
using System.Linq;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Options;

public class TokenInfoOptions
{
    public HashSet<string> NonResourceSymbols { get; set; } = new();

    public Dictionary<string, TokenInfoDto> TokenInfos { get; set; } = new();
    
    public List<MarketInfoDto> MarketInfos { get; set; }= new();
    
    public List<int> ActivityTypes { get; set; }= new();
    
    public MarketInfoDto GetMarketInfo(string marketName)
    {
        return MarketInfos.FirstOrDefault(item => item.MarketName.Equals(marketName));
    }
}

public class TokenInfoDto
{
    public string ImageUrl { get; set; }
}