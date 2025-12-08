using System.Collections.Generic;
using System.Linq;

namespace AElfScanServer.Common.Options;

public class ApiClientOption
{
    public List<ApiServer> ApiServers { get; set; } = new();
    
    public ApiServer GetApiServer(string serverName)
    {
        return ApiServers.FirstOrDefault(s => s.ServerName == serverName);
    }
}

public class ApiServer
{
    public string ServerName { get; set; }
    public string Domain { get; set; }
}

public class TokenServerOption
{
    public string Url { get; set; }
}

public class BlockChainOption
{
    public string Url { get; set; }
}