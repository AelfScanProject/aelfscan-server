namespace AElfScanServer.Common.Dtos.ChartData;

public class DailyTransactionCount
{
    public long Date { get; set; }

    public int TransactionCount { get; set; }

    public int MergeTransactionCount { get; set; }

    public int MainChainTransactionCount { get; set; }

    public int SideChainTransactionCount { get; set; }


    public int BlockCount { get; set; }

    public string DateStr { get; set; }
}

public class DailyUniqueAddressCount
{
    public long Date { get; set; }
    public int AddressCount { get; set; }

    public int MergeAddressCount { get; set; }

    public int MainChainAddressCount { get; set; }

    public int SideChainAddressCount { get; set; }
    public int TotalUniqueAddressees { get; set; }

    public int MergeTotalUniqueAddressees { get; set; }

    public int MainChainTotalUniqueAddressees { get; set; }

    public int SideChainTotalUniqueAddressees { get; set; }

    public int OwnerUniqueAddressees { get; set; }


    public string DateStr { get; set; }
}

public class DailyActiveAddressCount
{
    public long Date { get; set; }
    public long AddressCount { get; set; }

    public long MergeAddressCount { get; set; }

    public long MainChainAddressCount { get; set; }

    public long SideChainAddressCount { get; set; }

    public long SendAddressCount { get; set; }
    public long MergeSendAddressCount { get; set; }
    public long MainChainSendAddressCount { get; set; }
    public long SideChainSendAddressCount { get; set; }
    public long ReceiveAddressCount { get; set; }
    public long MainChainReceiveAddressCount { get; set; }
    public long SideChainReceiveAddressCount { get; set; }
    public long MergeReceiveAddressCount { get; set; }

    public string DateStr { get; set; }
}

public class DailyBlockProduceCount
{
    public long Date { get; set; }
    public string BlockProductionRate { get; set; }
    public long BlockCount { get; set; }
    public long MissedBlockCount { get; set; }

    public string DateStr { get; set; }
}

public class DailyBlockProduceDuration
{
    public long Date { get; set; }
    public string AvgBlockDuration { get; set; }
    public string LongestBlockDuration { get; set; }
    public string ShortestBlockDuration { get; set; }

    public string DateStr { get; set; }
}

public class DailyCycleCount
{
    public long Date { get; set; }
    public long CycleCount { get; set; }
    public long MissedBlockCount { get; set; }
    public long MissedCycle { get; set; }

    public string DateStr { get; set; }
}