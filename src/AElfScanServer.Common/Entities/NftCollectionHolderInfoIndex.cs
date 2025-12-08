using AElf.EntityMapping.Entities;
using Nest;

namespace AElfScanServer.Domain.Common.Entities;

public class NftCollectionHolderInfoIndex: AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword] public string Address { get; set; }
    [Keyword] public string CollectionSymbol { get; set; }
    public long Quantity { get; set; }
    public decimal FormatQuantity { get; set; }
    
    public string ChainId { get; set; }
}