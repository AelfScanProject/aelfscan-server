using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElfScanServer.Common.Dtos.Ads;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.Grains;
using AElfScanServer.Grains.Grain.Ads;
using AElfScanServer.Grains.State.Ads;
using AElfScanServer.HttpApi.Dtos.AdsData;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Worker.Core.Dtos;
using Elasticsearch.Net;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.HttpApi.Service;

public interface IAdsService
{
    public Task<AdsResp> GetAds(AdsReq req);

    public Task<AdsIndex> UpdateAds(UpdateAdsReq req);


    public Task<AdsIndex> DeleteAds(DeleteAdsReq req);


    public Task<AdsListResp> GetAdsList(GetAdsListReq req);


    public Task<AdsBannerResp> GetAdsBanner(AdsBannerReq req);

    public Task<AdsBannerIndex> UpdateAdsBanner(UpdateAdsBannerReq req);

    public Task<AdsBannerIndex> DeleteAdsBanner(DeleteAdsBannerReq req);

    public Task<AdsBannerListResp> GetAdsBannerList(GetAdsBannerListReq req);

    public Task<List<TwitterIndex>> GetLatestTwitterListAsync(int maxResultCount);
    public Task SaveTwitterListAsync();
}

public class AdsService : AbpRedisCache, IAdsService, ITransientDependency
{
    private readonly IOptionsMonitor<SecretOptions> _secretOptions;
    private readonly IOptionsMonitor<TwitterOptions> _twitterOptions;
    private readonly ILogger<AdsService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<AdsIndex, string> _adsRepository;
    private readonly IEntityMappingRepository<AdsBannerIndex, string> _adsBannerRepository;
    private readonly IEntityMappingRepository<TwitterIndex, string> _twitterRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IElasticClient _elasticClient;
    private readonly ITwitterProvider _twitterProvider;

    public AdsService(IOptions<RedisCacheOptions> optionsAccessor, IOptionsMonitor<SecretOptions> secretOptions,
        ILogger<AdsService> logger,
        IClusterClient clusterClient, IEntityMappingRepository<AdsIndex, string> adsRepository,
        IObjectMapper objectMapper, IOptionsMonitor<ElasticsearchOptions> options,
        IEntityMappingRepository<AdsBannerIndex, string> adsBannerRepository,
        IEntityMappingRepository<TwitterIndex, string> twitterRepository,
        IOptionsMonitor<TwitterOptions> twitterOptions,
        ITwitterProvider twitterProvider) : base(
        optionsAccessor)
    {
        _secretOptions = secretOptions;
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _adsRepository = adsRepository;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool);
        _elasticClient = new ElasticClient(settings);
        _adsBannerRepository = adsBannerRepository;
        _twitterRepository = twitterRepository;
        _twitterOptions = twitterOptions;
        _twitterProvider = twitterProvider;
    }

    public async Task<AdsBannerListResp> GetAdsBannerList(GetAdsBannerListReq req)
    {
        var result = new AdsBannerListResp();
        var searchResponse = await _elasticClient.SearchAsync<AdsBannerIndex>(s => s
            .Index("adsbannerindex")
            .Query(q => q
                .Bool(b => b
                    .Must(m =>
                        {
                            if (req.Labels == null || !req.Labels.Any())
                                return m.MatchAll();
                            else
                                return m.Terms(t => t
                                    .Field(f => f.Labels.Suffix("keyword"))
                                    .Terms(req.Labels)
                                );
                        },
                        m =>
                        {
                            if (req.AdsBannerId.IsNullOrEmpty())
                                return m.MatchAll();
                            else
                                return m.Term(t => t
                                    .Field(f => f.AdsBannerId).Value(req.AdsBannerId)
                                );
                        })
                )
            )
            .Sort(sort => sort
                .Field(f => f
                    .Field(c => c.CreateTime)
                    .Order(SortOrder.Descending)
                )
            )
            .Size(1000)
        );

        var adsIndexes = searchResponse.Documents.ToList();
        result.List = adsIndexes;
        result.Total = adsIndexes.Count;
        return result;
    }

    public async Task<List<TwitterIndex>> GetLatestTwitterListAsync(int maxResultCount)
    {

        var queryable = await _twitterRepository.GetQueryableAsync();
        return queryable.OrderByDescending(c => c.Id).Take(maxResultCount).ToList();
    }

    public async Task SaveTwitterListAsync()
    {
        var latestTwitterListAsync = await GetLatestTwitterListAsync(1);
        var sinceId = "0";
        if (!latestTwitterListAsync.IsNullOrEmpty())
        {
            sinceId = latestTwitterListAsync[0].Id;
        }

       var twitterList = await _twitterProvider.GetLatestTwittersAsync(_secretOptions.CurrentValue.TwitterBearToken,
            _twitterOptions.CurrentValue.TwitterUserId, sinceId);
       if (!twitterList.IsNullOrEmpty())
       {
           var list = _objectMapper.Map<List<Tweet>, List<TwitterIndex>>(twitterList);
           await _twitterRepository.AddOrUpdateManyAsync(list);
       }
    }

    public async Task<AdsListResp> GetAdsList(GetAdsListReq req)
    {
        var result = new AdsListResp();
        var searchResponse = await _elasticClient.SearchAsync<AdsIndex>(s => s
            .Index("adsindex")
            .Query(q => q
                .Bool(b => b
                    .Must(m =>
                        {
                            if (req.Labels == null || !req.Labels.Any())
                                return m.MatchAll();
                            else
                                return m.Terms(t => t
                                    .Field(f => f.Labels.Suffix("keyword"))
                                    .Terms(req.Labels)
                                );
                        },
                        m =>
                        {
                            if (req.AdsId.IsNullOrEmpty())
                                return m.MatchAll();
                            else
                                return m.Term(t => t
                                    .Field(f => f.AdsId).Value(req.AdsId)
                                );
                        })
                )
            )
            .Sort(sort => sort
                .Field(f => f
                    .Field(c => c.CreateTime)
                    .Order(SortOrder.Descending)
                )
            )
            .Size(1000)
        );

        var adsIndexes = searchResponse.Documents.ToList();
        result.List = adsIndexes;
        result.Total = adsIndexes.Count;
        return result;
    }

    public async Task<AdsBannerResp> GetAdsBanner(AdsBannerReq req)
    {
        var key = GrainIdHelper.GenerateAdsBannerKey(req.SearchKey, req.Label);
        await ConnectAsync();
        var adsBannerVisitCount = RedisDatabase.StringGet(key);
        var adsBannerList = new List<AdsBannerIndex>();
        var adsBannerResp = new AdsBannerResp();
        adsBannerResp.SearchKey = key;
        adsBannerList = await QueryAdsBannerList(req.Label, "", 1000);
        if (adsBannerList.IsNullOrEmpty())
        {
            return adsBannerResp;
        }

        if (adsBannerVisitCount.IsNullOrEmpty)
        {
            var adsBannerIndex = adsBannerList.First();
            adsBannerResp = _objectMapper.Map<AdsBannerIndex, AdsBannerResp>(adsBannerIndex);

            RedisDatabase.StringIncrement(key);
            RedisDatabase.KeyExpire(key, TimeSpan.FromDays(7));
            adsBannerResp.SearchKey = key;
            return adsBannerResp;
        }

        var count = long.Parse(adsBannerVisitCount);

        var totalVisitCount = 0;
        foreach (var ads in adsBannerList)
        {
            totalVisitCount += ads.TotalVisitCount;
            if (count < totalVisitCount)
            {
                RedisDatabase.StringIncrement(key);
                var resp = _objectMapper.Map<AdsBannerIndex, AdsBannerResp>(ads);
                resp.SearchKey = key;
                return resp;
            }
        }

        adsBannerResp.SearchKey = key;
        RedisDatabase.StringSet(key, 1);
        return _objectMapper.Map<AdsBannerIndex, AdsBannerResp>(adsBannerList.First());
    }


    public async Task<AdsResp> GetAds(AdsReq req)
    {
        var key = GrainIdHelper.GenerateAdsKey(req.SearchKey, req.Label);
        await ConnectAsync();
        var adsVisitCount = RedisDatabase.StringGet(key);
        var adsList = new List<AdsIndex>();
        var adsResp = new AdsResp();

        adsList = await QueryAdsList(req.Label, "", 1000);
        if (adsList.IsNullOrEmpty())
        {
            adsResp.SearchKey = key;
            return adsResp;
        }

        if (adsVisitCount.IsNullOrEmpty)
        {
            var adsIndex = adsList.First();
            adsResp = _objectMapper.Map<AdsIndex, AdsResp>(adsIndex);

            RedisDatabase.StringIncrement(key);
            RedisDatabase.KeyExpire(key, TimeSpan.FromDays(7));
            adsResp.SearchKey = key;
            return adsResp;
        }

        var count = long.Parse(adsVisitCount);

        var totalVisitCount = 0;
        foreach (var ads in adsList)
        {
            totalVisitCount += ads.TotalVisitCount;
            if (count < totalVisitCount)
            {
                RedisDatabase.StringIncrement(key);
                var resp = _objectMapper.Map<AdsIndex, AdsResp>(ads);
                resp.SearchKey = key;
                return resp;
            }
        }

        adsResp.SearchKey = key;
        RedisDatabase.StringSet(key, 1);
        return _objectMapper.Map<AdsIndex, AdsResp>(adsList.First());
    }


    public async Task<List<AdsIndex>> QueryAdsList(string label, string adsId, int size)
    {
        var utcMilliSeconds = DateTime.UtcNow.ToUtcMilliSeconds();
        var searchResponse = await _elasticClient.SearchAsync<AdsIndex>(s => s
            .Index("adsindex")
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m =>
                        {
                            if (label.IsNullOrEmpty())
                                return m.MatchAll();
                            return m.Terms(t => t
                                .Field(f => f.Labels.Suffix("keyword"))
                                .Terms(label)
                            );
                        },
                        m => m.Range(r => r
                            .Field(f => f.StartTime)
                            .LessThanOrEquals(utcMilliSeconds)
                        ),
                        m => m.Range(r => r
                            .Field(f => f.EndTime)
                            .GreaterThanOrEquals(utcMilliSeconds)
                        ),
                        m =>
                        {
                            if (adsId.IsNullOrEmpty())
                                return m.MatchAll();
                            return m.Term(t => t
                                .Field(f => f.AdsId)
                                .Value(adsId)
                            );
                        }
                    )
                )
            ).Sort(sort => sort
                .Field(f => f
                    .Field(c => c.CreateTime)
                    .Order(SortOrder.Ascending)
                )
            )
            .Size(size)
        );

        return searchResponse.Documents.ToList();
    }


    public async Task<List<AdsBannerIndex>> QueryAdsBannerList(string label, string adsBannerId, int size)
    {
        var utcMilliSeconds = DateTime.UtcNow.ToUtcMilliSeconds();
        var searchResponse = await _elasticClient.SearchAsync<AdsBannerIndex>(s => s
            .Index("adsbannerindex")
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m =>
                        {
                            if (label.IsNullOrEmpty())
                                return m.MatchAll();
                            return m.Terms(t => t
                                .Field(f => f.Labels.Suffix("keyword"))
                                .Terms(label)
                            );
                        },
                        m => m.Range(r => r
                            .Field(f => f.StartTime)
                            .LessThanOrEquals(utcMilliSeconds)
                        ),
                        m => m.Range(r => r
                            .Field(f => f.EndTime)
                            .GreaterThanOrEquals(utcMilliSeconds)
                        ),
                        m =>
                        {
                            if (adsBannerId.IsNullOrEmpty())
                                return m.MatchAll();
                            return m.Term(t => t
                                .Field(f => f.AdsBannerId)
                                .Value(adsBannerId)
                            );
                        }
                    )
                )
            ).Sort(sort => sort
                .Field(f => f
                    .Field(c => c.CreateTime)
                    .Order(SortOrder.Ascending)
                )
            )
            .Size(size)
        );

        return searchResponse.Documents.ToList();
    }

    public async Task<AdsIndex> UpdateAds(UpdateAdsReq req)
    {
        var adsIndex = _objectMapper.Map<UpdateAdsReq, AdsIndex>(req);
        if (req.AdsId.IsNullOrEmpty())
        {
            var adsId = Guid.NewGuid().ToString();
            adsIndex.AdsId = adsId;
            adsIndex.Id = adsId;
            adsIndex.CreateTime = DateTime.UtcNow;
            adsIndex.UpdateTime = DateTime.UtcNow;
        }
        else
        {
            adsIndex.Id = adsIndex.AdsId;
        }


        adsIndex.UpdateTime = DateTime.UtcNow;
        await _adsRepository.AddOrUpdateAsync(adsIndex);
        return adsIndex;
    }

    public async Task<AdsBannerIndex> UpdateAdsBanner(UpdateAdsBannerReq req)
    {
        var adsBannerIndex = _objectMapper.Map<UpdateAdsBannerReq, AdsBannerIndex>(req);
        if (req.AdsBannerId.IsNullOrEmpty())
        {
            var adsId = Guid.NewGuid().ToString();
            adsBannerIndex.AdsBannerId = adsId;
            adsBannerIndex.Id = adsId;
            adsBannerIndex.CreateTime = DateTime.UtcNow;
            adsBannerIndex.UpdateTime = DateTime.UtcNow;
        }
        else
        {
            adsBannerIndex.Id = adsBannerIndex.AdsBannerId;
        }


        adsBannerIndex.UpdateTime = DateTime.UtcNow;

        await _adsBannerRepository.AddOrUpdateAsync(adsBannerIndex);
        return adsBannerIndex;
    }


    public async Task<AdsIndex> DeleteAds(DeleteAdsReq req)
    {
        var queryableAsync = await _adsRepository.GetQueryableAsync();
        var adsIndices = queryableAsync.Where(c => c.AdsId == req.AdsId).Take(1);

        if (adsIndices.IsNullOrEmpty())
        {
            return new AdsIndex();
        }

        var index = new AdsIndex()
        {
            Id = req.AdsId
        };
        await _adsRepository.DeleteAsync(index);

        return adsIndices.First();
    }

    public async Task<AdsBannerIndex> DeleteAdsBanner(DeleteAdsBannerReq req)
    {
        var queryableAsync = await _adsBannerRepository.GetQueryableAsync();
        var adsIndices = queryableAsync.Where(c => c.AdsBannerId == req.AdsBannerId).Take(1);

        if (adsIndices.IsNullOrEmpty())
        {
            return new AdsBannerIndex();
        }

        var index = new AdsBannerIndex()
        {
            Id = req.AdsBannerId
        };
        await _adsBannerRepository.DeleteAsync(index);

        return adsIndices.First();
    }
}