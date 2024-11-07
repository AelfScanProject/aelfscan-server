using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.Common.Options;
using Elasticsearch.Net;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public class LogEventProvider : AbpRedisCache, ISingletonDependency
{
    private readonly INESTRepository<LogEventIndex, string> _logEventIndexRepository;
    private readonly GlobalOptions _globalOptions;
    private readonly IElasticClient _elasticClient;


    private readonly ILogger<HomePageProvider> _logger;

    public LogEventProvider(
        ILogger<HomePageProvider> logger, IOptionsMonitor<GlobalOptions> blockChainOptions,
        IOptions<ElasticsearchOptions> options,
        INESTRepository<LogEventIndex, string> logEventIndexRepository,
        IOptions<RedisCacheOptions> optionsAccessor) : base(optionsAccessor)
    {
        _logger = logger;
        _globalOptions = blockChainOptions.CurrentValue;
        var uris = options.Value.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool);
        _elasticClient = new ElasticClient(settings);
        _logEventIndexRepository = logEventIndexRepository;
    }

    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetLogEventListAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["request"])]
    public virtual async Task<LogEventResponseDto> GetLogEventListAsync(GetLogEventRequestDto request)
    {
        var result = new LogEventResponseDto();
        
            var mustQuery = new List<Func<QueryContainerDescriptor<LogEventIndex>, QueryContainer>>();
            
            mustQuery.Add(mu => mu.Term(t => t.Field(f => f.ContractAddress).Value(request.ContractAddress)));

            QueryContainer Filter(QueryContainerDescriptor<LogEventIndex> f) => f.Bool(b => b.Must(mustQuery));


            // var resp = await _logEventIndexRepository.GetListAsync(Filter, skip: request.SkipCount,
            //     limit: request.MaxResultCount,
            //     index: BlockChainIndexNameHelper.GenerateLogEventIndexName(request.ChainId));


            var resp = await _logEventIndexRepository.GetSortListAsync(Filter, skip: request.SkipCount,
                limit: request.MaxResultCount,
                index: BlockChainIndexNameHelper.GenerateLogEventIndexName(request.ChainId),
                sortFunc: GetQuerySortDescriptor(request.SortOrder));

            result.Total = resp.Item1;
            result.LogEvents = resp.Item2;
    

        return result;
    }

    private static Func<SortDescriptor<LogEventIndex>, IPromise<IList<ISort>>> GetQuerySortDescriptor(SortOrder sort)
    {
        //use default
        var sortDescriptor = new SortDescriptor<LogEventIndex>();

        if (sort == SortOrder.Ascending)
        {
            sortDescriptor.Ascending(a => a.BlockHeight);
        }
        else
        {
            sortDescriptor.Descending(a => a.BlockHeight);
        }

        sortDescriptor.Ascending(a => a.Index);

        return _ => sortDescriptor;
    }
}