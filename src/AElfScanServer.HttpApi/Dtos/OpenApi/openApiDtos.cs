namespace AElfScanServer.HttpApi.Dtos.OpenApi;

public class SupplyApiResp
{
    public decimal MaxSupply { get; set; }
    public decimal Burn { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal CirculatingSupply { get; set; }
}

public class DailyActivityAddressApiResp
{
    public AddressInfo MainChain { get; set; } = new();
    public AddressInfo SideChain { get; set; } = new();
}

public class AddressInfo
{
    public long Max { get; set; }
    public long Min { get; set; }
    public long Avg { get; set; }
}

public class DailyTransactionCountApiResp
{
    public TransactionInfo MainChain { get; set; } = new();
    public TransactionInfo SideChain { get; set; } = new();
}

public class TransactionInfo
{
    public decimal TransactionAvgByAllType { get; set; }
    public decimal TransactionAvgByExcludeSystem { get; set; }
}



public class CurrencyPrice
{
    public string Symbol { get; set; }
    public string CurrencyCode { get; set; }
}