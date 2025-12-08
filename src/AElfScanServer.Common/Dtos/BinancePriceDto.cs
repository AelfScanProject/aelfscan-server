namespace AElfScanServer.Common.Dtos;

public class BinancePriceDto
{
    public string Symbol { get; set; }
    public decimal PriceChangePercent { get; set; }
    public decimal LastPrice { get; set; }
}