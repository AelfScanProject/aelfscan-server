using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.Common;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.HttpClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public static class AELFIndexerApi
{
    public static ApiInfo GetTransaction { get; } = new(HttpMethod.Post, "/api/app/block/transactions");

    public static ApiInfo GetBlock { get; } = new(HttpMethod.Post, "/api/app/block/blocks");
    public static ApiInfo GetLogEvent { get; } = new(HttpMethod.Post, "/api/app/block/logEvents");

    public static ApiInfo GetLatestBlockHeight { get; } = new(HttpMethod.Post, "/api/app/block/summaries");

    public static ApiInfo GetToken { get; } = new(HttpMethod.Post, "/connect/token");
}

public interface IAELFIndexerProvider
{
    Task<List<IndexerBlockDto>> GetLatestBlocksAsync(string chainId, long startBlockHeight, long endBlockHeight);
    Task<long> GetLatestBlockHeightAsync(string chainId);
    Task<List<IndexSummaries>> GetLatestSummariesAsync(string chainId);
    Task<List<TransactionIndex>> GetTransactionsAsync(string chainId, long startBlockHeight, long endBlockHeight, string transactionId);
    Task<List<TransactionData>> GetTransactionsDataAsync(
        string chainId,
        long startBlockHeight,
        long endBlockHeight,
        string transactionId
    );
}

public class AELFIndexerProvider : IAELFIndexerProvider, ISingletonDependency

{
    private readonly ILogger<AELFIndexerProvider> _logger;
    private readonly AELFIndexerOptions _aelfIndexerOptions;
    private readonly SecretOptions _secretOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<string> _tokenCache;
    private readonly IDistributedCache<string> _blockHeightCache;


    public const string TokenCacheKey = "AELFIndexerToken";
    public const string BlockHeightCacheKey = "AELFIndexerBlockHeight";

    public AELFIndexerProvider(ILogger<AELFIndexerProvider> logger,
        IOptionsMonitor<AELFIndexerOptions> aelfIndexerOptions, IOptionsMonitor<SecretOptions> secretOptions,IHttpProvider httpProvider,
        IDistributedCache<string> tokenCache, IDistributedCache<string> blockHeightCache)
    {
        _logger = logger;
        _aelfIndexerOptions = aelfIndexerOptions.CurrentValue;
        _httpProvider = httpProvider;
        _tokenCache = tokenCache;
        _blockHeightCache = blockHeightCache;
        _secretOptions = secretOptions.CurrentValue;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var token = await _tokenCache.GetAsync(TokenCacheKey);

        if (!token.IsNullOrEmpty())
        {
            return token;
        }

        var response =
            await _httpProvider.PostAsync<GetTokenResp>(_aelfIndexerOptions.GetTokenHost + AELFIndexerApi.GetToken.Path,
                RequestMediaType.Form, new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }, { "scope", "AeFinder" },
                    { "client_id", _secretOptions.ClientId }, { "client_secret", _secretOptions.ClientSecret }
                },
                new Dictionary<string, string>
                {
                    ["content-type"] = "application/x-www-form-urlencoded",
                    ["accept"] = "application/json"
                });

        AssertHelper.NotNull(response?.AccessToken, "AccessToken response null");

        await _tokenCache.SetAsync(TokenCacheKey, response.AccessToken, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration =
                DateTimeOffset.UtcNow.AddSeconds(_aelfIndexerOptions.AccessTokenExpireDurationSeconds)
        });

        return response?.AccessToken;
    }

    public async Task<List<IndexerBlockDto>> GetLatestBlocksAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        var accessTokenAsync = GetAccessTokenAsync();
        var response =
            await _httpProvider.PostAsync<List<IndexerBlockDto>>(
                _aelfIndexerOptions.AELFIndexerHost + AELFIndexerApi.GetBlock.Path,
                RequestMediaType.Json, new Dictionary<string, object>
                {
                    ["chainId"] = chainId,
                    ["startBlockHeight"] = startBlockHeight,
                    ["endBlockHeight"] = endBlockHeight,
                },
                new Dictionary<string, string>
                {
                    ["content-type"] = "application/json",
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {accessTokenAsync.Result}"
                });

        return response;
    }


    public async Task<long> GetLatestBlockHeightAsync(string chainId)
    {
        var blockhieght = await _blockHeightCache.GetAsync(BlockHeightCacheKey + chainId);

        if (!blockhieght.IsNullOrEmpty())
        {
            return long.Parse(blockhieght);
        }

        var accessTokenAsync = await GetAccessTokenAsync();
        var response =
            await _httpProvider.PostAsync<List<IndexSummaries>>(
                _aelfIndexerOptions.AELFIndexerHost + AELFIndexerApi.GetLatestBlockHeight.Path,
                RequestMediaType.Json, new Dictionary<string, object>
                {
                    ["chainId"] = chainId
                },
                new Dictionary<string, string>
                {
                    ["content-type"] = "application/json",
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {accessTokenAsync}"
                });

        var latestBlockHeight = response[0].LatestBlockHeight;
        await _blockHeightCache.SetAsync(BlockHeightCacheKey + chainId, latestBlockHeight.ToString(),
            new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddSeconds(2)
            });

        return latestBlockHeight;
    }

    public async Task<List<IndexSummaries>> GetLatestSummariesAsync(string chainId)
    {
        var accessTokenAsync = await GetAccessTokenAsync();
        var response =
            await _httpProvider.PostAsync<List<IndexSummaries>>(
                _aelfIndexerOptions.AELFIndexerHost + AELFIndexerApi.GetLatestBlockHeight.Path,
                RequestMediaType.Json, new Dictionary<string, object>
                {
                    ["chainId"] = chainId
                },
                new Dictionary<string, string>
                {
                    ["content-type"] = "application/json",
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {accessTokenAsync}"
                });


        return response;
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetTransactionsAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGetTransactionsAsync), LogTargets = ["chainId","startBlockHeight","endBlockHeight","transactionId"])]
    public virtual async Task<List<TransactionIndex>> GetTransactionsAsync(string chainId, long startBlockHeight,
        long endBlockHeight, string transactionId)
    {
       
            var accessTokenAsync = GetAccessTokenAsync();
            var response =
                await _httpProvider.PostAsync<List<TransactionIndex>>(
                    _aelfIndexerOptions.AELFIndexerHost + AELFIndexerApi.GetTransaction.Path,
                    RequestMediaType.Json, new Dictionary<string, object>
                    {
                        ["chainId"] = chainId,
                        ["startBlockHeight"] = startBlockHeight,
                        ["endBlockHeight"] = endBlockHeight,
                        ["transactionId"] = transactionId
                    },
                    new Dictionary<string, string>
                    {
                        ["content-type"] = "application/json",
                        ["accept"] = "application/json",
                        ["Authorization"] = $"Bearer {accessTokenAsync.Result}"
                    });


            return response;
    
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetTransactionsDataAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGetTransactionsDataAsync), LogTargets = ["chainId","startBlockHeight","endBlockHeight","transactionId"])]
    public virtual async Task<List<TransactionData>> GetTransactionsDataAsync(string chainId, long startBlockHeight,
        long endBlockHeight, string transactionId)
    {
       
            var accessTokenAsync = GetAccessTokenAsync();
            var response =
                await _httpProvider.PostAsync<List<TransactionData>>(
                    _aelfIndexerOptions.AELFIndexerHost + AELFIndexerApi.GetTransaction.Path,
                    RequestMediaType.Json, new Dictionary<string, object>
                    {
                        ["chainId"] = chainId,
                        ["startBlockHeight"] = startBlockHeight,
                        ["endBlockHeight"] = endBlockHeight,
                        ["transactionId"] = transactionId
                    },
                    new Dictionary<string, string>
                    {
                        ["content-type"] = "application/json",
                        ["accept"] = "application/json",
                        ["Authorization"] = $"Bearer {accessTokenAsync.Result}"
                    });


            return response;
        
    }



    public async Task<List<IndexerLogEventDto>> GetTokenCreatedLogEventAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        var accessTokenAsync = GetAccessTokenAsync();
        var response =
            await _httpProvider.PostAsync<List<IndexerLogEventDto>>(
                _aelfIndexerOptions.AELFIndexerHost + AELFIndexerApi.GetLogEvent.Path,
                RequestMediaType.Json, new Dictionary<string, object>
                {
                    ["chainId"] = chainId,
                    ["startBlockHeight"] = startBlockHeight,
                    ["endBlockHeight"] = endBlockHeight,
                    ["events"] = new List<Dictionary<string, object>>
                    {
                        new()
                        {
                            ["eventNames"] = new List<string>()
                            {
                                "TokenCreated"
                            }
                        }
                    }
                },
                new Dictionary<string, string>
                {
                    ["content-type"] = "application/json",
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {accessTokenAsync.Result}"
                });


        // _logger.LogInformation(
        //     "get log event list from AELFIndexer success,total:{total},chainId:{chainId},startBlockHeight:{startBlockHeight},endBlockHeight:{endBlockHeight}",
        //     response?.Count, chainId, response?.First()?.BlockHeight,
        //     response?.Last()?.BlockHeight);

        return response;
    }
}