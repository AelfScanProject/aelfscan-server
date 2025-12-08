using System;
using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos.Input;

public class TokenListInput : BaseInput
{
    //default Token
    public List<SymbolType> Types { get; set; } = new() { SymbolType.Token };

    public List<string> Symbols { get; set; } = new();

    public List<string> CollectionSymbols { get; set; }

    public string Search { get; set; } = "";

    public string ExactSearch { get; set; } = "";

    public string FuzzySearch { get; set; } = "";
    
    public DateTime? BeginBlockTime { get; set; }

    public void SetDefaultSort()
    {
        if (!Sort.IsNullOrEmpty())
        {
            return;
        }

        Sort = "Desc";
        OrderBy = "HolderCount";
    }
}