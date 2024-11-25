using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Service;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Elasticsearch.Net;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;
using Field = Google.Protobuf.WellKnownTypes.Field;

namespace AElfScanServer.HttpApi.Service;

public interface IHomePageService
{
    public Task<TransactionPerMinuteResponseDto> GetTransactionPerMinuteAsync(
        string chainId);


    public Task<FilterTypeResponseDto> GetFilterType();
}

[Ump]
public class HomePageService :  IHomePageService, ITransientDependency
{
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;

    private readonly ILogger<HomePageService> _logger;

    private readonly ICacheProvider _cacheProvider;


    public HomePageService(
        ILogger<HomePageService> logger, IOptionsMonitor<GlobalOptions> globalOptions,
        ICacheProvider cacheProvider
    ) 
    {
        _logger = logger;
        _globalOptions = globalOptions;
        _cacheProvider = cacheProvider;
    }

    public async Task<TransactionPerMinuteResponseDto> GetTransactionPerMinuteAsync(
        string chainId)
    {
        var transactionPerMinuteResp = new TransactionPerMinuteResponseDto();
        var key = RedisKeyHelper.TransactionChartData(chainId);

        var dataValue = await _cacheProvider.StringGetAsync(key);

        var data =
            JsonConvert.DeserializeObject<List<TransactionCountPerMinuteDto>>(dataValue);

        transactionPerMinuteResp.Owner = data;

        var redisValue = await _cacheProvider.StringGetAsync(RedisKeyHelper.TransactionChartData("merge"));
        var mergeData =
            JsonConvert.DeserializeObject<List<TransactionCountPerMinuteDto>>(redisValue);

        transactionPerMinuteResp.All = mergeData;


        return transactionPerMinuteResp;
    }


    public async Task<FilterTypeResponseDto> GetFilterType()
    {
        var filterTypeResp = new FilterTypeResponseDto();

        filterTypeResp.FilterTypes = new List<FilterTypeDto>();
        foreach (var keyValuePair in _globalOptions.CurrentValue.FilterTypes)
        {
            var filterTypeDto = new FilterTypeDto();
            filterTypeDto.FilterType = keyValuePair.Value;
            filterTypeDto.FilterInfo = keyValuePair.Key;
            filterTypeResp.FilterTypes.Add(filterTypeDto);
        }

        filterTypeResp.FilterTypes = filterTypeResp.FilterTypes.OrderBy(o => o.FilterType).ToList();

        return filterTypeResp;
    }
}