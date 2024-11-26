using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Dtos;

public class IndexerAccountTokenDto
{
    public IndexerAccountTokenListDto AccountToken { get; set; }
}

public class IndexerAccountTokenListDto
{
    public long TotalCount { get; set; }
    public List<AccountTokenDto> Items { get; set; } = new();
}