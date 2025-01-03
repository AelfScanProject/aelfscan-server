using System.Collections.Generic;
using AElfScanServer.Common.Dtos.ChartData;

namespace AElfScanServer.HttpApi.Dtos.ChartData;

public class UniqueAddressCountResp
{
    public long Total { get; set; }
    public List<DailyUniqueAddressCount> List { get; set; }
    public DailyUniqueAddressCount HighestIncrease { get; set; }
    public DailyUniqueAddressCount LowestIncrease { get; set; }
}

public class ActiveAddressCountResp
{
    public long Total { get; set; }
    public List<DailyActiveAddressCount> List { get; set; }
    public DailyActiveAddressCount HighestActiveCount { get; set; }
    public DailyActiveAddressCount LowestActiveCount { get; set; }
    public string ChainId { get; set; }
}

public class MonthlyActiveAddressCountResp
{
    public long Total { get; set; }
    public List<MonthlyActiveAddressCount> List { get; set; }
    public MonthlyActiveAddressCount HighestActiveCount { get; set; }
    public MonthlyActiveAddressCount LowestActiveCount { get; set; }
    public string ChainId { get; set; }
}

public class MonthlyActiveAddressCount
{
    public string ChainId { get; set; }
    public long AddressCount { get; set; }

    public long MergeAddressCount { get; set; }
    public long MainChainAddressCount { get; set; }
    public long SideChainAddressCount { get; set; }

    public long SendAddressCount { get; set; }
    public long MergeSendAddressCount { get; set; }
    public long MainChainSendAddressCount { get; set; }
    public long SideChainSendAddressCount { get; set; }
    public long ReceiveAddressCount { get; set; }
    public long MergeReceiveAddressCount { get; set; }
    public long MainChainReceiveAddressCount { get; set; }
    public long SideChainReceiveAddressCount { get; set; }
    public int DateMonth { get; set; }
}

public class BlockProduceRateResp
{
    public long Total { get; set; }
    public DailyMergeBlockProduceCount HighestBlockProductionRate { get; set; }
    public DailyMergeBlockProduceCount lowestBlockProductionRate { get; set; }
    public List<DailyMergeBlockProduceCount> List { get; set; }
}

public class AvgBlockDurationResp
{
    public long Total { get; set; }

    public List<DailyMergeBlockProduceDuration> List { get; set; }
}

public class CycleCountResp
{
    public long Total { get; set; }

    public DailyMergeCycleCount HighestMissedCycle { get; set; }
    public List<DailyMergeCycleCount> List { get; set; }
}

public class NodeBlockProduceResp
{
    public long Total { get; set; }
    public List<NodeBlockProduce> List { get; set; }
    public int TotalCycle { get; set; }
}

public class NodeBlockProduce
{
    public int Total { get; set; }

    public long TotalCycle { get; set; }

    public long DurationSeconds { get; set; }

    public long Blocks { get; set; }

    public long MissedBlocks { get; set; }

    public string BlocksRate { get; set; }


    public long InRound { get; set; }

    public string CycleRate { get; set; }

    public string NodeName { get; set; }

    public string NodeAddress { get; set; }
}