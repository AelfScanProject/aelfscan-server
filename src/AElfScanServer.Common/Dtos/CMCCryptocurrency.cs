using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AElfScanServer.Common.Dtos;

public class CMCCryptoCurrency
{
    public CMCStatus Status { get; set; }
    public List<SymbolInfo> Data { get; set; }
}

public class CMCStatus
{
    public DateTime Timestamp { get; set; }
}

public class SymbolInfo
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public Dictionary<string, QuoteInfo> Quote { get; set; }
}

public class QuoteInfo
{
    public decimal Price { get; set; }

    public decimal Volume_24h { get; set; }
}

public class CoinInfo
{
    public MarketInfo Market_Data { get; set; }
}

public class MarketInfo
{
    public Dictionary<string, decimal> Current_Price { get; set; }
}

