using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.Common.Dtos;

public class TokenTransferInfoDto
{
    public string ChainId { get; set; }
    public List<string> ChainIds { get; set; } = new();
    public string TransactionId { get; set; }
    public string Method { get; set; }
    public long BlockHeight { get; set; }
    public long BlockTime { get; set; }
    public string Symbol { get; set; }

    public string SymbolName { get; set; }
    
    public int IsIn { get; set; }

    public DateTime DateTime
    {
        get => DateTimeOffset.FromUnixTimeSeconds(BlockTime).DateTime;
    }

    public string SymbolImageUrl { get; set; }
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
    public decimal Quantity { get; set; }
    

    public TransactionStatus Status { get; set; }

    public List<TransactionFeeDto> TransactionFeeList { get; set; }
}

public class TokenTransferInfosDto : ListResponseDto<TokenTransferInfoDto>
{
    public bool IsAddress { get; set; }
    public decimal Balance { get; set; }
    public decimal Value { get; set; }
}