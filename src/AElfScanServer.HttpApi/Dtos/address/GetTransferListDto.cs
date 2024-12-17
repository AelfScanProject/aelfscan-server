using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetTransferListInput : BaseInput
{
    [Required] public string Address { get; set; }
     public string Symbol { get; set; }

    public SymbolType TokenType { get; set; } = SymbolType.Token;

    public void SetDefaultSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockTime, SortDirection.Desc));
    }
}

public class GetTransferListResultDto
{
    public long Total { get; set; }

    public List<TokenTransferInfoDto> List { get; set; }
}