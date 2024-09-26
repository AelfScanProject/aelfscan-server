using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetContractContracts : PagedResultRequestDto
{
    public string ChainId { get; set; } = "";

    public string OrderBy { get; set; } = "";

    public string Sort { get; set; } = "";
}

public class GetContractListResultDto
{
    public long Total { get; set; }
    public List<ContractDto> List { get; set; }
}

public class ContractDto
{
    public string Address { get; set; }
    public string ContractName { get; set; } = "-";
    public string Type { get; set; }
    public List<string> ChainIds { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public long Txns { get; set; }
    public string ContractVersion { get; set; } = "-";
}