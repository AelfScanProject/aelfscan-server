using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.Common.Dtos.Input;

public class TokenHolderInput : BaseInput
{
    public string Symbol { get; set; } = "";
    public string CollectionSymbol { get; set; } = "";
    public string Address { get; set; } = "";
    public string PartialSymbol { get; set; } = "";

    public string Search { get; set; } = "";

    public List<SymbolType> Types { get; set; } = new();

    public List<string> Symbols { get; set; } = new();
    
    public List<string> AddressList { get; set; } = new();

    //symbol or collection symbol
    public List<string> SearchSymbols { get; set; } = new();

    public string FuzzySearch { get; set; } = "";

    public void SetDefaultSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.FormatAmount, SortDirection.Desc), (SortField.Address, SortDirection.Desc));
    }
}