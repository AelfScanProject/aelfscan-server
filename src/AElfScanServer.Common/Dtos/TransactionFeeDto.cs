namespace AElfScanServer.Common.Dtos;

public class TransactionFeeDto 
{
    public string Symbol { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountOfUsd { get; set; }
}