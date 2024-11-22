// using Volo.Abp.Application.Dtos;
//
// namespace Portkey.backend;
//

using Volo.Abp.Application.Dtos;

public class GetTransactionsReq : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public List<string> CaAddress { get; set; }
}
//
// public class TransactionsResponseDto
// {
//     public long Total { get; set; }
//     public List<TransactionResponseDto> Transactions { get; set; } = new List<TransactionResponseDto>();
// }
//
// public class TransactionResponseDto
// {
//     public string TransactionId { get; set; }
//
//     public long BlockHeight { get; set; }
//
//     public string Method { get; set; }
//
//     public TransactionStatus Status { get; set; } = TransactionStatus.Mined;
//     public CommonAddressDto From { get; set; }
//
//     public CommonAddressDto To { get; set; }
//
//     public long Timestamp { get; set; }
//
//     public string TransactionValue { get; set; }
//
//     public string TransactionFee { get; set; }
// }
//
// public class CommonAddressDto
// {
//     public string Name { get; set; }
//     public string Address { get; set; }
//     public AddressType AddressType { get; set; }
//     public bool IsManager { get; set; }
//     public bool IsProducer { get; set; }
// }
//
// public enum AddressType
// {
//     EoaAddress,
//     ContractAddress
// }
//
// public enum TransactionStatus
// {
//     /// <summary>
//     /// The execution result of the transaction does not exist.
//     /// </summary>
//     NotExisted = 0,
//
//     /// <summary>
//     /// The transaction is in the transaction pool waiting to be packaged.
//     /// </summary>
//     Pending = 1,
//
//     /// <summary>
//     /// Transaction execution failed.
//     /// </summary>
//     Failed = 2,
//
//     /// <summary>
//     /// The transaction was successfully executed and successfully packaged into a block.
//     /// </summary>
//     Mined = 3,
//
//     /// <summary>
//     /// When executed in parallel, there are conflicts with other transactions.
//     /// </summary>
//     Conflict = 4,
//
//     /// <summary>
//     /// The transaction is waiting for validation.
//     /// </summary>
//     PendingValidation = 5,
//
//     /// <summary>
//     /// Transaction validation failed.
//     /// </summary>
//     NodeValidationFailed = 6,
// }