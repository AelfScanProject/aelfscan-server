using System.Collections.Generic;


namespace AElfScanServer.Common.Dtos.Indexer;

public class IndexerTransferInfoDto
{
    public string Id { get; set; } = "";
    public string TransactionId { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Method { get; set; }
    
    public long Amount { get; set; } 
    public decimal FormatAmount { get; set; }
    public IndexerTokenBaseDto Token { get; set; }
    public MetadataDto Metadata { get; set; }
    
    public string Status { get; set; }
    
    public List<ExternalInfoDto> ExtraProperties { get; set; } = new();
}

public class IndexerTokenTransfersDto
{
    public IndexerTokenTransferListDto TransferInfo { get; set; }
}

public class IndexerTokenTransferListDto
{
    public long TotalCount  { get; set; }
    public List<IndexerTransferInfoDto> Items { get; set; } = new();
}