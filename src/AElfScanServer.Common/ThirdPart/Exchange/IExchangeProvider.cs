using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.ThirdPart.Exchange;

public interface IExchangeProvider
{
    public ExchangeProviderName Name();

    public Task<TokenExchangeDto> LatestAsync(string fromSymbol, string toSymbol);

    public Task<TokenExchangeDto> HistoryAsync(string fromSymbol, string toSymbol, long timestamp);

}


public enum ExchangeProviderName
{
    Binance,
    Okx,
    CoinGecko,
}