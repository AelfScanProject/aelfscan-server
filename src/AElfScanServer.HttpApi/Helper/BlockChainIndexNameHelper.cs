namespace AElfScanServer.HttpApi.Helper;

public class BlockChainIndexNameHelper
{
    public static string GenerateAddressIndexName(string chainId)
    {
        return $"{chainId.ToLower()}_address";
    }


    public static string GenerateTokenIndexName(string chainId)
    {
        return $"{chainId.ToLower()}_token";
    }

    public static string GenerateLogEventIndexName(string chainId)
    {
        return $"{chainId.ToLower()}_logevent";
    }


    public static string GenerateTransactionIndexName(string chainId)
    {
        return $"{chainId.ToLower()}_transaction";
    }

    public static string GenerateBlockExtraIndexName(string chainId)
    {
        return $"{chainId.ToLower()}_block_extra";
    }
}

public class RedisKeyHelper
{
    public static string LatestBlocks(string chainId)
    {
        return $"explore_{chainId}_latest_blocks";
    }


    public static string CurrentBpProduce(string chainId)
    {
        return $"explore_{chainId}_CurrentBpProduce";
    }


    public static string LatestTransactions(string chainId)
    {
        return $"explore_{chainId}_latest_transaction";
    }


    public static string LatestRound(string chainId)
    {
        return $"explore_{chainId}_latest_round";
    }

    public static string AddressSet(string chainId)
    {
        return $"explore_{chainId}_addressSet";
    }

    public static string BlockSizeLastBlockHeight(string chainId)
    {
        return $"explore_{chainId}_BlockSizeLastBlockHeight";
    }


    public static string HomeOverview(string chainId)
    {
        return $"explore_{chainId}_home_overview";
    }

    public static string TransactionChartData(string chainId)
    {
        return $"explore_{chainId}_transaction_chart";
    }


    public static string TransactionLastBlockHeight(string chainId)
    {
        return $"explore_{chainId}_transaction_last_blockheight";
    }


    public static string FixDailyData()
    {
        return $"explore_FixDailyData";
    }

    
    public static string LogEventTransactionLastBlockHeight(string chainId)
    {
        return $"explore_{chainId}_LogEventTransactionLastBlockHeight";
    }

    

    public static string RewardKey(string chainId)
    {
        return $"explore_{chainId}_rewardKey";
    }


    public static string BlockRewardKey(string chainId, long blockHeight)
    {
        return $"explore_{chainId}_block_reward_{blockHeight}";
    }


    public static string TokenInfoKey(string chainId, string symbol)
    {
        return $"explore_{chainId}_token_info_{symbol}";
    }
    
    public static string FixTokenHolder()
    {
        return $"explore_FixTokenHolder";
    }
}