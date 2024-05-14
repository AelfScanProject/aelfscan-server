using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Address.HttpApi.Provider.Entity;

namespace AElfScanServer.Address.HttpApi.Dtos;

public class GetContractHistoryInput : GetDetailBasicDto
{
    [Required] public string Address { get; set; }
}

public class GetContractHistoryResultDto
{
    public List<ContractRecordDto> Record { get; set; }
}

public class ContractRecordDto
{
    public DateTime BlockTime { get; set; }
    public string Address { get; set; }
    public string CodeHash { get; set; }
    public string Author { get; set; }
    public int Version { get; set; }
    public string NameHash { get; set; }
    public string ContractType { get; set; } // 0: SystemContract 1: UserContract
    public string TransactionId { get; set; }
    public long BlockHeight { get; set; }

    public string ContractOperationType { get; set; }
}