using System.Collections.Generic;

namespace AElfScanServer.Common.Options;

public class IndexerOptions
{
    public Dictionary<string, IndexerInfo> IndexerInfos { get; set; }
}

public class IndexerInfo
{
    public string BaseUrl { get; set; }
}