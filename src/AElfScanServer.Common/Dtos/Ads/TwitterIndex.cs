using AElf.EntityMapping.Entities;
using AElfScanServer.Domain.Common.Entities;
using Nest;

namespace AElfScanServer.Common.Dtos.Ads;

public class TwitterIndex: AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Text { get; set; }
}