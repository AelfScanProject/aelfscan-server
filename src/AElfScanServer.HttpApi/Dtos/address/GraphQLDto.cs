using System;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GraphQLDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    
    public string CodeHash { get; set; }
    public DateTime BlockTime { get; set; }
}

