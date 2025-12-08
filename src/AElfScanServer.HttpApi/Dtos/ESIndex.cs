using System;
using System.Collections.Generic;
using System.Transactions;
using AElf.EntityMapping.Entities;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Domain.Common.Entities;
using Nest;

namespace AElfScanServer.HttpApi.Dtos;

public class AddressIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string Address { get; set; }

    [Keyword] public string LowerAddress { get; set; }

    [Keyword] public AddressType AddressType { get; set; }
    [Keyword] public string Name { get; set; }

    [Keyword] public string LowerName { get; set; }
    public bool IsProducer { get; set; }

    public bool IsManager { get; set; }

    [Keyword] public string ChainId { get; set; }
}



public class BlockExtraIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return BlockHeight + "_" + BlockHash; }
    }

    public long BlockHeight { get; set; }

    public string BlockHash { get; set; }

    public long BurntFee { get; set; }
}

public class LogEventIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return ContractAddress + "_" + BlockHeight + "_" + Index; }
    }

    public long BlockHeight { get; set; }


    [Keyword] public string ContractAddress { get; set; }

    public int Index { get; set; }


    [Keyword] public string TransactionId { get; set; }

    [Keyword] public string EventName { get; set; }

    [Keyword] public string MethodName { get; set; }
}