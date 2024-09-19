using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElfScanServer.Domain.Common.Entities;
using Nest;

namespace AElfScanServer.Common.Dtos.MergeData;

public class AccountTokenIndex :  AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    public TokenBase Token { get; set; }
    public long Amount { get; set; }
    public decimal FormatAmount { get; set; }
    public long TransferCount { get; set; }
    public List<string> ChainIds { get; set; } = new();
}