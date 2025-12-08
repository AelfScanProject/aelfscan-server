using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace AElfScanServer.HttpApi.Dtos.address;

public class GetTransactionListInput : GetListInputBasicDto
{
    [Required] public string Address { get; set; }
}

public class GetTransactionListResultDto
{
    public long Total { get; set; }
    public List<TransactionResponseDto> Transactions { get; set; }
}