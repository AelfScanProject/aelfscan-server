using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos.Indexer;

public class IndexerTokenInfoDto : IndexerTokenBaseDto
{
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public long Supply { get; set; }
    public long Issued { get; set; }
    public string Issuer { get; set; }
    public string Owner { get; set; }
    public bool IsPrimaryToken { get; set; }
    public bool IsBurnable { get; set; }
    public string IssueChainId { get; set; }
    public int Decimals { get; set; }

    public SymbolType Type { get; set; }
    public List<ExternalInfoDto> ExternalInfo { get; set; } = new();
    public long HolderCount { get; set; }
    public long TransferCount { get; set; }

    public decimal ItemCount { get; set; }

    public MetadataDto Metadata { get; set; }
}

public class IndexerTokenBaseDto
{
    public string Symbol { get; set; }
    public string CollectionSymbol { get; set; }
    public SymbolType Type { get; set; }
    public int Decimals { get; set; }
}

public class IndexerTokenInfosDto
{
    public IndexerTokenInfoListDto TokenInfo { get; set; }
}

public class IndexerTokenInfoListDto
{
    public long TotalCount { get; set; }
    public List<IndexerTokenInfoDto> Items { get; set; } = new();
}