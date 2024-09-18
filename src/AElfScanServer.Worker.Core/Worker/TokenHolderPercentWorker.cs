using System;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Worker.Core.Options;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class TokenHolderPercentWorker : AsyncPeriodicBackgroundWorkerBase
{
    private const string WorkerKey = "TokenHolderPercentWorker";
    private readonly ILogger<TokenHolderPercentWorker> _logger;
    private readonly ITokenHolderPercentProvider _tokenHolderPercentProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly IElasticClient _elasticClient;

    public TokenHolderPercentWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<TokenHolderPercentWorker> logger, ITokenHolderPercentProvider tokenHolderPercentProvider,
        ITokenIndexerProvider tokenIndexerProvider, IOptionsMonitor<WorkerOptions> workerOptionsMonitor,
        IOptionsMonitor<ElasticsearchOptions> options, IOptionsMonitor<GlobalOptions> globalOptions)
        : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _tokenHolderPercentProvider = tokenHolderPercentProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _workerOptions = workerOptionsMonitor;
        Timer.RunOnStart = true;
        timer.Period = _workerOptions.CurrentValue.GetWorkerPeriodMinutes(WorkerKey) * 60 * 1000;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        _elasticClient = new ElasticClient(settings);
        EsIndex.SetElasticClient(_elasticClient);
        _globalOptions = globalOptions;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var tasks = _workerOptions.CurrentValue.Chains.Select(info => UpdateTokenHolderCount(info.ChainId)).ToList();
        tasks.Add(UpdateMergeTokenHolderCount());
        await Task.WhenAll(tasks);
    }

    private async Task UpdateMergeTokenHolderCount()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        _logger.LogInformation("Update MergeTokenHolderCount date {dateStr}", today);
        var exist = await _tokenHolderPercentProvider.CheckExistAsync("", today);
        if (exist)
        {
            _logger.LogInformation("Update MergeTokenHolderCount date {dateStr} exist.", today);
            return;
        }

        var result =
            await EsIndex.SearchMergeTokenList(0, 10000, "desc", _globalOptions.CurrentValue.SpecialSymbols);


        var dictionary = result.list.ToDictionary(token => token.Symbol, token => token.HolderCount);
        await _tokenHolderPercentProvider.UpdateTokenHolderCount(dictionary, "");
    }

    private async Task UpdateTokenHolderCount(string chainId)
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        _logger.LogInformation("Update TokenHolderCount date {dateStr}", today);
        var exist = await _tokenHolderPercentProvider.CheckExistAsync(chainId, today);
        if (exist)
        {
            _logger.LogInformation("Update TokenHolderCount date {dateStr} exist.", today);
            return;
        }

        var batchSize = 1000;
        var skipCount = 0;
        var moreData = true;
        while (moreData)
        {
            var input = new TokenListInput()
            {
                ChainId = chainId,
                SkipCount = skipCount,
                MaxResultCount = batchSize
            };

            var tokenListDto = await _tokenIndexerProvider.GetTokenListAsync(input);

            if (!tokenListDto.Items.Any() || tokenListDto.Items.Count < batchSize)
            {
                moreData = false;
            }
            else
            {
                skipCount += batchSize;
            }

            var dictionary = tokenListDto.Items.ToDictionary(token => token.Symbol, token => token.HolderCount);
            await _tokenHolderPercentProvider.UpdateTokenHolderCount(dictionary, chainId);
        }
    }
}