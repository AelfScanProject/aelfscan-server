namespace AElfScanServer.HttpApi.Helper;

public class HubGroupHelper
{
    public static string GetLatestTransactionsGroupName(string chainId)
    {
        return $"{chainId}_latest_transactions";
    }
    
    public static string GetMergeBlockInfoGroupName(string chainId="")
    {
        return $"{chainId}_merge_blockInfo";
    }

    
    
    public static string GetBpProduceGroupName(string chainId)
    {
        return $"{chainId}_BpProduce";
    }

    public static string GetTransactionCountPerMinuteGroupName(string chainId="")
    {
        return $"{chainId}_transactions_count_per_minute";
    }


    public static string GetLatestBlocksGroupName(string chainId)
    {
        return $"{chainId}_latest_blocks";
    }


    public static string GetBlockOverviewGroupName(string chainId)
    {
        return $"{chainId}_block_overview";
    }
}