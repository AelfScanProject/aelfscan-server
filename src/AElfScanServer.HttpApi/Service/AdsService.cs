using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.EntityMapping.Options;
using AElf.EntityMapping.Repositories;
using AElfScanServer.Common.Commons;
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
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.HttpApi.Service;

public interface IAdsService
{
    public Task<AdsResp> GetAds(AdsReq req);

    public Task<List<AdsResp>> GetAdsDetailList(int size);

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

public class AdsService : IAdsService, ITransientDependency
{
    private readonly IOptionsMonitor<SecretOptions> _secretOptions;
    private readonly IOptionsMonitor<TwitterOptions> _twitterOptions;
    private readonly ILogger<AdsService> _logger;
    private readonly IEntityMappingRepository<AdsIndex, string> _adsRepository;
    private readonly IEntityMappingRepository<AdsBannerIndex, string> _adsBannerRepository;
    private readonly IEntityMappingRepository<TwitterIndex, string> _twitterRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IElasticClient _elasticClient;
    private readonly ITwitterProvider _twitterProvider;
    private readonly IOptionsMonitor<AElfEntityMappingOptions> _mappingOptions;
    private readonly ICacheProvider _cacheProvider;

    public AdsService( IOptionsMonitor<SecretOptions> secretOptions,
        ILogger<AdsService> logger,
         IEntityMappingRepository<AdsIndex, string> adsRepository,
        
        IObjectMapper objectMapper, IOptionsMonitor<ElasticsearchOptions> options,
        IEntityMappingRepository<AdsBannerIndex, string> adsBannerRepository,
        IEntityMappingRepository<TwitterIndex, string> twitterRepository,
        IOptionsMonitor<TwitterOptions> twitterOptions,IOptionsMonitor<AElfEntityMappingOptions> mappingOptions,
        ITwitterProvider twitterProvider,ICacheProvider cacheProvider) 
    {
        _secretOptions = secretOptions;
        _logger = logger;
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
        _mappingOptions = mappingOptions;
        _cacheProvider = cacheProvider;
    }


    public async Task<AdsBannerListResp> GetAdsBannerList(GetAdsBannerListReq req)
    {
        var result = new AdsBannerListResp();
        var searchResponse = await _elasticClient.SearchAsync<AdsBannerIndex>(s => s
            .Index(CommonIndexUtil.GetIndexName(_mappingOptions.CurrentValue.CollectionPrefix,"adsbannerindex"))
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

    public async Task<List<AdsResp>> GetAdsDetailList(int size)
    {
        var adsIndices = await QueryAdsList(size);

        var adsResps = _objectMapper.Map<List<AdsIndex>, List<AdsResp>>(adsIndices);

        return adsResps;
    }

    public async Task<AdsListResp> GetAdsList(GetAdsListReq req)
    {
        var result = new AdsListResp();
        var searchResponse = await _elasticClient.SearchAsync<AdsIndex>(s => s
            .Index(CommonIndexUtil.GetIndexName(_mappingOptions.CurrentValue.CollectionPrefix,"adsindex"))
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
       
        var adsBannerVisitCount = await _cacheProvider.StringGetAsync(key);
        var adsBannerList = new List<AdsBannerIndex>();
        var adsBannerResp = new AdsBannerResp();
        adsBannerResp.SearchKey = key;
        adsBannerList = await QueryAdsBannerList(req.Label, "", 1000);
        if (adsBannerList.IsNullOrEmpty())
        {
            return adsBannerResp;
        }

        if (adsBannerVisitCount.IsNullOrEmpty())
        {
            var adsBannerIndex = adsBannerList.First();
            adsBannerResp = _objectMapper.Map<AdsBannerIndex, AdsBannerResp>(adsBannerIndex);

            await _cacheProvider.StringIncrement(key, 1, TimeSpan.FromDays(7));
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
                await _cacheProvider.StringIncrement(key, 1, TimeSpan.FromDays(7));
                var resp = _objectMapper.Map<AdsBannerIndex, AdsBannerResp>(ads);
                resp.SearchKey = key;
                return resp;
            }
        }

        adsBannerResp.SearchKey = key;
        await _cacheProvider.StringSetAsync(key, 1,null);
        return _objectMapper.Map<AdsBannerIndex, AdsBannerResp>(adsBannerList.First());
    }


    public async Task<AdsResp> GetAds(AdsReq req)
    {
        var key = GrainIdHelper.GenerateAdsKey(req.SearchKey, req.Label);
        var adsVisitCount = await _cacheProvider.StringGetAsync(key);
        var adsList = new List<AdsIndex>();
        var adsResp = new AdsResp();

        adsList = await QueryAdsList(req.Label, "", 1000);
        if (adsList.IsNullOrEmpty())
        {
            adsResp.SearchKey = key;
            return adsResp;
        }

        if (adsVisitCount.IsNullOrEmpty())
        {
            var adsIndex = adsList.First();
            adsResp = _objectMapper.Map<AdsIndex, AdsResp>(adsIndex);

            await _cacheProvider.StringIncrement(key, 1, TimeSpan.FromDays(7));
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
                await _cacheProvider.StringIncrement(key, 1, TimeSpan.FromDays(7));
                var resp = _objectMapper.Map<AdsIndex, AdsResp>(ads);
                resp.SearchKey = key;
                return resp;
            }
        }

        adsResp.SearchKey = key;
        await _cacheProvider.StringSetAsync(key, 1,null);
        return _objectMapper.Map<AdsIndex, AdsResp>(adsList.First());
    }


    public async Task<List<AdsIndex>> QueryAdsList(int size)
    {
        var utcMilliSeconds = DateTime.UtcNow.ToUtcMilliSeconds();
        var searchResponse = await _elasticClient.SearchAsync<AdsIndex>(s => s
            .Index(CommonIndexUtil.GetIndexName(_mappingOptions.CurrentValue.CollectionPrefix,"adsindex"))
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Range(r => r
                            .Field(f => f.StartTime)
                            .LessThanOrEquals(utcMilliSeconds)
                        ),
                        m => m.Range(r => r
                            .Field(f => f.EndTime)
                            .GreaterThanOrEquals(utcMilliSeconds)
                        )
                    )
                )
            )
            .Sort(sort => sort
                .Field(f => f
                    .Field(c => c.StartTime)
                    .Order(SortOrder.Ascending)
                )
            )
            .Size(size)
        );

        return searchResponse.Documents.ToList();
    }


    public async Task<List<AdsIndex>> QueryAdsList(string label, string adsId, int size)
    {
        var utcMilliSeconds = DateTime.UtcNow.ToUtcMilliSeconds();
        var searchResponse = await _elasticClient.SearchAsync<AdsIndex>(s => s
            .Index(CommonIndexUtil.GetIndexName(_mappingOptions.CurrentValue.CollectionPrefix,"adsindex"))
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
            .Index(CommonIndexUtil.GetIndexName(_mappingOptions.CurrentValue.CollectionPrefix,"adsbannerindex"))
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