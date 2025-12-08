namespace AElfScanServer.HttpApi.Constant;

public static class BlockChainConstant
{
    private const string BlockChainBaseUri = "api/blockchain/";
    public const string AddressDicApi = BlockChainBaseUri + "addressDictionary";
    public const string BlockListUri = BlockChainBaseUri + "blocks";
    public const string TransactionsUri = BlockChainBaseUri + "transactions";
    public const string LatestBlocksUri = BlockChainBaseUri + "latestBlocks";
    public const string LatestTransactionsUri = BlockChainBaseUri + "latestTransactions";
    public const string FiltersUri = BlockChainBaseUri + "filters";
    public const string LogEventsUri = BlockChainBaseUri + "logEvents";
}