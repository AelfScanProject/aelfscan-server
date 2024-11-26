using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Token.Provider;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.Token;

public interface ITokenPriceService
{
    Task<CommonTokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin="usdt");

    Task<CommonTokenPriceDto> GetTokenHistoryPriceAsync(string baseCoin, string quoteCoin, long timestamp);
}

public class TokenPriceService : ITokenPriceService, ISingletonDependency
{
    private readonly ILogger<TokenPriceService> _logger;
    private readonly ITokenExchangeProvider _tokenExchangeProvider;
    private readonly IPriceServerProvider _priceServerProvider;

    public TokenPriceService(ILogger<TokenPriceService> logger, ITokenExchangeProvider tokenExchangeProvider,IPriceServerProvider priceServerProvide)
    {
        _logger = logger;
        _tokenExchangeProvider = tokenExchangeProvider;
        _priceServerProvider=priceServerProvide;
    }

    [ExceptionHandler( typeof(Exception),
        Message = "GetHistoryExchangeAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGetTokenPriceAsync), LogTargets = ["baseCoin","quoteCoin"])]
    public virtual async Task<CommonTokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin)
    {       
            AssertHelper.IsTrue(!baseCoin.IsNullOrEmpty() && !quoteCoin.IsNullOrEmpty(),
                "Get token price fail, baseCoin or quoteCoin is empty.");
            
            if (baseCoin.ToUpper().Equals(quoteCoin.ToUpper()))
            {
                return new CommonTokenPriceDto { Price = 1.00m };
            }


            var tokenPriceAsync = await _tokenExchangeProvider.GetTokenPriceAsync(baseCoin, quoteCoin);
            
            return new CommonTokenPriceDto()
            {
                Price =  tokenPriceAsync
            };
      
        
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetTokenHistoryPriceAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGetTokenPriceAsync),LogTargets = ["baseCoin","quoteCoin","timestamp"])]
    public virtual async Task<CommonTokenPriceDto> GetTokenHistoryPriceAsync(string baseCoin, string quoteCoin, long timestamp)
    {
        
            AssertHelper.IsTrue(!baseCoin.IsNullOrEmpty() && !quoteCoin.IsNullOrEmpty() && timestamp > 0,
                "Get token price fail, baseCoin or quoteCoin is empty.");
            if (baseCoin.ToUpper().Equals(quoteCoin.ToUpper()))
            {
                return new CommonTokenPriceDto { Price = 1.00m };
            }

            var exchange = await _tokenExchangeProvider.GetHistoryAsync(baseCoin, quoteCoin, timestamp);
            AssertHelper.NotEmpty(exchange,
                $"History Exchange data {baseCoin}/{quoteCoin} timestamp {timestamp} not found.",
                baseCoin, quoteCoin, timestamp);
            var avgExchange = exchange.Values
                .Where(ex => ex.Exchange > 0)
                .Average(ex => ex.Exchange);
            AssertHelper.IsTrue(avgExchange > 0, "History Exchange amount error {avgExchange}", avgExchange);
            return new CommonTokenPriceDto
            {
                Price = avgExchange
            };
       
    }
}