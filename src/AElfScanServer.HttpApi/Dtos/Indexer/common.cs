using System;

namespace AElfScanServer.HttpApi.Dtos.Indexer;

public class MetadataDto
{
    public string ChainId { get; set; }

    public BlockMetadataDto Block { get; set; }
}

public class BlockMetadataDto
{
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public DateTime BlockTime { get; set; }
}