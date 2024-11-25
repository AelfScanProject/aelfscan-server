using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Token;

namespace AElfScanServer.Mocks.Provider;

public class MockTokenPriceService: ITokenPriceService
{
    public async Task<CommonTokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin = "usdt")
    {
       return new CommonTokenPriceDto
       {
           Price = 1
       };
    }

    public async Task<CommonTokenPriceDto> GetTokenHistoryPriceAsync(string baseCoin, string quoteCoin, long timestamp)
    {
        return new CommonTokenPriceDto
        {
            Price = 1
        };
    }
}