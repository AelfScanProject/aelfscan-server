using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using Elasticsearch.Net;
using AElfScanServer.Common.Helper;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.ExceptionHandler;
using AElf.OpenTelemetry;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.ExceptionHandling;
using Castle.Components.DictionaryAdapter.Xml;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.DataStrategy;
using AElfScanServer.HttpApi.DataStrategy;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using StackExchange.Redis;
using Volo.Abp.Caching.StackExchangeRedis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Nito.AsyncEx;
using Volo.Abp;
using Volo.Abp.Caching;

namespace AElfScanServer.HttpApi.Service;

public interface IBlockChainService

{
    public Task<TransactionsResponseDto> GetTransactionsAsync(TransactionsRequestDto requestD);


    public Task<BlocksResponseDto> GetBlocksAsync(BlocksRequestDto requestDto);

    public Task<BlockDetailResponseDto> GetBlockDetailAsync(BlockDetailRequestDto requestDto);


    public Task<TransactionDetailResponseDto> GetTransactionDetailAsync(TransactionDetailRequestDto request);

    public Task<LogEventResponseDto> GetLogEventsAsync(GetLogEventRequestDto request);
}

[AggregateExecutionTime]
public class BlockChainService : IBlockChainService, ITransientDependency
{
    private readonly INESTRepository<BlockExtraIndex, string> _blockExtraIndexRepository;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly IBlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly BlockChainDataProvider _blockChainProvider;
    private readonly LogEventProvider _logEventProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptionsMonitor;
    private readonly DataStrategyContext<string, HomeOverviewResponseDto> _overviewDataStrategy;
    private IDistributedCache<TransactionDetailResponseDto> _transactionDetailCache;
    private readonly ILogger<HomePageService> _logger;
    private readonly IElasticClient _elasticClient;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly IObjectMapper _objectMapper;
    public const long TimeToReduceMiningRewardByHalf = 126144000; // 60 * 60 * 24 * 365 * 4
    public const long InitialMiningRewardPerBlock = 12500000;

    public BlockChainService(
        ILogger<HomePageService> logger, IOptionsMonitor<GlobalOptions> blockChainOptions,
        AELFIndexerProvider aelfIndexerProvider,
        INESTRepository<BlockExtraIndex, string> blockExtraIndexRepository,
        LogEventProvider logEventProvider, IObjectMapper objectMapper,
        BlockChainDataProvider blockChainProvider, IBlockChainIndexerProvider blockChainIndexerProvider,
        ITokenIndexerProvider tokenIndexerProvider, IOptionsMonitor<TokenInfoOptions> tokenInfoOptions,
        OverviewDataStrategy overviewDataStrategy,
        IDistributedCache<TransactionDetailResponseDto> transactionDetailCache, ITokenInfoProvider tokenInfoProvider,
        IOptionsMonitor<ElasticsearchOptions> options)
    {
        _logger = logger;
        _globalOptions = blockChainOptions;
        _aelfIndexerProvider = aelfIndexerProvider;
        _blockExtraIndexRepository = blockExtraIndexRepository;
        _logEventProvider = logEventProvider;
        _blockChainProvider = blockChainProvider;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenInfoOptionsMonitor = tokenInfoOptions;
        _overviewDataStrategy = new DataStrategyContext<string, HomeOverviewResponseDto>(overviewDataStrategy);
        _transactionDetailCache = transactionDetailCache;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        _elasticClient = new ElasticClient(settings);
        EsIndex.SetElasticClient(_elasticClient);
        _objectMapper = objectMapper;
    }


    [ExceptionHandler(typeof(Exception),
        Message = "GetTransactionDetailAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["request"])]
    public virtual async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(TransactionDetailRequestDto request)
    {
        var transactionDetailResponseDto = new TransactionDetailResponseDto();
        if (!_globalOptions.CurrentValue.ChainIds.Exists(s => s == request.ChainId))
        {
            return transactionDetailResponseDto;
        }

      
            var detailResponseDto = await _transactionDetailCache.GetAsync(request.TransactionId);
            if (detailResponseDto != null)
            {
                return detailResponseDto;
            }

            var blockHeight = 0l;
            NodeTransactionDto transactionDto = new NodeTransactionDto();

            var tasks = new List<Task>();


            tasks.Add(_overviewDataStrategy.DisplayData(request.ChainId).ContinueWith(task =>
            {
                blockHeight = task.Result.BlockHeight;
            }));


            tasks.Add(_blockChainProvider.GetTransactionDetailAsync(request.ChainId,
                request.TransactionId).ContinueWith(task => { transactionDto = task.Result; }));

            await tasks.WhenAll();
            var transactionIndex = _aelfIndexerProvider.GetTransactionsAsync(request.ChainId,
                transactionDto.BlockNumber,
                transactionDto.BlockNumber, request.TransactionId).Result.First();


            var detailDto = new TransactionDetailDto();
            detailDto.TransactionId = transactionIndex.TransactionId;
            detailDto.Status = transactionIndex.Status;
            detailDto.BlockConfirmations = detailDto.Status == TransactionStatus.Mined ? blockHeight : 0;
            detailDto.BlockHeight = transactionIndex.BlockHeight;
            detailDto.Timestamp = DateTimeHelper.GetTotalSeconds(transactionIndex.BlockTime);
            detailDto.Method = transactionIndex.MethodName;
            detailDto.TransactionParams = transactionDto.Transaction.Params;
            detailDto.TransactionSignature = transactionIndex.Signature;
            detailDto.Confirmed = transactionIndex.Confirmed;
            detailDto.From = ConvertAddress(transactionIndex.From, transactionIndex.ChainId);
            detailDto.To = ConvertAddress(transactionIndex.To, transactionIndex.ChainId);


            await AnalysisExtraPropertiesAsync(detailDto, transactionIndex);
            await AnalysisTransferredAsync(detailDto, transactionIndex);
            await AnalysisLogEventAsync(detailDto, transactionIndex);
            var result = new TransactionDetailResponseDto()
            {
                List = new List<TransactionDetailDto>() { detailDto }
            };
            await _transactionDetailCache.SetAsync(request.TransactionId, result);
            return result;
      
    }


    public async Task<List<long>> ParseTokenImageBlockHeight()
    {
        string[] lines = File.ReadAllLines("result.txt");

        // 解析每一行为long类型，并存储到数组中
        long[] numbers = new long[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            if (long.TryParse(lines[i], out long number))
            {
                numbers[i] = number;
            }
            else
            {
                Console.WriteLine($"Error parsing line {i + 1}: {lines[i]}");
            }
        }

        var result = new List<long>() { numbers[0] };

        for (var i = 1; i < numbers.Length; i++)
        {
            if (numbers[i] - result[result.Count - 1] > 100)
            {
                result.Add(numbers[i]);
            }
        }

        return numbers.ToList();
    }

    public async Task<BlockDetailResponseDto> GetBlockDetailAsync(BlockDetailRequestDto requestDto)
    {
        var blockResponseDto = new BlockDetailResponseDto();


        List<Task> blockDetailsTasks = new List<Task>();


        var blockDto = new BlockDetailDto();

        var transactionList = new List<TransactionIndex>();

        var blockList = new List<IndexerBlockDto>();
        Dictionary<long, long> blockBurntFee = new Dictionary<long, long>();

        var blockDtoTask =
            _blockChainProvider.GetBlockDetailAsync(requestDto.ChainId, requestDto.BlockHeight).ContinueWith(task =>
            {
                blockDto = task.Result;
            });


        var transactionListTask =
            _aelfIndexerProvider.GetTransactionsAsync(requestDto.ChainId, requestDto.BlockHeight,
                requestDto.BlockHeight, "").ContinueWith(task => { transactionList = task.Result; });

        var blockListTask = _aelfIndexerProvider.GetLatestBlocksAsync(requestDto.ChainId, requestDto.BlockHeight - 1,
            requestDto.BlockHeight + 1).ContinueWith(task => { blockList = task.Result; });


        var blockBurntFeeTask = ParseBlockBurntAsync(requestDto.ChainId,
            requestDto.BlockHeight,
            requestDto.BlockHeight).ContinueWith(task => { blockBurntFee = task.Result; });

        blockDetailsTasks.Add(blockDtoTask);
        blockDetailsTasks.Add(transactionListTask);
        blockDetailsTasks.Add(blockListTask);
        blockDetailsTasks.Add(blockBurntFeeTask);
        await blockDetailsTasks.WhenAll();


        blockResponseDto.BlockHeight = requestDto.BlockHeight;
        blockResponseDto.ChainId = requestDto.ChainId;
        var fee = await _blockChainProvider.GetBlockRewardAsync(requestDto.BlockHeight, requestDto.ChainId);

        if (blockBurntFee.TryGetValue(requestDto.BlockHeight, out var burnt))
        {
            blockResponseDto.BurntFee = new BurntFee()
            {
                ElfFee = burnt.ToString(),
                UsdFee =
                    (double.Parse(await _blockChainProvider.GetTokenUsdPriceAsync("ELF")) * burnt / 1e8)
                    .ToString()
            };
        }


        var elfReward = requestDto.ChainId == "AELF"
            ? CommomHelper
                .GetMiningRewardPerBlock(_globalOptions.CurrentValue.BlockchainStartTimestamp)
            : 0;
        blockResponseDto.Reward = new RewardDto()
        {
            ElfReward = elfReward.ToString(),
            UsdReward = (double.Parse(await _blockChainProvider.GetTokenUsdPriceAsync("ELF")) * elfReward / 1e8)
                .ToString()
        };

        var midBlockDto = blockList.Where(b => b.BlockHeight == requestDto.BlockHeight).First();

        blockResponseDto.Confirmed = midBlockDto.Confirmed;
        blockResponseDto.Timestamp = DateTimeHelper.GetTotalSeconds(midBlockDto.BlockTime);
        blockResponseDto.BlockHash = midBlockDto.BlockHash;
        blockResponseDto.Producer = new Producer()
        {
            address = midBlockDto.Miner,
            name = await GetBpNameAsync(midBlockDto.ChainId, midBlockDto.Miner)
        };


        blockResponseDto.PreviousBlockHash = blockDto.Header.PreviousBlockHash;
        blockResponseDto.BlockSize = blockDto.BlockSize.ToString();

        blockResponseDto.PreviousBlockHash = blockDto.Header.PreviousBlockHash;

        blockResponseDto.PreviousBlockHash = blockDto.Header.PreviousBlockHash;
        blockResponseDto.MerkleTreeRootOfTransactions = blockDto.Header.MerkleTreeRootOfTransactions;
        blockResponseDto.MerkleTreeRootOfWorldState = blockDto.Header.MerkleTreeRootOfWorldState;
        blockResponseDto.MerkleTreeRootOfTransactionState = blockDto.Header.MerkleTreeRootOfTransactionState;
        blockResponseDto.Extra = blockDto.Header.Extra;
        blockResponseDto.Transactions = new List<TransactionResponseDto>();
        blockResponseDto.PreBlockHeight = blockList.Exists(b => b.BlockHeight == requestDto.BlockHeight - 1)
            ? requestDto.BlockHeight - 1
            : 0;

        blockResponseDto.NextBlockHeight = blockList.Exists(b => b.BlockHeight == requestDto.BlockHeight + 1)
            ? requestDto.BlockHeight + 1
            : 0;
        foreach (var transactionIndex in transactionList)
        {
            var transactionRespDto = new TransactionResponseDto()
            {
                TransactionId = transactionIndex.TransactionId,
                Timestamp = DateTimeHelper.GetTotalSeconds(transactionIndex.BlockTime),
                BlockHeight = transactionIndex.BlockHeight,
                Method = transactionIndex.MethodName,
                Status = transactionIndex.Status
            };

            transactionRespDto.From = ConvertAddress(transactionIndex.From, transactionIndex.ChainId);

            transactionRespDto.To = ConvertAddress(transactionIndex.To, transactionIndex.ChainId);

            var value = await ParseIndexerTransactionValueInfoAsync(transactionIndex);
            transactionRespDto.TransactionValue = value.Item1;

            transactionRespDto.TransactionFee = value.Item2;
            blockResponseDto.Transactions.Add(transactionRespDto);
        }

        blockResponseDto.Total = blockResponseDto.Transactions.Count;


        return blockResponseDto;
    }


    public async Task<TransactionDetailDto> AnalysisTransaction(TransactionIndex transactionIndex, long blockHeight)
    {
        var detailDto = new TransactionDetailDto();

        var transactionDetailAsync =
            await _blockChainProvider.GetTransactionDetailAsync(transactionIndex.ChainId,
                transactionIndex.TransactionId);


        ;
        detailDto.TransactionId = transactionIndex.TransactionId;
        detailDto.Status = transactionIndex.Status;
        detailDto.BlockConfirmations = detailDto.Status == TransactionStatus.Mined ? blockHeight : 0;
        detailDto.BlockHeight = transactionIndex.BlockHeight;
        detailDto.Timestamp = DateTimeHelper.GetTotalSeconds(transactionIndex.BlockTime);
        detailDto.Method = transactionIndex.MethodName;
        detailDto.TransactionParams = transactionDetailAsync.Transaction.Params;
        detailDto.TransactionSignature = transactionIndex.Signature;
        detailDto.Confirmed = transactionIndex.Confirmed;
        detailDto.From = ConvertAddress(transactionIndex.From, transactionIndex.ChainId);
        detailDto.To = ConvertAddress(transactionIndex.To, transactionIndex.ChainId);


        await AnalysisExtraPropertiesAsync(detailDto, transactionIndex);
        await AnalysisTransferredAsync(detailDto, transactionIndex);
        await AnalysisLogEventAsync(detailDto, transactionIndex);

        return detailDto;
    }


    public async Task AnalysisLogEventAsync(TransactionDetailDto detailDto, TransactionIndex transactionIndex)
    {
        foreach (var transactionDtoLogEvent in transactionIndex.LogEvents)
        {
            transactionDtoLogEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
            transactionDtoLogEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);
            var logEventInfoDto = new LogEventInfoDto()
            {
                Indexed = indexed,
                NonIndexed = nonIndexed,
                EventName = transactionDtoLogEvent.EventName,
                ContractInfo = ConvertAddress(transactionDtoLogEvent.ContractAddress, transactionIndex.ChainId)
            };
            detailDto.LogEvents.Add(logEventInfoDto);
            //add parse log event logic
            if (!indexed.IsNullOrEmpty() &&
                (_globalOptions.CurrentValue.ParseLogEvent(detailDto.From.Address, detailDto.Method)
                 || _globalOptions.CurrentValue.ParseLogEvent(detailDto.To.Address, detailDto.Method)))
            {
                var message = ParseMessage(transactionDtoLogEvent.EventName, ByteString.FromBase64(indexed));
                detailDto.AddParseLogEvents(message);
            }
        }
    }

    public async Task AnalysisExtraPropertiesAsync(TransactionDetailDto detailDto, TransactionIndex transactionIndex)
    {
        if (!transactionIndex.ExtraProperties.IsNullOrEmpty())
        {
            if (transactionIndex.ExtraProperties.TryGetValue("Version", out var version))
            {
                detailDto.Version = version;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("RefBlockNumber", out var refBlockNumber))
            {
                detailDto.TransactionRefBlockNumber = refBlockNumber;
            }

            if (transactionIndex.ExtraProperties.TryGetValue("RefBlockPrefix", out var refBlockPrefix))
            {
                detailDto.TransactionRefBlockPrefix = refBlockPrefix;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("Bloom", out var bloom))
            {
                detailDto.Bloom = bloom;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("ReturnValue", out var returnValue))
            {
                detailDto.ReturnValue = returnValue;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("Error", out var error))
            {
                detailDto.Error = error;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("TransactionSize", out var transactionSize))
            {
                detailDto.TransactionSize = transactionSize;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("ResourceFee", out var resourceFee))
            {
                detailDto.ResourceFee = resourceFee;
            }
        }
    }


    public async Task AnalysisTransferredAsync(TransactionDetailDto detailDto,
        TransactionIndex transactionIndex)
    {
        var transactionValues = new Dictionary<string, ValueInfoDto>();

        var transactionFees = new Dictionary<string, ValueInfoDto>();


        var burntFees = new Dictionary<string, ValueInfoDto>();
        var chainId = transactionIndex.ChainId;

        foreach (var txnLogEvent in transactionIndex.LogEvents)
        {
            txnLogEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
            txnLogEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);

            var indexedList = indexed != null
                ? JsonConvert.DeserializeObject<List<string>>(indexed)
                : new List<string>();
            var logEvent = new LogEvent
            {
                Indexed = { indexedList?.Select(ByteString.FromBase64) },
            };

            if (nonIndexed != null)
            {
                logEvent.NonIndexed = ByteString.FromBase64(nonIndexed);
            }


            switch (txnLogEvent.EventName)
            {
                case nameof(Transferred):
                    var transferred = new Transferred();
                    transferred.MergeFrom(logEvent);


                    await SetValueInfoAsync(transactionValues, transferred.Symbol, transferred.Amount);


                    if (TokenSymbolHelper.GetSymbolType(transferred.Symbol) == SymbolType.Token)
                    {
                        _globalOptions.CurrentValue.TokenImageUrls.TryGetValue(transferred.Symbol, out var imageUrl);
                        var token = new TokenTransferredDto()
                        {
                            Symbol = transferred.Symbol,
                            Name = transferred.Symbol,
                            Amount = transferred.Amount,
                            AmountString =
                                await _blockChainProvider.GetDecimalAmountAsync(transferred.Symbol, transferred.Amount,
                                    transactionIndex.ChainId),
                            From = ConvertAddress(transferred.From.ToBase58(), transactionIndex.ChainId),
                            To = ConvertAddress(transferred.To.ToBase58(), transactionIndex.ChainId),
                            ImageUrl = await _tokenIndexerProvider.GetTokenImageAsync(transferred.Symbol,
                                txnLogEvent.ChainId),
                            NowPrice = await _blockChainProvider.TransformTokenToUsdValueAsync(transferred.Symbol,
                                transferred.Amount,chainId)
                        };


                        detailDto.TokenTransferreds.Add(token);
                    }
                    else
                    {
                        var nft = new NftsTransferredDto()
                        {
                            Symbol = transferred.Symbol,
                            Amount = transferred.Amount,
                            AmountString = await _blockChainProvider.GetDecimalAmountAsync(transferred.Symbol,
                                transferred.Amount, transactionIndex.ChainId),
                            Name = transferred.Symbol,
                            From = ConvertAddress(transferred.From.ToBase58(), transactionIndex.ChainId),
                            To = ConvertAddress(transferred.To.ToBase58(), transactionIndex.ChainId),
                            IsCollection = TokenSymbolHelper.IsCollection(transferred.Symbol),
                        };
                        if (_tokenInfoOptionsMonitor.CurrentValue.TokenInfos.TryGetValue(
                                TokenSymbolHelper.GetCollectionSymbol(transferred.Symbol), out var info))
                        {
                            nft.ImageUrl = info.ImageUrl;
                        }

                        detailDto.NftsTransferreds.Add(nft);
                    }

                    break;
                case nameof(TransactionFeeCharged):
                    var transactionFeeCharged = new TransactionFeeCharged();
                    transactionFeeCharged.MergeFrom(logEvent);
                    await SetValueInfoAsync(transactionFees, transactionFeeCharged.Symbol,
                        transactionFeeCharged.Amount);

                    break;
                case nameof(Burned):
                    var burned = new Burned();
                    burned.MergeFrom(logEvent);
                    if (TokenSymbolHelper.GetSymbolType(burned.Symbol) == SymbolType.Nft &&
                        "SEED-0".Equals(TokenSymbolHelper.GetCollectionSymbol(burned.Symbol)))
                    {
                        break;
                    }

                    if (_globalOptions.CurrentValue.BurntFeeContractAddresses.TryGetValue(transactionIndex.ChainId,
                            out var addressList)
                        && addressList.Contains(burned.Burner.ToBase58()))
                    {
                        await SetValueInfoAsync(burntFees, burned.Symbol, burned.Amount);
                    }

                    break;
            }
        }


        foreach (var valueInfoDto in transactionValues)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount,chainId);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }

        foreach (var valueInfoDto in transactionFees)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount,chainId);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }


        foreach (var valueInfoDto in burntFees)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount,chainId);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }


        detailDto.TransactionFees = transactionFees.Values.OrderByDescending(x => x.Amount).ToList();

        detailDto.TransactionValues = transactionValues.Values.OrderByDescending(x => x.Amount).ToList();
        detailDto.BurntFees = burntFees.Values.OrderByDescending(x => x.Amount).ToList();
    }


    public async Task SetValueInfoAsync(Dictionary<string, ValueInfoDto> dic, string symbol, long amount)
    {
        if (dic.TryGetValue(symbol, out var value))
        {
            value.Amount += amount;
        }
        else
        {
            dic.Add(symbol, new ValueInfoDto()
            {
                Amount = amount,
                Symbol = symbol
            });
        }
    }


    public async Task<LogEventResponseDto> GetLogEventsAsync(GetLogEventRequestDto request)
    {
        return await _logEventProvider.GetLogEventListAsync(request);
    }


    public async Task<BlocksResponseDto> GetMergeBlocksAsync(BlocksRequestDto requestDto)
    {
        var result = new BlocksResponseDto() { };
        var searchMergeBlockList = EsIndex.SearchMergeBlockList(requestDto.SkipCount, requestDto.MaxResultCount);
        result.Blocks = _objectMapper.Map<List<BlockIndex>, List<BlockRespDto>>(searchMergeBlockList.Result.list);
        result.Total = searchMergeBlockList.Result.totalCount;
        return result;
    }

    
    [ExceptionHandler(typeof(Exception),
        Message = "GetBlocksAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["requestDto"])]
    public virtual async Task<BlocksResponseDto> GetBlocksAsync(BlocksRequestDto requestDto)
    
  {
    
        if (requestDto.ChainId.IsNullOrEmpty())
    { 
        return await GetMergeBlocksAsync(requestDto);
    }
    
        var result = new BlocksResponseDto() { };
       
            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();
            var summariesList = await _aelfIndexerProvider.GetLatestSummariesAsync(requestDto.ChainId);

            var blockHeightAsync = summariesList.First().LatestBlockHeight;
            stopwatch1.Stop();
            var endBlockHeight = blockHeightAsync - requestDto.SkipCount;
            var startBlockHeight = endBlockHeight - requestDto.MaxResultCount;

            if (requestDto.IsLastPage)
            {
                endBlockHeight = requestDto.MaxResultCount;
                startBlockHeight = 1;
            }


            _logger.LogInformation($"Cost time by get last blockHeight:{stopwatch1.Elapsed.TotalSeconds}");

            List<Task> getBlockRawDataTasks = new List<Task>();
            List<IndexerBlockDto> blockList = new List<IndexerBlockDto>();
            Dictionary<long, long> blockBurntFee = new Dictionary<long, long>();
            Stopwatch stopwatch2 = new Stopwatch();
            stopwatch2.Start();

            var blockListTask = _aelfIndexerProvider.GetLatestBlocksAsync(requestDto.ChainId,
                startBlockHeight,
                endBlockHeight).ContinueWith(task => { blockList = task.Result; });

            getBlockRawDataTasks.Add(blockListTask);

            var blockBurntFeeTask = ParseBlockBurntAsync(requestDto.ChainId,
                startBlockHeight,
                endBlockHeight).ContinueWith(task => { blockBurntFee = task.Result; });


            getBlockRawDataTasks.Add(blockBurntFeeTask);

            await Task.WhenAll(getBlockRawDataTasks);

            stopwatch2.Stop();
            _logger.LogInformation($"Cost time by get block raw data :{stopwatch2.Elapsed.TotalSeconds}");

            result.Blocks = new List<BlockRespDto>();
            result.Total = blockHeightAsync;


            Stopwatch stopwatch3 = new Stopwatch();
            stopwatch3.Start();


            for (var i = blockList.Count - 1; i >= 0; i--)
            {
                var indexerBlockDto = blockList[i];
                var latestBlockDto = new BlockRespDto();

                latestBlockDto.BlockHeight = indexerBlockDto.BlockHeight;
                latestBlockDto.Timestamp = DateTimeHelper.GetTotalSeconds(indexerBlockDto.BlockTime);
                latestBlockDto.TransactionCount = indexerBlockDto.TransactionIds.Count;
                latestBlockDto.ProducerAddress = indexerBlockDto.Miner;
                latestBlockDto.ProducerName = await GetBpNameAsync(indexerBlockDto.ChainId, indexerBlockDto.Miner);


                latestBlockDto.BurntFees = blockBurntFee.TryGetValue(indexerBlockDto.BlockHeight, out var value)
                    ? value.ToString()
                    : "0";

                if (i == 0)
                {
                    latestBlockDto.TimeSpan = result.Blocks.Last().TimeSpan;
                }
                else
                {
                    latestBlockDto.TimeSpan = (Convert.ToDouble(0 < blockList.Count
                        ? DateTimeHelper.GetTotalMilliseconds(indexerBlockDto.BlockTime) -
                          DateTimeHelper.GetTotalMilliseconds(blockList[i - 1].BlockTime)
                        : 0) / 1000).ToString("0.0");
                }


                result.Blocks.Add(latestBlockDto);
                if (indexerBlockDto.ChainId == "AELF")
                {
                    latestBlockDto.Reward = CommomHelper
                        .GetMiningRewardPerBlock(_globalOptions.CurrentValue.BlockchainStartTimestamp).ToString();
                }

                latestBlockDto.ChainIds.Add(requestDto.ChainId);
            }


            stopwatch3.Stop();
            _logger.LogInformation($"Cost time by parse block raw data:{stopwatch3.Elapsed.TotalSeconds}");
            return result;
      
    }
    

    public async Task<string> GetBpNameAsync(string chainId, string address)
    {
        if (_globalOptions.CurrentValue.BPNames.TryGetValue(chainId, out var bpNames))
        {
            if (bpNames.TryGetValue(address, out var name))
            {
                return name;
            }
        }

        return "";
    }

    [ExceptionHandler(typeof(IOException),  typeof(Exception),
        Message = "ParseBlockBurntAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionParseBlockBurntAsync), LogTargets = ["chainId","startBlockHeight","endBlockHeight"])]
    public virtual async Task<Dictionary<long, long>> ParseBlockBurntAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        var result = new Dictionary<long, long>();
      
            var blockBurntFeeListAsync =
                await _tokenIndexerProvider.GetBlockBurntFeeListAsync(chainId, startBlockHeight, endBlockHeight);


            foreach (var blockBurnFeeDto in blockBurntFeeListAsync)
            {
                result.Add(blockBurnFeeDto.BlockHeight, blockBurnFeeDto.Amount);
            }
     

        return result;
    }

    
    [ExceptionHandler(typeof(Exception),
        Message = "GetTransactionsAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["requestDto"])]
    public virtual async Task<TransactionsResponseDto> GetTransactionsAsync(TransactionsRequestDto requestDto)
    {
        var result = new TransactionsResponseDto();
        result.Transactions = new List<TransactionResponseDto>();

      
            var indexerTransactionList = await _blockChainIndexerProvider.GetTransactionsAsync(requestDto);


            foreach (var transactionIndex in indexerTransactionList.Items)
            {
                var transactionRespDto = new TransactionResponseDto()
                {
                    TransactionId = transactionIndex.TransactionId,
                    Timestamp = DateTimeHelper.GetTotalSeconds(transactionIndex.Metadata.Block.BlockTime),
                    TransactionValue = transactionIndex.TransactionValue.ToString(),
                    BlockHeight = transactionIndex.BlockHeight,
                    Method = transactionIndex.MethodName,
                    Status = transactionIndex.Status,
                    TransactionFee = transactionIndex.Fee.ToString(),
                };


                transactionRespDto.From = ConvertAddress(transactionIndex.From, requestDto.ChainId);

                transactionRespDto.To = ConvertAddress(transactionIndex.To, requestDto.ChainId);
                result.Transactions.Add(transactionRespDto);
            }

            result.Transactions = result.Transactions.OrderByDescending(item => item.BlockHeight)
                .ThenByDescending(item => item.TransactionId)
                .ToList();

            result.Total = indexerTransactionList.TotalCount;
       

        return result;
    }


    public async Task<TransactionsResponseDto> ParseIndexerTransactionListAsync(List<TransactionIndex> list)
    {
        var transactionsResponseDto = new TransactionsResponseDto();
        foreach (var indexerTransactionDto in list)
        {
            var transactionResponseDto = new TransactionResponseDto()
            {
                TransactionId = indexerTransactionDto.TransactionId,
                BlockHeight = indexerTransactionDto.BlockHeight,
                Method = indexerTransactionDto.MethodName,
                Status = indexerTransactionDto.Status,
                From = ConvertAddress(indexerTransactionDto.From, indexerTransactionDto.ChainId),
                To = ConvertAddress(indexerTransactionDto.To, indexerTransactionDto.ChainId),
                Timestamp = DateTimeHelper.GetTotalSeconds(indexerTransactionDto.BlockTime),
            };
            var value = await ParseIndexerTransactionValueInfoAsync(indexerTransactionDto);

            transactionResponseDto.TransactionValue = value.Item1;
            transactionResponseDto.TransactionFee = value.Item2;
            transactionsResponseDto.Transactions.Add(transactionResponseDto);
        }


        return transactionsResponseDto;
    }


    public async Task<(string, string)> ParseIndexerTransactionValueInfoAsync(TransactionIndex transactionIndex)
    {
        double value = 0;
        double fee = 0;

        foreach (var indexerLogEventDto in transactionIndex.LogEvents)
        {
            indexerLogEventDto.ExtraProperties.TryGetValue("Indexed", out var indexed);
            indexerLogEventDto.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);

            var indexedList = indexed != null
                ? JsonConvert.DeserializeObject<List<string>>(indexed)
                : new List<string>();
            var logEvent = new LogEvent
            {
                Indexed = { indexedList?.Select(ByteString.FromBase64) },
            };

            if (nonIndexed != null)
            {
                logEvent.NonIndexed = ByteString.FromBase64(nonIndexed);
            }

            switch (indexerLogEventDto.EventName)
            {
                case nameof(Transferred):
                    var transferred = new Transferred();
                    transferred.MergeFrom(logEvent);
                    value += transferred.Symbol == "ELF" ? Convert.ToDouble(transferred.Amount) : 0;
                    break;
                case nameof(TransactionFeeCharged):
                    var transactionFeeCharged = new TransactionFeeCharged();
                    transactionFeeCharged.MergeFrom(logEvent);
                    fee += transactionFeeCharged.Symbol == "ELF"
                        ? Convert.ToDouble(transactionFeeCharged.Amount)
                        : 0;


                    break;
            }
        }

        return (value.ToString(), fee.ToString());
    }

    public async Task SetTransactionInfoAsync(TransactionResponseDto transaction,
        TransactionIndex transactionIndex)
    {
        var transactionValue = 0d;
        var fee = 0d;

        foreach (var txnLogEvent in transactionIndex.LogEvents)
        {
            txnLogEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
            txnLogEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);

            var indexedList = indexed != null
                ? JsonConvert.DeserializeObject<List<string>>(indexed)
                : new List<string>();
            var logEvent = new LogEvent
            {
                Indexed = { indexedList?.Select(ByteString.FromBase64) },
            };

            if (nonIndexed != null)
            {
                logEvent.NonIndexed = ByteString.FromBase64(nonIndexed);
            }

            if (txnLogEvent.EventName == nameof(Transferred))
            {
                var transferred = new Transferred();
                transferred.MergeFrom(logEvent);
                transactionValue += transferred.Symbol == "ELF" ? Convert.ToDouble(transferred.Amount) : 0;
            }

            if (txnLogEvent.EventName == nameof(TransactionFeeCharged))
            {
                var transactionFeeCharged = new TransactionFeeCharged();
                transactionFeeCharged.MergeFrom(logEvent);
                fee += transactionFeeCharged.Symbol == "ELF"
                    ? Convert.ToDouble(transactionFeeCharged.Amount)
                    : 0;
            }
        }

        transaction.TransactionValue = transactionValue.ToString();
        transaction.TransactionFee = fee.ToString();
    }


    private CommonAddressDto ConvertAddress(string address, string chainId)
    {
        var commonAddressDto = new CommonAddressDto()
        {
            AddressType = AddressType.EoaAddress,
            Address = address
        };
        if (_globalOptions.CurrentValue.ContractNames.TryGetValue(chainId, out var contractNames))
        {
            if (contractNames.TryGetValue(address, out var contractName))
            {
                commonAddressDto.Name = contractName;
                commonAddressDto.AddressType = AddressType.ContractAddress;
            }
        }


        return commonAddressDto;
    }

    public IMessage? ParseMessage(string eventName, ByteString byteString)
    {
        IMessage? message = eventName switch
        {
            "Transferred" => Transferred.Parser.ParseFrom(byteString),
            "CrossChainTransferred" => CrossChainTransferred.Parser.ParseFrom(byteString),
            "CrossChainReceived" => CrossChainReceived.Parser.ParseFrom(byteString),
            _ => null
        };
        return message;
    }
}