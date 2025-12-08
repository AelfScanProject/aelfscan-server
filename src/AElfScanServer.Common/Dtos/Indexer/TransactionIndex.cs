using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElfScanServer.Common.Enums;
using AElfScanServer.Domain.Common.Entities;
using Nest;

namespace AElfScanServer.Common.Dtos.Indexer;

public class TransactionIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return TransactionId + "_" + ChainId; }
    }

    [Keyword] public string TransactionId { get; set; }

    [Keyword] public string ChainId { get; set; }

    [Keyword] public string From { get; set; }

    [Keyword] public string To { get; set; }

    public long BlockHeight { get; set; }

    public string Signature { get; set; }
    public bool Confirmed { get; set; }
    public DateTime BlockTime { get; set; }

    public TransactionStatus Status { get; set; }

    [Keyword] public string MethodName { get; set; }


    [Keyword] public string DateStr { get; set; }


    public Dictionary<string, string> ExtraProperties { get; set; }

    public List<IndexerLogEventDto> LogEvents { get; set; }
}

public class TransactionData
{
    public string TransactionId { get; set; }

    public string From { get; set; }

    public string To { get; set; }

    public long BlockHeight { get; set; }

    public DateTime BlockTime { get; set; }


    public string MethodName { get; set; }
    public string DateStr { get; set; }

    public Dictionary<string, string> ExtraProperties { get; set; }

    public List<IndexerLogEventDto> LogEvents { get; set; }
}

public class IndexerLogEventDto
{
    [Keyword] public string ChainId { get; set; }
    // public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword] public string TransactionId { get; set; }

    public DateTime BlockTime { get; set; }

    [Keyword] public string ContractAddress { get; set; }

    [Keyword] public string EventName { get; set; }

    public int Index { get; set; }

    public Dictionary<string, string> ExtraProperties { get; set; }
}