using System;
using System.Collections.Generic;
using System.Transactions;
using Newtonsoft.Json;

namespace AElfScanServer.BlockChain.Dtos;

public class GetTokenResp
{
    [JsonProperty("access_token")] public string AccessToken { get; set; }
    [JsonProperty("token_type")] public string TokenType { get; set; }
    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
}

public class IndexerBlockDto
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    // public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }

    // public string SignerPubkey { get; set; }
    public string Miner { get; set; }

    // public string Signature { get; set; }
    public bool Confirmed { get; set; }
    // public Dictionary<string, string> ExtraProperties { get; set; }

    // public List<TransactionDto> Transactions {get;set;}
    public List<string> TransactionIds { get; set; } = new();
    // public int LogEventCount { get; set; }
}

public class IndexSummaries
{
    public long LatestBlockHeight { get; set; }
    public string LatestBlockHash { get; set; }
    public long ConfirmedBlockHeight { get; set; }
    public string ConfirmedBlockHash { get; set; }
}

public class IndexerTransactionCountDto
{
    public long Count { get; set; }
}

public class IndexerTransactionDto
{
    public string TransactionId { get; set; }

    public string ChainId { get; set; }

    public string From { get; set; }

    public string To { get; set; }

    // public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    // public string PreviousBlockHash { get; set; }

    public DateTime BlockTime { get; set; }

    public string MethodName { get; set; }

    public string Params { get; set; }

    public string Signature { get; set; }

    /// <summary>
    /// The ranking position of transactions within a block
    /// </summary>
    // public int Index { get; set; }

    public TransactionStatus Status { get; set; }

    public bool Confirmed { get; set; }

    public Dictionary<string, string> ExtraProperties { get; set; }

    public List<IndexerLogEventDto> LogEvents { get; set; }
}

public class IndexerLogEventDto
{
    // public string ChainId { get; set; }
    public string BlockHash { get; set; }


    public long BlockHeight { get; set; }

    // public string PreviousBlockHash { get; set; }

    public string TransactionId { get; set; }

    // public DateTime BlockTime { get; set; }

    public string ContractAddress { get; set; }

    public string EventName { get; set; }

    public int Index { get; set; }

    // public bool Confirmed { get; set; }

    public Dictionary<string, string> ExtraProperties { get; set; }
}

public class Header
{
    public string PreviousBlockHash { get; set; }
    public string MerkleTreeRootOfTransactions { get; set; }
    public string MerkleTreeRootOfWorldState { get; set; }
    public string MerkleTreeRootOfTransactionState { get; set; }

    public string Extra { get; set; }
    // public string Height { get; set; }
    // public string Time { get; set; }
    // public string ChainId { get; set; }
    // public string Bloom { get; set; }
    // public string SignerPubkey { get; set; }
}

public class BlockDetailDto
{
    // public string BlockHash { get; set; }
    public Header Header { get; set; }

    // public Body Body { get; set; }
    public int BlockSize { get; set; }
}

public class NodeTransactionDto
{
    public NodeTransactionInfo Transaction { get; set; }
}

public class NodeTransactionInfo
{
    public string Params { get; set; }
}