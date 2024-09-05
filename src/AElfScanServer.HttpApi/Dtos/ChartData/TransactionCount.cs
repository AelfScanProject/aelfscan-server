using System;
using System.Collections.Generic;
using System.Linq;
using AElfScanServer.Common.Dtos.ChartData;
using Nest;

namespace AElfScanServer.HttpApi.Dtos.ChartData;

public class ChartDataRequest
{
    public long StartDate { get; set; } = 0;
    public long EndDate { get; set; } = 0;

    public int DateInterval { get; set; }

    public string ChainId { get; set; }
}

public class SetRoundRequest
{
    public string ChainId { get; set; }

    public string StartDate { get; set; }
    public string EndDate { get; set; }

    public bool InitRound { get; set; }
    public int SetNumber { get; set; }

    public bool UpdateData { get; set; }
}

public class SetJob
{
    public string ChainId { get; set; }

    public long SetBlockHeight { get; set; }

    public long SetSizBlockHeight { get; set; }


    public long SetLastRound { get; set; }
}

public class InitRoundResp
{
    public long MinRound { get; set; }

    public long MaxRound { get; set; }

    public long RoundCount { get; set; }

    public long InitRoundNumber { get; set; }

    public string InitRoundDate { get; set; }

    public string MinDate { get; set; }

    public List<string> UpdateDate { get; set; }


    public List<string> UpdateDateNodeBlockProduce { get; set; }

    public string MaxDate { get; set; }
    public string FinishDate { get; set; }
}

public class JonInfoResp
{
    public long RedisLastBlockHeight { get; set; }

    public long RedisLastRound { get; set; }
    public long EsLastRound { get; set; }
    public string EsLastRoundDate { get; set; }
    public long BlockSizeBlockHeight { get; set; }

    public string TransactionLastDate { get; set; }
    public int TransactionDateCount { get; set; }

    public HomeOverviewResponseDto Overview { get; set; }
}

public class DailyTransactionCountResp
{
    public long Total { get; set; }
    public List<DailyTransactionCount> List { get; set; }

    public DailyTransactionCount HighestTransactionCount { get; set; }
    public DailyTransactionCount LowesTransactionCount { get; set; }
}

public class DailyAvgTransactionFeeResp
{
    public long Total { get; set; }
    public List<DailyAvgTransactionFee> List { get; set; }

    public DailyAvgTransactionFee Highest { get; set; }
    public DailyAvgTransactionFee Lowest { get; set; }
}

public class DailyTransactionFeeResp
{
    public long Total { get; set; }
    public List<DailyTransactionFee> List { get; set; }

    public DailyTransactionFee Highest { get; set; }
    public DailyTransactionFee Lowest { get; set; }
}

public class DailyAvgTransactionFee
{
    public long Date { get; set; }

    public string AvgFeeUsdt { get; set; }

    public string AvgFeeElf { get; set; }

    public string TotalFeeElf { get; set; }
    public int TransactionCount { get; set; }

    public string DateStr { get; set; }
}

public class DailyTransactionFee
{
    public long Date { get; set; }

    public string TotalFeeElf { get; set; }

    public string DateStr { get; set; }
}

public class DailyBlockRewardResp
{
    public long Total { get; set; }
    public List<DailyBlockReward> List { get; set; }

    public DailyBlockReward Highest { get; set; }
    public DailyBlockReward Lowest { get; set; }
}

public class DailyAvgBlockSizeResp
{
    public long Total { get; set; }
    public List<DailyAvgBlockSize> List { get; set; }

    public DailyAvgBlockSize Highest { get; set; }
    public DailyAvgBlockSize Lowest { get; set; }
}

public class DailyAvgBlockSize
{
    public long Date { get; set; }
    public string DateStr { get; set; }
    public string AvgBlockSize { get; set; }
    public long TotalSize { get; set; }
    public int BlockCount { get; set; }
}

public class DailyBlockReward
{
    public long Date { get; set; }
    public string BlockReward { get; set; } = "0";
    public string DateStr { get; set; }
    public long TotalBlockCount { get; set; }
}

public class DailyTotalBurntResp
{
    public long Total { get; set; }
    public List<DailyTotalBurnt> List { get; set; }

    public DailyTotalBurnt Highest { get; set; }
    public DailyTotalBurnt Lowest { get; set; }
}

public class DailyTotalBurnt
{
    public long Date { get; set; }

    public string Burnt { get; set; }

    public int HasBurntBlockCount { get; set; }
    public string DateStr { get; set; }
}

public class DailyDeployContractResp
{
    public long Total { get; set; }
    public List<DailyDeployContract> List { get; set; }

    public DailyDeployContract Highest { get; set; }
    public DailyDeployContract Lowest { get; set; }
}

public class DailyDeployContract
{
    public long Date { get; set; }

    public string Count { get; set; }

    public string TotalCount { get; set; }

    public string DateStr { get; set; }
}

public class ElfPriceIndexResp
{
    public long Total { get; set; }
    public List<ElfPrice> List { get; set; }

    public ElfPrice Highest { get; set; }
    public ElfPrice Lowest { get; set; }
}

public class ElfPrice
{
    public long Date { get; set; }

    public string DateStr { get; set; }
    public string Open { get; set; }
    public string High { get; set; }
    public string Low { get; set; }
    public string Price { get; set; }
}

public class DailyTotalContractCallResp
{
    public long Total { get; set; }
    public List<DailyTotalContractCall> List { get; set; }

    public DailyTotalContractCall Highest { get; set; }
    public DailyTotalContractCall Lowest { get; set; }
}

public class TopContractCallResp
{
    public long Total { get; set; }
    public List<TopContractCall> List { get; set; }

    public TopContractCall Highest { get; set; }
    public TopContractCall Lowest { get; set; }
}

public class DailyTotalContractCall
{
    public long Date { get; set; }
    public string DateStr { get; set; }


    public long CallCount { get; set; }

    public long CallAddressCount { get; set; }
}

public class TopContractCall
{
    public long CallCount { get; set; }

    public long CallAddressCount { get; set; }

    public string CallRate { get; set; }
    public string ContractName { get; set; }

    public string ContractAddress { get; set; }
}

public class DailyMarketCapResp
{
    public long Total { get; set; }
    public List<DailyMarketCap> List { get; set; }

    public DailyMarketCap Highest { get; set; }
    public DailyMarketCap Lowest { get; set; }
}

public class DailySupplyGrowthResp
{
    public long Total { get; set; }
    public List<DailySupplyGrowth> List { get; set; }
}

public class DailyMarketCap
{
    public long Date { get; set; }
    public string DateStr { get; set; }

    public string TotalMarketCap { get; set; }

    public string FDV { get; set; }

    public string Price { get; set; }

    public string IncrMarketCap { get; set; }
}

public class DailySupplyGrowth
{
    public long Date { get; set; }
    public string DateStr { get; set; }

    public string TotalSupply
    {
        get
        {
            var totalSupply = 1000000000 - TotalOrganizationBalance - TotalConsensusBalance - TotalBurnt-TotalUnReceived;
            if (!SideChainBurnt.IsNullOrEmpty())
            {
                totalSupply -= decimal.Parse(SideChainBurnt);
            }

            return totalSupply.ToString("F4");
        }
    }

    public string MainChainBurnt
    {
        get { return DailyBurnt.ToString("F4"); }
    }

    public string SideChainBurnt { get; set; }

    public string OrganizationUnlock
    {
        get { return (-DailyOrganizationBalance).ToString("F4"); }
    }

    public string Reward
    {
        get { return (DailyConsensusBalance < 0 ? -DailyConsensusBalance : 0).ToString("F4"); }
    }

    public decimal DailyConsensusBalance { get; set; }
    public decimal TotalConsensusBalance { get; set; }
    public decimal DailyBurnt { get; set; }
    public decimal TotalBurnt { get; set; }

    public decimal TotalUnReceived { get; set; }
    public decimal DailyOrganizationBalance { get; set; }
    public decimal TotalOrganizationBalance { get; set; }
}

public class DailyStakedResp
{
    public long Total { get; set; }
    public List<DailyStaked> List { get; set; }
}

public class DailyHolderResp
{
    public long Total { get; set; }
    public List<DailyHolder> List { get; set; }
    public DailyHolder Highest { get; set; }
    public DailyHolder Lowest { get; set; }
}

public class DailyHolder
{
    public long Date { get; set; }
    public string DateStr { get; set; }

    public long Count { get; set; }
}

public class DailyStaked
{
    public long Date { get; set; }
    public string DateStr { get; set; }

    public string TotalStaked { get; set; }
    public string BpStaked { get; set; }

    public string VoteStaked { get; set; }

    public string Supply { get; set; }

    public string Rate { get; set; }
}

public class DailyTVLResp
{
    public long Total { get; set; }
    public List<DailyTVL> List { get; set; }

    public DailyTVL Highest { get; set; }
    public DailyTVL Lowest { get; set; }
}

public class DailyTVL
{
    public string DateStr { get; set; }
    public string TVL { get; set; }

    public long Date { get; set; }
    public string BPLocked { get; set; }

    public string VoteLocked { get; set; }

    public double TotalBPLockedAmount { get; set; }

    public string AwakenLocked { get; set; }
}

public class NodeProduceBlockInfoResp
{
    public long RoundNumber { get; set; }
    public List<NodeProduceBlockInfo> List { get; set; }
}

public class NodeProduceBlockInfo
{
    public string NodeAddress { get; set; }
    public int Order { get; set; }
    public long ExpectingTime { get; set; }
    public int BlockCount { get; set; }
}

public class NodeProduceBlockRequest
{
    public string ChainId { get; set; }
}