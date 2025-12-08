using System.Collections.Generic;

namespace AElfScanServer.HttpApi.Dtos.Indexer;

public class IndexerAddressTransactionCountListDto
{
    public List<IndexerAddressTransactionCountDto> Items;
}

public class IndexerAddressTransactionCountDto
{
    public string ChainId { get; set; }
    public long Count { get; set; }
    public string Address { get; set; }
}

public class IndexerAddressTransactionCountResultDto
{
    public IndexerAddressTransactionCountListDto AddressTransactionCount { get; set; }
}