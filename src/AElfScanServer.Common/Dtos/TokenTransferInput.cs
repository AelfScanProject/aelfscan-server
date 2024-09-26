using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.Helper;

namespace AElfScanServer.Common.Dtos;

public class TokenTransferInput : BaseInput

{
    public string Symbol { get; set; } = "";
    public string Search { get; set; } = "";
    public string CollectionSymbol { get; set; } = "";

    public string Address { get; set; } = "";

    public List<SymbolType> Types { get; set; } = new() { SymbolType.Token };

    public string FuzzySearch { get; set; } = "";

    public DateTime? BeginBlockTime { get; set; }

    public bool IsSearchAddress()
    {
        return !Search.IsNullOrWhiteSpace() && CommomHelper.IsValidAddress(Search);
    }

    public void SetDefaultSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockHeight, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }


    public void SetBlockTimeSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockTime, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }
}