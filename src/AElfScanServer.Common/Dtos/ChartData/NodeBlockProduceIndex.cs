using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Domain.Common.Entities;
using Nest;

namespace AElfScanServer.Common.Dtos.ChartData;

public class NodeBlockProduceIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return NodeAddress + "_" + RoundNumber + "_" + ChainId; }
    }

    public long RoundNumber { get; set; }

    public long DurationSeconds { get; set; }

    [Keyword] public string ChainId { get; set; }
    public long Blcoks { get; set; }

    public long MissedBlocks { get; set; }


    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public bool IsExtraBlockProducer { get; set; }

    [Keyword] public string NodeAddress { get; set; }
}

public class RoundIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return RoundNumber + "_" + ChainId; }
    }

    public long RoundNumber { get; set; }
    [Keyword] public string ChainId { get; set; }

    public long DurationSeconds { get; set; }

    [Keyword] public string DateStr { get; set; }

    public int ProduceBlockBpCount { get; set; }

    public int NotProduceBlockBpCount { get; set; }

    public List<string> ProduceBlockBpAddresses { get; set; }

    public List<string> NotProduceBlockBpAddresses { get; set; }

    public long Blcoks { get; set; }

    public long MissedBlocks { get; set; }

    public long StartTime { get; set; }

    public long EndTime { get; set; }
}

public class HourNodeBlockProduceIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Date + "_" + ChainId + "_" + NodeAddress; }
    }

    public long Date { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string DateStr { get; set; }
    public long TotalCycle { get; set; }

    public int Hour { get; set; }

    public long Blocks { get; set; }

    public long MissedBlocks { get; set; }


    [Keyword] public string NodeAddress { get; set; }
}

public class DailyBlockProduceCountIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Date + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }


    [Keyword] public string BlockProductionRate { get; set; }

    public long BlockCount { get; set; }

    public long MissedBlockCount { get; set; }
}

public class DailyBlockProduceDurationIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Date + "_" + ChainId; }
    }

    [Keyword] public string ChainId { get; set; }

    public long Date { get; set; }
    [Keyword] public string AvgBlockDuration { get; set; }
    [Keyword] public string LongestBlockDuration { get; set; }
    [Keyword] public string ShortestBlockDuration { get; set; }
}

public class DailyCycleCountIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Date + "_" + ChainId; }
    }

    [Keyword] public string ChainId { get; set; }
    public long Date { get; set; }
    public long CycleCount { get; set; }
    public long MissedBlockCount { get; set; }
    public long MissedCycle { get; set; }
}

public class DailyTransactionsChartSet
{
    public DailyAvgTransactionFeeIndex DailyAvgTransactionFeeIndex { get; set; }
    public DailyBlockRewardIndex DailyBlockRewardIndex { get; set; }
    public DailyTotalBurntIndex DailyTotalBurntIndex { get; set; }
    public DailyDeployContractIndex DailyDeployContractIndex { get; set; }
    public DailyTransactionCountIndex DailyTransactionCountIndex { get; set; }
    public DailyUniqueAddressCountIndex DailyUniqueAddressCountIndex { get; set; }
    public DailyActiveAddressCountIndex DailyActiveAddressCountIndex { get; set; }
    public DailyHasFeeTransactionIndex DailyHasFeeTransactionIndex { get; set; }
    public DailySupplyGrowthIndex DailySupplyGrowthIndex { get; set; }

    public Dictionary<string, MonthlyActiveAddressInfoIndex> MonthlyAddressDic { get; set; } = new();

    public double DailyUnReceived { get; set; }
    public List<string> TransactionFeeRecords { get; set; } = new();
    public DailyTVLIndex DailyTVLIndex { get; set; }

    public string ChainId { get; set; }

    public Dictionary<string, DailyVotedIndex> DailyVotedIndexDic { get; set; }
    public List<DailyWithDrawnIndex> DailyWithDrawnList { get; set; } = new();
    public DailyStakedIndex DailyStakedIndex { get; set; }
    public Dictionary<string, DailyContractCallIndex> DailyContractCallIndexDic { get; set; }
    public DailyTotalContractCallIndex DailyTotalContractCallIndex { get; set; }

    public DailySupplyChange DailySupplyChange { get; set; }
    public Dictionary<string, HashSet<string>> CallersDic { get; set; } = new();
    public string DateStr { get; set; }

    public long DateTimeStamp { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime WirteFinishiTime { get; set; }

    public double CostTime { get; set; }

    public double TotalBpStaked { get; set; }
    public double TotalVotedStaked { get; set; }

    public List<string> VoteRecords { get; set; } = new();
    public List<string> WithDrawVotedIds { get; set; } = new();

    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }

    public HashSet<string> AddressSet { get; set; }
    public HashSet<string> AddressFromSet { get; set; }
    public HashSet<string> AddressToSet { get; set; }


    public double DailyBurnt { get; set; }
    public double DailyConsensusBalance { get; set; }
    public double TotalFee { get; set; }


    public double DailyOrganizationBalance { get; set; }


    public DailyTransactionsChartSet(string chainId, long totalMilliseconds, string date)
    {
        DailyAvgTransactionFeeIndex = new DailyAvgTransactionFeeIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };
        DailyBlockRewardIndex = new DailyBlockRewardIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyDeployContractIndex = new DailyDeployContractIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyTotalBurntIndex = new DailyTotalBurntIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyTransactionCountIndex = new DailyTransactionCountIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyUniqueAddressCountIndex = new DailyUniqueAddressCountIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyActiveAddressCountIndex = new DailyActiveAddressCountIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyContractCallIndexDic = new Dictionary<string, DailyContractCallIndex>();

        DailyTotalContractCallIndex = new DailyTotalContractCallIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyHasFeeTransactionIndex = new DailyHasFeeTransactionIndex()
        {
            ChainId = chainId,
            DateStr = date,
            TransactionIds = new List<string>()
        };


        DailySupplyGrowthIndex = new DailySupplyGrowthIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date,
        };

        DailyVotedIndexDic = new Dictionary<string, DailyVotedIndex>();

        DailyStakedIndex = new DailyStakedIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date,
        };

        DailySupplyChange = new DailySupplyChange()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        DailyTVLIndex = new DailyTVLIndex()
        {
            ChainId = chainId,
            Date = totalMilliseconds,
            DateStr = date
        };

        AddressSet = new HashSet<string>();

        AddressFromSet = new HashSet<string>();

        AddressToSet = new HashSet<string>();
    }
}

public class DailyAvgTransactionFeeIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }

    [Keyword] public string AvgFeeUsdt { get; set; }

    [Keyword] public string AvgFeeElf { get; set; }

    public int HasFeeTransactionCount { get; set; }
    public string TotalFeeElf { get; set; }
    public int TransactionCount { get; set; }

    [Keyword] public string DateStr { get; set; }
}

public class DailyHasFeeTransactionIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    [Keyword] public string DateStr { get; set; }

    [Keyword] public string ChainId { get; set; }

    public List<string> TransactionIds { get; set; }

    public int TransactionCount { get; set; }
}

public class DailyBlockRewardIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }

    [Keyword] public string BlockReward { get; set; }


    [Keyword] public string DateStr { get; set; }
    public long TotalBlockCount { get; set; }
}

public class DailyTotalBurntIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }

    [Keyword] public string Burnt { get; set; }

    public int HasBurntBlockCount { get; set; }
    [Keyword] public string DateStr { get; set; }
}

public class DailyDeployContractIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }

    public int Count { get; set; }


    [Keyword] public string DateStr { get; set; }
}

public class ElfPriceIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr; }
    }

    public long OpenTime { get; set; }
    [Keyword] public string DateStr { get; set; }
    [Keyword] public string Open { get; set; }
    [Keyword] public string High { get; set; }
    [Keyword] public string Low { get; set; }

    [Keyword] public string Close { get; set; }
    // public string Volume { get; set; }
    // public long CloseTime { get; set; }
    // public string QuoteAssetVolume { get; set; }
    // public int NumberOfTrades { get; set; }
    // public string TakerBuyBaseAssetVolume { get; set; }
    // public string TakerBuyQuoteAssetVolume { get; set; }
    // public string Ignore { get; set; }
}

public class DailyTransactionCountIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }
    public int TransactionCount { get; set; }
    public int BlockCount { get; set; }


    [Keyword] public string DateStr { get; set; }
}

public class DailyUniqueAddressCountIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }
    public int AddressCount { get; set; }

    public int TotalUniqueAddressees { get; set; }


    [Keyword] public string DateStr { get; set; }
}

public class DailyMergeUniqueAddressCountIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }
    public int AddressCount { get; set; }

    public int TotalUniqueAddressees { get; set; }


    [Keyword] public string DateStr { get; set; }
}

public class DailyActiveAddressCountIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }
    public long AddressCount { get; set; }

    public long SendAddressCount { get; set; }
    public long ReceiveAddressCount { get; set; }

    [Keyword] public string DateStr { get; set; }
}

public class AddressIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Address + "_" + ChainId; }
    }

    [Keyword] public string Date { get; set; }

    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
}

public class MergeAddressIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Address + "_" + ChainId; }
    }

    public long Date { get; set; }

    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
}

public class DailyAvgBlockSizeIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string DateStr { get; set; }

    [Keyword] public string AvgBlockSize { get; set; }

    public long TotalSize { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }

    public int BlockCount { get; set; }
}

public class BlockSizeErrInfoIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return BlockHeight + "_" + ChainId; }
    }

    public DateTime Date { get; set; }
    [Keyword] public string ChainId { get; set; }

    [Keyword] public string ErrMsg { get; set; }

    public long BlockHeight { get; set; }
}

public class DailyTotalContractCallIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }
    [Keyword] public string DateStr { get; set; }

    public long CallCount { get; set; }

    public long CallAddressCount { get; set; }

    [Keyword] public string ChainId { get; set; }
}

public class DailyContractCallIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId + "_" + ContractAddress; }
    }

    public long Date { get; set; }
    [Keyword] public string DateStr { get; set; }
    public long CallCount { get; set; }

    public HashSet<string> CallerSet { get; set; }
    public long CallAddressCount { get; set; }

    [Keyword] public string ContractAddress { get; set; }
    [Keyword] public string ChainId { get; set; }
}

public class DailyTransactionRecordIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    [Keyword] public string ChainId { get; set; }
    [Keyword] public string DateStr { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime DataWriteFinishTime { get; set; }
    public double WriteCostTime { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
}

public class DailySupplyGrowthIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }
    [Keyword] public string DateStr { get; set; }


    public double DailyConsensusBalance { get; set; }
    public double TotalConsensusBalance { get; set; }
    public double DailyBurnt { get; set; }
    public double TotalBurnt { get; set; }
    public double DailyOrganizationBalance { get; set; }

    public double DailyUnReceived { get; set; }


    public double TotalUnReceived { get; set; }
    public double TotalOrganizationBalance { get; set; }
    [Keyword] public string ChainId { get; set; }
}

public class BlockIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return BlockHeight + "_" + ChainId; }
    }

    public long BlockHeight { get; set; }
    public long Timestamp { get; set; }
    [Keyword] public string ChainId { get; set; }
    public int TransactionCount { get; set; }
    [Keyword] public string TimeSpan { get; set; }
    [Keyword] public string Reward { get; set; }
    [Keyword] public string BurntFees { get; set; }
    [Keyword] public string ProducerName { get; set; }
    [Keyword] public string ProducerAddress { get; set; }

    public List<string> ChainIds { get; set; } = new();
}

public class DailyStakedIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }
    [Keyword] public string DateStr { get; set; }

    [Keyword] public string BpStaked { get; set; }

    [Keyword] public string VoteStaked { get; set; }


    [Keyword] public string ChainId { get; set; }
}

public class DailySupplyChange : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    public long Date { get; set; }
    [Keyword] public string DateStr { get; set; }

    public List<string> SupplyChange { get; set; } = new();

    public long TotalSupply { get; set; }
    [Keyword] public string ChainId { get; set; }
}

public class DailyVotedIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId + "_" + VoteId; }
    }

    public long Date { get; set; }
    [Keyword] public string DateStr { get; set; }

    [Keyword] public string VoteId { get; set; }

    public double VoteAmount { get; set; }

    [Keyword] public string TransactionId { get; set; }

    [Keyword] public string ChainId { get; set; }
}

public class MonthlyActiveAddressInfoIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateMonth + "_" + Address + "_" + ChainId + "_" + Type; }
    }

    public long Date { get; set; }
    public int DateMonth { get; set; }


    [Keyword] public string Address { get; set; }

    [Keyword] public string Type { get; set; }
    [Keyword] public string ChainId { get; set; }
}

public class MonthlyActiveAddressIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateMonth + "_" + ChainId; }
    }


    [Keyword] public string ChainId { get; set; }
    public long AddressCount { get; set; }
    
    public long SendAddressCount { get; set; }
    public long ReceiveAddressCount { get; set; }

    public int DateMonth { get; set; }
}

public class DailyWithDrawnIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId + "_" + VoteId; }
    }

    public long Date { get; set; }
    [Keyword] public string DateStr { get; set; }

    [Keyword] public string VoteId { get; set; }

    public double VoteAmount { get; set; }

    [Keyword] public string TransactionId { get; set; }

    [Keyword] public string ChainId { get; set; }
}

public class TransactionErrInfoIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return "_" + ChainId; }
    }

    public DateTime HappenTime { get; set; }
    [Keyword] public string ChainId { get; set; }

    [Keyword] public string ErrMsg { get; set; }

    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
}

public class DailyTVLIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return DateStr + "_" + ChainId; }
    }

    [Keyword] public string DateStr { get; set; }
    public double TVL { get; set; }

    public double DailyPrice { get; set; }
    public long Date { get; set; }
    public double BPLockedAmount { get; set; }

    public double VoteLockedAmount { get; set; }

    public double AwakenLocked { get; set; }


    [Keyword] public string ChainId { get; set; }
}

public class FixDailyData
{
    public Dictionary<string, List<string>> FixDate { get; set; }
}