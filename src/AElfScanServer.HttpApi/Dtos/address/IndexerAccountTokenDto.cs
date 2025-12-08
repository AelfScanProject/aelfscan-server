using System;
using System.Collections.Generic;

namespace AElfScanServer.HttpApi.Dtos.address;

public class IndexerAccountTokenDto
{
    public IndexerAccountTokenListDto AccountToken { get; set; }
}

public class IndexerAccountTokenListDto
{
    public long TotalCount { get; set; }
    public List<AccountTokenDto> Items { get; set; } = new();
}


// public class IndexerTokenHolderInfoDto
// {
//     public string Address { get; set; }
//     public IndexerTokenBaseDto Token { get; set; }
//     public long Amount { get; set; }
//     public decimal FormatAmount { get; set; }
//     public long TransferCount { get; set; }
//     public string FirstNftTransactionId { get; set; }
//     public DateTime? FirstNftTime { get; set; }
// }