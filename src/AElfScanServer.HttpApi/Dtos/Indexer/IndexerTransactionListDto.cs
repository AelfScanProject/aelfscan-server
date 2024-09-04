using System.Collections.Generic;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.HttpApi.Dtos.Indexer;

public class GetIndexerTransactionListInput
{
    public string ChainId { get; set; }

    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; }
}

public class IndexerTransactionResultDto
{
    public IndexerTransactionListResultDto TransactionInfos { get; set; }
}


public class IndexerTransactionByHashResultDto
{
    public IndexerTransactionListResultDto TransactionByHash { get; set; }
}


public class IndexerTransactionListResultDto
{
    public long TotalCount { get; set; }
    public List<IndexerTransactionInfoDto> Items { get; set; }
}

public class IndexerTransactionInfoDto
{
    public string TransactionId { get; set; }
    public long BlockHeight { get; set; }

    public string MethodName { get; set; }

    public TransactionStatus Status { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public long TransactionValue { get; set; }

    public MetadataDto Metadata { get; set; }


    public long Fee { get; set; }
}