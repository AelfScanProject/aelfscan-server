using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElfScanServer.BlockChain.Dtos;
using AElfScanServer.BlockChain.Options;
using AElfScanServer;
using AElfScanServer.HttpClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.BlockChain.Provider;

public static class AELFIndexerApi
{
    public static ApiInfo GetTransaction { get; } = new(HttpMethod.Post, "/api/app/block/transactions");

    public static ApiInfo GetBlock { get; } = new(HttpMethod.Post, "/api/app/block/blocks");
    public static ApiInfo GetLogEvent { get; } = new(HttpMethod.Post, "/api/app/block/logEvents");

    public static ApiInfo GetLatestBlockHeight { get; } = new(HttpMethod.Post, "/api/app/block/summaries");

    public static ApiInfo GetToken { get; } = new(HttpMethod.Post, "/connect/token");
}

public class AELFIndexerProvider : ISingletonDependency

{
    private readonly ILogger<AELFIndexerProvider> _logger;
    private readonly AELFIndexerOptions _aelfIndexerOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<string> _tokenCache;
    private readonly IDistributedCache<string> _blockHeightCache;


    public const string TokenCacheKey = "AELFIndexerToken";
    public const string BlockHeightCacheKey = "AELFIndexerBlockHeight";

    public AELFIndexerProvider(ILogger<AELFIndexerProvider> logger,
        IOptionsMonitor<AELFIndexerOptions> aelfIndexerOptions, IHttpProvider httpProvider,
        IDistributedCache<string> tokenCache, IDistributedCache<string> blockHeightCache)
    {
        _logger = logger;
        _aelfIndexerOptions = aelfIndexerOptions.CurrentValue;
        _httpProvider = httpProvider;
        _tokenCache = tokenCache;
        _blockHeightCache = blockHeightCache;
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
                    { "client_id", _aelfIndexerOptions.ClientId }, { "client_secret", _aelfIndexerOptions.ClientSecret }
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
        var blockhieght = await _blockHeightCache.GetAsync(BlockHeightCacheKey);

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
        await _blockHeightCache.SetAsync(BlockHeightCacheKey, latestBlockHeight.ToString(),
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


    public async Task<List<IndexerTransactionDto>> GetTransactionsAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        var accessTokenAsync = GetAccessTokenAsync();
        var response =
            await _httpProvider.PostAsync<List<IndexerTransactionDto>>(
                _aelfIndexerOptions.AELFIndexerHost + AELFIndexerApi.GetTransaction.Path,
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


        _logger.LogInformation(
            "get transaction list from AELFIndexer success,total:{total},chainId:{chainId},startBlockHeight:{startBlockHeight},endBlockHeight:{endBlockHeight}",
            response?.Count, chainId, response?.First()?.BlockHeight,
            response?.Last()?.BlockHeight);

        return response;
    }


    public async Task<List<IndexerLogEventDto>> GetLogEventAsync(string chainId, long startBlockHeight,
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
                                "Burned"
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