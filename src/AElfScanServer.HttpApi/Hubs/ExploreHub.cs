using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AElf.ExceptionHandler;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.DataStrategy;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.DataStrategy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;
using Timer = System.Timers.Timer;

namespace AElfScanServer.HttpApi.Hubs;

public class ExploreHub : AbpHub
{
    private readonly IHomePageService _HomePageService;
    private readonly IBlockChainService _blockChainService;
    private readonly IHubContext<ExploreHub> _hubContext;
    private readonly ILogger<ExploreHub> _logger;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private static Timer _timer = new Timer();
    private readonly DataStrategyContext<string, HomeOverviewResponseDto> _overviewDataStrategy;
    private readonly DataStrategyContext<string, TransactionsResponseDto> _latestTransactionsDataStrategy;
    private readonly DataStrategyContext<string, BlocksResponseDto> _latestBlocksDataStrategy;
    private readonly DataStrategyContext<string, BlockProduceInfoDto> _bpDataStrategy;
    private readonly IChartDataService _chartDataService;
    private readonly IDistributedCache<List<TopTokenDto>> _cache;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IObjectMapper _objectMapper;


    private static readonly ConcurrentDictionary<string, bool>
        _isPushRunning = new ConcurrentDictionary<string, bool>();


    public ExploreHub(IHomePageService homePageService, ILogger<ExploreHub> logger,
        IBlockChainService blockChainService, IHubContext<ExploreHub> hubContext,
        OverviewDataStrategy overviewDataStrategy, LatestTransactionDataStrategy latestTransactionsDataStrategy,
        CurrentBpProduceDataStrategy bpDataStrategy,
        LatestBlocksDataStrategy latestBlocksDataStrategy, IOptionsMonitor<GlobalOptions> globalOptions,
        IChartDataService chartDataService,
        IDistributedCache<List<TopTokenDto>> cache, ITokenIndexerProvider tokenIndexerProvider,
        IObjectMapper objectMapper)
    {
        _HomePageService = homePageService;
        _logger = logger;
        _blockChainService = blockChainService;
        _hubContext = hubContext;
        _overviewDataStrategy = new DataStrategyContext<string, HomeOverviewResponseDto>(overviewDataStrategy);
        _latestTransactionsDataStrategy =
            new DataStrategyContext<string, TransactionsResponseDto>(latestTransactionsDataStrategy);
        _latestBlocksDataStrategy =
            new DataStrategyContext<string, BlocksResponseDto>(latestBlocksDataStrategy);
        _bpDataStrategy = new DataStrategyContext<string, BlockProduceInfoDto>(bpDataStrategy);
        _globalOptions = globalOptions;
        _chartDataService = chartDataService;
        _cache = cache;
        _tokenIndexerProvider = tokenIndexerProvider;
        _objectMapper = objectMapper;
    }


    public async Task RequestBpProduce(CommonRequest request)
    {
        var startNew = Stopwatch.StartNew();
        var resp = await _bpDataStrategy.DisplayData(request.ChainId);

        await Groups.AddToGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetBpProduceGroupName(request.ChainId));
        _logger.LogInformation("RequestBpProduce: {chainId}", request.ChainId);
        await Clients.Caller.SendAsync("ReceiveBpProduce", resp);
        startNew.Stop();
        _logger.LogInformation("RequestBpProduce costTime:{chainId},{costTime}", request.ChainId,
            startNew.Elapsed.TotalSeconds);
        PushRequestBpProduceAsync(request.ChainId);
    }

    public async Task UnsubscribeBpProduce(CommonRequest request)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetBpProduceGroupName(request.ChainId));
    }

    [ExceptionHandler(typeof(Exception),
        Message = "PushRequestBpProduceAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,
        FinallyTargetType = typeof(ExploreHub), FinallyMethodName = nameof(FinallyPushRequestBpProduceAsync))]
    public virtual async Task PushRequestBpProduceAsync(string chainId)
    {
        var key = "bpProduce" + chainId;

        if (!_isPushRunning.TryAdd(key, true))
        {
            return;
        }

        while (true)
        {
            await Task.Delay(2000);
            var startNew = Stopwatch.StartNew();
            var resp = await _bpDataStrategy.DisplayData(chainId);

            await _hubContext.Clients.Groups(HubGroupHelper.GetBpProduceGroupName(chainId))
                .SendAsync("ReceiveBpProduce", resp);
            startNew.Stop();
            _logger.LogInformation("PushRequestBpProduceAsync costTime:{chainId},{costTime}", chainId,
                startNew.Elapsed.TotalSeconds);
        }
    }

    public async Task FinallyPushRequestBpProduceAsync(string chainId)
    {
        var key = "bpProduce" + chainId;
        _isPushRunning.TryRemove(key, out var v);
        _logger.LogInformation($"FinallyPushRequestBpProduceAsync {key}");
    }


    public async Task RequestMergeBlockInfo(MergeBlockInfoReq request)
    {
        if (request.ChainId.IsNullOrEmpty())
        {
            await RequestMergeChainInfo();
        }
        else
        {
            var startNew = Stopwatch.StartNew();
            var transactions = await _latestTransactionsDataStrategy.DisplayData(request.ChainId);
            var blocks = await _latestBlocksDataStrategy.DisplayData(request.ChainId);
            var resp = new WebSocketMergeBlockInfoDto()
            {
                LatestTransactions = transactions,
                LatestBlocks = blocks
            };

            await Groups.AddToGroupAsync(Context.ConnectionId,
                HubGroupHelper.GetMergeBlockInfoGroupName(request.ChainId));
            _logger.LogInformation("RequestMergeBlockInfo: {chainId}", request.ChainId);
            await Clients.Caller.SendAsync("ReceiveMergeBlockInfo", resp);

            startNew.Stop();
            _logger.LogInformation("RequestMergeBlockInfo costTime:{chainId},{costTime}", request.ChainId,
                startNew.Elapsed.TotalSeconds);
        }

        PushMergeBlockInfoAsync(request.ChainId);
    }


    public async Task RequestMergeChainInfo()
    {
        var transactions = await _latestTransactionsDataStrategy.DisplayData("");
        var blocks = await GetLatestBlocks();

        var resp = new WebSocketMergeBlockInfoDto()
        {
            LatestTransactions = transactions,
            LatestBlocks = blocks,
        };

        await Groups.AddToGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetMergeBlockInfoGroupName());
        await Clients.Caller.SendAsync("ReceiveMergeBlockInfo", resp);
    }


    public async Task<BlocksResponseDto> GetLatestBlocks()
    {
        var result = new BlocksResponseDto() { };
        var searchMergeBlockList = EsIndex.SearchMergeBlockList(0, 10);
        result.Blocks = _objectMapper.Map<List<BlockIndex>, List<BlockRespDto>>(searchMergeBlockList.Result.list);
        result.Total = searchMergeBlockList.Result.totalCount;

        return result;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "GetTopTokens err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<List<TopTokenDto>> GetTopTokens()
    {
        var key = "TopTokens";
        var list = await _cache.GetAsync(key);
        if (!list.IsNullOrEmpty())
        {
            return list;
        }

        var searchMergeTokenList =
            await EsIndex.SearchMergeTokenList(0, 6, "desc", null, _globalOptions.CurrentValue.SpecialSymbols);

        var topTokenDtos = new List<TopTokenDto>();
        foreach (var tokenInfoIndex in searchMergeTokenList.list)
        {
            topTokenDtos.Add(new TopTokenDto
            {
                Symbol = tokenInfoIndex.Symbol,
                ChainIds = tokenInfoIndex.ChainIds.OrderByDescending(c => c).ToList(),
                Transfers = tokenInfoIndex.TransferCount,
                Holder = tokenInfoIndex.HolderCount,
                TokenName = tokenInfoIndex.TokenName,
                Type = tokenInfoIndex.Type,
                ImageUrl = await _tokenIndexerProvider.GetTokenImageAsync(tokenInfoIndex.Symbol,
                    tokenInfoIndex.IssueChainId, tokenInfoIndex.ExternalInfo)
            });
        }

        await _cache.SetAsync(key, topTokenDtos, new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
        });
        return topTokenDtos;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "PushMergeBlockInfoAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,
        FinallyTargetType = typeof(ExploreHub), FinallyMethodName = nameof(FinallyPushMergeBlockInfoAsync))]
    public virtual async Task PushMergeBlockInfoAsync(string chainId = "")
    {
        var key = "mergeBlockInfo" + chainId;
        if (!_isPushRunning.TryAdd(key, true))
        {
            _logger.LogInformation($"PushMergeBlockInfoAsync {key}");
            return;
        }

        while (true)
        {
            await Task.Delay(2000);
            if (chainId.IsNullOrEmpty())
            {
                var resp = new WebSocketMergeBlockInfoDto()
                {
                    LatestTransactions = await _latestTransactionsDataStrategy.DisplayData(""),
                    LatestBlocks = await GetLatestBlocks()
                };
                await _hubContext.Clients.Groups(HubGroupHelper.GetMergeBlockInfoGroupName())
                    .SendAsync("ReceiveMergeBlockInfo", resp);
                _logger.LogInformation("push merge PushMergeBlockInfoAsync");
                
            }
            else
            {
                var startNew = Stopwatch.StartNew();
                var transactions = await _latestTransactionsDataStrategy.DisplayData(chainId);
                var blocks = await _latestBlocksDataStrategy.DisplayData(chainId);
                var resp = new WebSocketMergeBlockInfoDto()
                {
                    LatestTransactions = transactions,
                    LatestBlocks = blocks
                };
                await _hubContext.Clients.Groups(HubGroupHelper.GetMergeBlockInfoGroupName(chainId))
                    .SendAsync("ReceiveMergeBlockInfo", resp);
                startNew.Stop();
                _logger.LogInformation("PushMergeBlockInfoAsync costTime:{chainId},{costTime}", chainId,
                    startNew.Elapsed.TotalSeconds);
            }
        }
    }


    public async Task FinallyPushMergeBlockInfoAsync(string chainId)
    {
        var key = "mergeBlockInfo" + chainId;
        _isPushRunning.TryRemove(key, out var v);
        _logger.LogInformation($"FinallyPushMergeBlockInfoAsync {key}");
    }


    public async Task UnsubscribeMergeBlockInfo(CommonRequest request)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetMergeBlockInfoGroupName(request.ChainId));
    }


    public async Task RequestBlockchainOverview(BlockchainOverviewRequestDto request)
    {
        var startNew = Stopwatch.StartNew();

        var resp = await _overviewDataStrategy.DisplayData(request.ChainId);

        await Groups.AddToGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetBlockOverviewGroupName(request.ChainId));
        await Clients.Caller.SendAsync("ReceiveBlockchainOverview", resp);
        PushBlockOverViewAsync(request.ChainId);

        startNew.Stop();
        _logger.LogInformation("RequestBlockchainOverview costTime:{chainId},{costTime}", request.ChainId,
            startNew.Elapsed.TotalSeconds);
    }

    public async Task UnsubscribeBlockchainOverview(CommonRequest request)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetBlockOverviewGroupName(request.ChainId));
    }


    [ExceptionHandler(typeof(Exception),
        Message = "PushBlockOverViewAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,
        FinallyTargetType = typeof(ExploreHub), FinallyMethodName = nameof(FinallyPushBlockOverViewAsync))]
    public virtual async Task PushBlockOverViewAsync(string chainId)
    {
        var key = "overview" + chainId;
        if (!_isPushRunning.TryAdd(key, true))
        {
            return;
        }


        while (true)
        {
            await Task.Delay(2000);
            var startNew = Stopwatch.StartNew();
            var resp = await _overviewDataStrategy.DisplayData(chainId);
            await _hubContext.Clients.Groups(HubGroupHelper.GetBlockOverviewGroupName(chainId))
                .SendAsync("ReceiveBlockchainOverview", resp);

            startNew.Stop();
            _logger.LogInformation("PushBlockOverViewAsync costTime:{chainId},{costTime}", chainId,
                startNew.Elapsed.TotalSeconds);
        }
    }


    public async Task FinallyPushBlockOverViewAsync(string chainId)
    {
        var key = "overview" + chainId;
        _isPushRunning.TryRemove(key, out var v);
        _logger.LogInformation($"FinallyPushBlockOverViewAsync {key}");
    }

    public async Task RequestTransactionDataChart(GetTransactionPerMinuteRequestDto request)
    {
        if (request.ChainId.IsNullOrEmpty())
        {
            await RequestMergeTransactionDataChart();
        }
        else
        {
            var startNew = Stopwatch.StartNew();
            var resp = await _HomePageService.GetTransactionPerMinuteAsync(request.ChainId);

            resp.All = resp.All.Take(resp.All.Count - 3).ToList();
            resp.Owner = resp.Owner.Take(resp.Owner.Count - 3).ToList();
            await Groups.AddToGroupAsync(Context.ConnectionId,
                HubGroupHelper.GetTransactionCountPerMinuteGroupName(request.ChainId));

            _logger.LogInformation("RequestTransactionDataChart: {chainId}", request.ChainId);
            await Clients.Caller.SendAsync("ReceiveTransactionDataChart", resp);
            startNew.Stop();
            _logger.LogInformation("RequestTransactionDataChart costTime:{chainId},{costTime}", request.ChainId,
                startNew.Elapsed.TotalSeconds);
        }

        PushTransactionCountPerMinuteAsync(request.ChainId);
    }

    public async Task RequestMergeTransactionDataChart()
    {
        var mainChainData = await _HomePageService.GetTransactionPerMinuteAsync("AELF");
        var sideChainData =
            await _HomePageService.GetTransactionPerMinuteAsync(_globalOptions.CurrentValue.SideChainId);

        mainChainData.All = mainChainData.All.Take(mainChainData.All.Count - 3).ToList();
        mainChainData.MainChain = mainChainData.Owner.Take(mainChainData.Owner.Count - 3).ToList();
        mainChainData.SideChain = sideChainData.Owner.Take(sideChainData.Owner.Count - 3).ToList();
        await Groups.AddToGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetTransactionCountPerMinuteGroupName());

        await Clients.Caller.SendAsync("ReceiveTransactionDataChart", mainChainData);
    }


    public async Task UnsubscribeTransactionDataChart(CommonRequest request)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,
            HubGroupHelper.GetTransactionCountPerMinuteGroupName(request.ChainId));
    }

    [ExceptionHandler(typeof(Exception),
        Message = "PushTransactionCountPerMinuteAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,
        FinallyTargetType = typeof(ExploreHub), FinallyMethodName = nameof(FinallyPushTransactionCountPerMinuteAsync))]
    public virtual async Task PushTransactionCountPerMinuteAsync(string chainId = "")
    {
        var key = "transactionCountPerMinute" + chainId;
        if (!_isPushRunning.TryAdd(key, true))
        {
            return;
        }


        while (true)
        {
            if (chainId.IsNullOrEmpty())
            {
                await Task.Delay(60 * 1000);
                await RequestMergeTransactionDataChart();
            }
            else
            {
                await Task.Delay(60 * 1000);
                var startNew = Stopwatch.StartNew();
                var resp = await _HomePageService.GetTransactionPerMinuteAsync(chainId);
                resp.All = resp.All.Take(resp.All.Count - 3).ToList();
                resp.Owner = resp.Owner.Take(resp.Owner.Count - 3).ToList();
                await _hubContext.Clients.Groups(HubGroupHelper.GetTransactionCountPerMinuteGroupName(chainId))
                    .SendAsync("ReceiveTransactionDataChart", resp);
                startNew.Stop();
                _logger.LogInformation("PushTransactionCountPerMinuteAsync costTime:{chainId},{costTime}", chainId,
                    startNew.Elapsed.TotalSeconds);
            }
        }
    }


    public async Task FinallyPushTransactionCountPerMinuteAsync(string chainId)
    {
        var key = "transactionCountPerMinute" + chainId;
        _isPushRunning.TryRemove(key, out var v);
        _logger.LogInformation($"FinallyPushTransactionCountPerMinuteAsync {key}");
    }
}