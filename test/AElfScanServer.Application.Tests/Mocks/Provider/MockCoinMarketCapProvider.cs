using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ThirdPart.Exchange;
using CoinGecko.Entities.Response.Coins;

public class MockCoinMarketCapProvider : ICoinMarketCapProvider
{
    private readonly SymbolInfo _mockedSymbolInfo = new SymbolInfo
    {
        Symbol = "ELF",
        Quote = new Dictionary<string, QuoteInfo>()
        {
            ["USD"] = new QuoteInfo
            {
                Price = 100.0m,
                Volume_24h = 500000.0m
            }
        }
    };

    private readonly CoinInfo _mockedCoinInfo = new CoinInfo
    {
        Market_Data = new MarketInfo()
        {
            Current_Price = new Dictionary<string, decimal>
            {
                { "USD", 100.0m },
            }
        },
    };

    public async Task<SymbolInfo> GetVolume24hFromCMC(string symbol)
    {
        return _mockedSymbolInfo;
    }

    public async Task<CoinInfo> GetCurrencyPrice()
    {
        return _mockedCoinInfo;
    }
}