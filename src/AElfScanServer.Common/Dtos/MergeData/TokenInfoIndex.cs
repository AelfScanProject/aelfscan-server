using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Domain.Common.Entities;
using AElfScanServer.Domain.Shared.Common;
using Nest;

namespace AElfScanServer.Common.Dtos.MergeData;

public class TokenInfoIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Symbol; }
    }

    [Keyword] public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public long Supply { get; set; }
    public long Issued { get; set; }
    [Keyword] public string Issuer { get; set; }
    [Keyword] public string Owner { get; set; }
    public bool IsPrimaryToken { get; set; }
    public bool IsBurnable { get; set; }
    [Keyword] public string IssueChainId { get; set; }
    public List<ExternalInfoDto> ExternalInfo { get; set; } = new();
    public long HolderCount { get; set; }
    public long TransferCount { get; set; }

    [Keyword] public string ChainId { get; set; }
    public decimal ItemCount { get; set; }

    public MetadataDto Metadata { get; set; }
    public List<string> ChainIds { get; set; } = new();

    [Keyword] public string Symbol { get; set; }
    [Keyword] public string CollectionSymbol { get; set; }
    public SymbolType Type { get; set; }
    public int Decimals { get; set; }
}