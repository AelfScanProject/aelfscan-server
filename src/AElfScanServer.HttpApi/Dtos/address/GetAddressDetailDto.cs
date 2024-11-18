using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Common.Dtos;


namespace AElfScanServer.HttpApi.Dtos.address;

public class GetAddressDetailInput : GetDetailBasicDto
{
    [Required] public string Address { get; set; }
}

public class GetAddressDetailResultDto
{
    public AddressType AddressType { get; set; } = AddressType.EoaAddress;
    public List<string> AddressTypeList { get; set; }

    public string ContractName { get; set; }

    public string Author { get; set; }


    public Portfolio Portfolio { get; set; } = new();

    public List<string> ChainIds { get; set; } = new();
}

public class Portfolio
{
    public long MainTokenCount { get; set; }
    public long SideTokenCount { get; set; }
    public long MainNftCount { get; set; }
    public long SideNftCount { get; set; }
    public decimal MainTokenValue { get; set; }
    public decimal SideTokenValue { get; set; }
    public long TotalTokenCount { get; set; }
    public long TotalNftCount { get; set; }

    public decimal TotalTokenValue
    {
        get { return MainTokenValue + SideTokenValue; }
    }
}

public class TransactionInfoDto
{
    public string TransactionId { get; set; }

    // public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
}