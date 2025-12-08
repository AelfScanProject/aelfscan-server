using System;
using AElf.EntityMapping.Entities;
using AElfScanServer.Domain.Common.Entities;
using Nest;

namespace AElfScanServer.Common.Dtos;

public class LogEventIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return TransactionId + "_" + ChainId + "_" + Index; }
    }

    [Keyword] public string ChainId { get; set; }

    public long BlockHeight { get; set; }

    [Keyword] public string TransactionId { get; set; }

    public DateTime BlockTime { get; set; }

    public long TimeStamp { get; set; }


    [Keyword] public string ToAddress { get; set; }
    [Keyword] public string ContractAddress { get; set; }

    [Keyword] public string MethodName { get; set; }

    [Keyword] public string EventName { get; set; }
    [Keyword] public string NonIndexed { get; set; }
    [Keyword] public string Indexed { get; set; }
    public int Index { get; set; }
}