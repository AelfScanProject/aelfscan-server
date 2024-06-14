using System;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Common.Dtos;


namespace AElfScanServer.HttpApi.Dtos.address;

public class GetAddressDetailInput : GetDetailBasicDto
{
    [Required] public string Address { get; set; }
    [Required] public AddressType AddressType { get; set; }
}

public class GetAddressDetailResultDto
{
    public decimal ElfBalance { get; set; }
    public decimal ElfBalanceOfUsd { get; set; }
    public decimal ElfPriceInUsd { get; set; }
    public long TokenHoldings { get; set; }
    
    public decimal TotalValueOfUsd { get; set; }
    public decimal TotalValueOfElf { get; set; }
    public decimal TotalValueOfUsdChangeRate { get; set; }

    // only address type is caAddress|eocAddress
    public TransactionInfoDto FirstTransactionSend { get; set; }
    public TransactionInfoDto LastTransactionSend { get; set; }

    // only address type is contract
    public string ContractName { get; set; }
    public string Author { get; set; }
    public string ContractTransactionHash { get; set; }
}

public class TransactionInfoDto
{
    public string TransactionId { get; set; }
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
}