using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetContractHistoryInput : GetDetailBasicDto
{
    [Required] public string Address { get; set; }
}

public class GetContractHistoryResultDto
{
    public List<ContractRecordDto> Record { get; set; }
}

// public class ContractRecordDto
// {
//     public DateTime BlockTime { get; set; }
//     public string Address { get; set; }
//     public string CodeHash { get; set; }
//     public string Author { get; set; }
//     public int Version { get; set; }
//     public string NameHash { get; set; }
//     public string ContractType { get; set; } // 0: SystemContract 1: UserContract
//     public string TransactionId { get; set; }
//     public long BlockHeight { get; set; }
//
//     public string ContractOperationType { get; set; }
// }

public class ContractRecordDto : GraphQLDto
{
    public string OperationType { get; set; }
    public string Operator { get; set; }
    public string TransactionId { get; set; }
    public string Author { get; set; }
    public string Address { get; set; }
    public string Version { get; set; }
    public ContractInfoDto ContractInfo { get; set; }
    public MetadataDto Metadata { get; set; }
    public string ContractType { get; set; } // 0: SystemContract 1: UserContract
    public string ContractOperationType { get; set; }
}

public enum ContractOperationType
{
    DeployContract,
    UpdateContract,
    SetAuthor
}