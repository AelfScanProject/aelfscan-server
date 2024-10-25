using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetAddressTokenListInput : BaseInput
{
    [Required] public string Address { get; set; }

    public string Search { get; set; } = "";


    public void SetDefaultSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }


        OfOrderInfos((SortField.FormatAmount, SortDirection.Desc), (SortField.Symbol, SortDirection.Desc));
    }
}

public class GetAddressTokenListResultDto
{
    public decimal AssetInUsd { get; set; }
    public decimal AssetInElf { get; set; }
    public long Total { get; set; }
    public List<TokenInfoDto> List { get; set; }
}