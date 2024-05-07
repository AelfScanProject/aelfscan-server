using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Client.Treasury;
using AElf.Client.Consensus;
using AElf.Client.Consensus.AEDPoS;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using AElf.Standards.ACS10;
using AElfScanServer.BlockChain.Dtos;
using AElfScanServer.BlockChain.Helper;
using AElfScanServer.BlockChain.Options;
using Elasticsearch.Net;
using AElfScanServer.Common;
using AElfScanServer.Helper;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Caching.StackExchangeRedis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Nito.Collections;
using StackExchange.Redis;

namespace AElfScanServer.BlockChain.Provider;

public class HomePageProvider : AbpRedisCache, ISingletonDependency
{
    private readonly INESTRepository<TransactionIndex, string> _transactionIndexRepository;
    private readonly INESTRepository<BlockExtraIndex, string> _blockExtraIndexRepository;
    private readonly INESTRepository<AddressIndex, string> _addressIndexRepository;
    private readonly INESTRepository<TokenInfoIndex, string> _tokenInfoIndexRepository;
    private readonly BlockChainOptions _blockChainOptions;
    private readonly IElasticClient _elasticClient;
    private const string TransactionCountRedisKey = "transaction_count";
    private const string AddressCountRedisKey = "address_count";
    private const string BlockHeightRedisKey = "address_count";


    private readonly ILogger<HomePageProvider> _logger;

    public HomePageProvider(INESTRepository<TransactionIndex, string> transactionIndexRepository,
        ILogger<HomePageProvider> logger, IOptionsMonitor<BlockChainOptions> blockChainOptions,
        IOptions<ElasticsearchOptions> options,
        INESTRepository<BlockExtraIndex, string> blockExtraIndexRepository,
        INESTRepository<AddressIndex, string> addressIndexRepository,
        INESTRepository<TokenInfoIndex, string> tokenInfoIndexRepository,
        IOptions<RedisCacheOptions> optionsAccessor) : base(optionsAccessor)
    {
        _transactionIndexRepository = transactionIndexRepository;
        _logger = logger;
        _blockChainOptions = blockChainOptions.CurrentValue;
        var uris = options.Value.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool);
        _elasticClient = new ElasticClient(settings);
        _blockExtraIndexRepository = blockExtraIndexRepository;
        _addressIndexRepository = addressIndexRepository;
        _tokenInfoIndexRepository = tokenInfoIndexRepository;
        // InitDeque();
    }

    public async Task<long> GetRewardAsync(string chainId)
    {
        try
        {
            await ConnectAsync();
            var redisValue = RedisDatabase.StringGet(RedisKeyHelper.RewardKey(chainId));
            if (!redisValue.IsNullOrEmpty)
            {
                return Convert.ToInt64(redisValue);
            }


            var aElfClient = new AElfClient(_blockChainOptions.ChainNodeHosts[chainId]);

            var address = (await aElfClient.GetContractAddressByNameAsync(
                HashHelper.ComputeFrom("AElf.ContractNames.Consensus"))).ToBase58();

            var transactionGetCurrentTermMiningReward =
                await aElfClient.GenerateTransactionAsync(
                    aElfClient.GetAddressFromPrivateKey(BlockChainOptions.PrivateKey),
                    address,
                    "GetCurrentTermMiningReward", new Empty());

            var signTransaction =
                aElfClient.SignTransaction(BlockChainOptions.PrivateKey, transactionGetCurrentTermMiningReward);
            var transactionResult = await aElfClient.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = signTransaction.ToByteArray().ToHex()
            });

            var amount = Int64Value.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult)).Value;


            address = (await aElfClient.GetContractAddressByNameAsync(
                HashHelper.ComputeFrom("AElf.ContractNames.Treasury"))).ToBase58();

            var transactionGetUndistributedDividends =
                await aElfClient.GenerateTransactionAsync(
                    aElfClient.GetAddressFromPrivateKey(BlockChainOptions.PrivateKey),
                    address,
                    "GetUndistributedDividends", new Empty());


            signTransaction =
                aElfClient.SignTransaction(BlockChainOptions.PrivateKey, transactionGetUndistributedDividends);
            transactionResult = await aElfClient.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = signTransaction.ToByteArray().ToHex()
            });
            var dividend = Dividends.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult));
            if (dividend.Value.TryGetValue("ELF", out var value))
            {
                amount += value;
            }

            RedisDatabase.StringSet(RedisKeyHelper.RewardKey(chainId), amount,
                TimeSpan.FromSeconds(_blockChainOptions.RewardCacheExpiration));
            return amount;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get reward err,chainId:{c}", chainId);
        }

        return 0;
    }

    public async Task<List<TransactionCountPerMinuteDto>> FindTransactionRateList(string chainId)
    {
        var transactionCountPerMinuteList = new List<TransactionCountPerMinuteDto>();
        try
        {
            await ConnectAsync();
            var redisValues = RedisDatabase.ListRange(RedisKeyHelper.TransactionTPS(chainId), -180, -1);
            if (redisValues.IsNullOrEmpty())
            {
                await InitDeque();
                redisValues = RedisDatabase.ListRange(RedisKeyHelper.TransactionTPS(chainId), -1);
                if (redisValues.IsNullOrEmpty())
                {
                    _logger.LogWarning("double not find tx rate date from redis err,chainId:{c}", chainId);
                    return transactionCountPerMinuteList;
                }
            }

            foreach (var redisValue in redisValues)
            {
                var strings = redisValue.ToString().Split("_");
                if (strings.Length != 2)
                {
                    _logger.LogWarning("tx rate list len is not 2 from redis,chainId:{c},value:{v}", chainId, strings);
                    continue;
                }

                transactionCountPerMinuteList.Add(new TransactionCountPerMinuteDto()
                {
                    Start = Convert.ToInt64(strings[0]),
                    Count = Convert.ToInt32(strings[1])
                });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get tx rate date from redis err,chainId:{c}", chainId);
        }

        return transactionCountPerMinuteList;
    }

    public async Task<TransactionPerMinuteResponseDto> GetTransactionPerMinuteAsync(string chainId)
    {
        var transactionPerMinuteResp = new TransactionPerMinuteResponseDto()
        {
            All = new List<TransactionCountPerMinuteDto>(),
            Owner = new List<TransactionCountPerMinuteDto>()
        };

        var ownerList = await FindTransactionRateList(chainId);
        transactionPerMinuteResp.Owner = ownerList;
        var list = new List<Dictionary<long, long>>();
        foreach (var validChainId in _blockChainOptions.ValidChainIds.Where(s => s != chainId).ToList())
        {
            var data = await FindTransactionRateList(validChainId);
            if (!data.IsNullOrEmpty())
            {
                list.Add(data.ToDictionary(s => s.Start, s => s.Count));
            }
        }


        foreach (var txCountPerMinuteDto in ownerList)
        {
            transactionPerMinuteResp.All.Add(new TransactionCountPerMinuteDto()
            {
                Start = txCountPerMinuteDto.Start,
                Count = list.Sum(s => s.ContainsKey(txCountPerMinuteDto.Start) ? s[txCountPerMinuteDto.Start] : 0)
            });
        }

        return transactionPerMinuteResp;
    }


    public async Task InitDeque()
    {
        foreach (var validChainId in _blockChainOptions.ValidChainIds)
        {
            try
            {
                await ConnectAsync();
                if (RedisDatabase.KeyDelete(RedisKeyHelper.TransactionTPS(validChainId)))
                {
                    _logger.LogInformation("delete deque success,chainId:{c}", validChainId);
                }
                else
                {
                    _logger.LogWarning("delete deque fail,chainId:{c}", validChainId);
                }


                var resp = await _transactionIndexRepository.GetListAsync(limit: 1, sortExp: s => s.BlockTime,
                    sortType: SortOrder.Descending,
                    index: BlockChainIndexNameHelper.GenerateTransactionIndexName(validChainId));
                if (resp.Item2.Count > 0)
                {
                    var transactionCountPerMinuteList =
                        await GetTransactionRateAsync(validChainId,
                            resp.Item2.First().BlockTime.AddMinutes(-_blockChainOptions.TransactionPerMinuteCount),
                            resp.Item2.First().BlockTime);
                    var list = transactionCountPerMinuteList
                        .Select(s => $"{s.Start}_{s.Count}").ToArray();
                    RedisValue[] redisValues = Array.ConvertAll<string, RedisValue>(list, x => (RedisValue)x);
                    await RedisDatabase.ListRightPushAsync(RedisKeyHelper.TransactionTPS(validChainId), redisValues);
                    _logger.LogInformation("init deque success,chainId:{c},len:{q}", validChainId, redisValues.Length);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "init deque err,chainId:{c}", validChainId);
            }
        }
    }

    public async Task UpdateTransactionRateAsync()
    {
        foreach (var validChainId in _blockChainOptions.ValidChainIds)
        {
            try
            {
                await ConnectAsync();
                var listGetByIndex =
                    RedisDatabase.ListRightPop(RedisKeyHelper.TransactionTPS(validChainId));
                if (listGetByIndex.IsNullOrEmpty)
                {
                    _logger.LogWarning("deque is empty,chainId:{c}", validChainId);
                    // InitDeque();
                    continue;
                }

                var strings = listGetByIndex.ToString().Split("_");
                if (strings.Length != 2)
                {
                    _logger.LogWarning("deque is empty,chainId:{c},value:{v}", validChainId, strings);
                    // InitDeque();
                    continue;
                }

                var dateTime = CommomHelper.ConvertStringToDate(strings[0]);
                var count = Convert.ToInt64(strings[1]);
                var transactionCountPerMinuteList =
                    await GetTransactionRateAsync(validChainId, dateTime, dateTime.AddDays(1));

                var txCountPerMinutes = new List<string>();

                foreach (var txCountPerMinute in transactionCountPerMinuteList)
                {
                    if (txCountPerMinute.Start == Convert.ToInt64(strings[0]))
                    {
                        count += txCountPerMinute.Count;
                        txCountPerMinutes.Add($"{strings[0]}_{count}");
                    }
                    else
                    {
                        txCountPerMinutes.Add(
                            $"{txCountPerMinute.Start}_{txCountPerMinute.Count}");
                    }
                }

                RedisValue[] redisValues =
                    Array.ConvertAll<string, RedisValue>(txCountPerMinutes.ToArray(), x => (RedisValue)x);
                await RedisDatabase.ListRightPushAsync(RedisKeyHelper.TransactionTPS(validChainId), redisValues);
                _logger.LogInformation("update deque success,chainId:{c},len:{q}", validChainId,
                    redisValues.Length);

                Task.Run(async () =>
                {
                    var listLength = RedisDatabase.ListLength(RedisKeyHelper.TransactionTPS(validChainId));
                    if (listLength > _blockChainOptions.TransactionPerMinuteCount)
                    {
                        await RedisDatabase.ListTrimAsync(RedisKeyHelper.TransactionTPS(validChainId), 0,
                            listLength - _blockChainOptions.TransactionPerMinuteCount - 1);
                        _logger.LogInformation("trim deque success,chainId:{c},len:{q}", validChainId,
                            listLength - _blockChainOptions.TransactionPerMinuteCount - 1);
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "update deque err,chainId:{c}", validChainId);
            }
        }
    }


    public async Task SetTransactionPerMinuteAsync(string chainId, HomeOverviewResponseDto responseDto)
    {
        try
        {
            await ConnectAsync();
            var redisValue = RedisDatabase.ListGetByIndex(RedisKeyHelper.TransactionTPS(chainId), -1);
            if (redisValue.IsNullOrEmpty)
            {
                _logger.LogError("tx rate date from redis is empty,chainId:{c}", chainId);
                return;
            }

            var strings = redisValue.ToString().Split("_");
            if (strings.Length != 2)
            {
                _logger.LogError("tx rate date from redis err,chainId:{c},value:{v}", chainId, strings);
                return;
            }

            responseDto.Tps = Convert.ToString(strings[1]);

            responseDto.TpsTime = (DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(strings[0]))).DateTime;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "set transaction per minute err,chainId:{c}", chainId);
        }
    }

    public async Task<List<TransactionCountPerMinuteDto>> GetTransactionRateAsync(string chainId, DateTime startTime,
        DateTime endTime)
    {
        var txCountPerMinutes = new List<TransactionCountPerMinuteDto>();

        try
        {
            var searchRequest = new SearchRequest
            {
                Size = 0,
                Aggregations = new Dictionary<string, AggregationContainer>
                {
                    {
                        "documents_per_minute", new DateHistogramAggregation("documents_per_minute")
                        {
                            Field = "blockTime",
                            FixedInterval = "1m",
                            MinimumDocumentCount = 0,
                        }
                    }
                },
                Query = new DateRangeQuery()
                {
                    Field = "blockTime",
                    GreaterThanOrEqualTo = startTime,
                    LessThanOrEqualTo = endTime,
                }
            };

            var searchResponse = _elasticClient.Search<object>(searchRequest);
            if (!searchResponse.IsValid)
            {
                _logger.LogError("find transaction rate searchResponse is invalid err:{m},chainId:{c}",
                    searchResponse.DebugInformation, chainId);
                return txCountPerMinutes;
            }

            var multiBucketAggregate = searchResponse.Aggregations.DateHistogram("documents_per_minute");

            foreach (var bucket in multiBucketAggregate.Buckets)
            {
                var keyAsString = bucket.KeyAsString;
                var keyAsDateTime = bucket.Date;
                var docCount = bucket.DocCount;
                txCountPerMinutes.Add(
                    new TransactionCountPerMinuteDto()
                    {
                        Start = ((DateTimeOffset)keyAsDateTime).ToUnixTimeSeconds(),
                        Count = Convert.ToInt32(docCount)
                    }
                );
            }

            return txCountPerMinutes;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get transaction rate from es err,chainId:{c}", chainId);
        }

        return txCountPerMinutes;
    }


    public async Task<long> GetBlockHeightCount(string chainId)
    {
        try
        {
            await ConnectAsync();
            var redisValue = RedisDatabase.StringGet(BlockHeightRedisKey);
            if (!redisValue.IsNullOrEmpty)
            {
                return Convert.ToInt64(redisValue);
            }

            var aElfClient = new AElfClient(_blockChainOptions.ChainNodeHosts[chainId]);
            var blockHeight = await aElfClient.GetBlockHeightAsync();
            RedisDatabase.StringSet(BlockHeightRedisKey, blockHeight,
                TimeSpan.FromSeconds(_blockChainOptions.BlockHeightCacheExpiration));
            return blockHeight;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get blockHeight err,chainId:{c}", chainId);
            return 0;
        }
    }

    public async Task<long> GetTransactionCount(string chainId)
    {
        try
        {
            await ConnectAsync();
            var redisValue = RedisDatabase.StringGet(TransactionCountRedisKey);
            if (!redisValue.IsNullOrEmpty)
            {
                return Convert.ToInt64(redisValue);
            }

            var countAsync = await _transactionIndexRepository.CountAsync(null,
                indexPrefix: BlockChainIndexNameHelper.GenerateTransactionIndexName(chainId));


            if (!countAsync.IsValid)
            {
                _logger.LogError("count transaction err:{m},chainId:{c}", countAsync.DebugInformation, chainId);
                return 0;
            }

            RedisDatabase.StringSet(TransactionCountRedisKey, countAsync.Count,
                TimeSpan.FromSeconds(_blockChainOptions.TransactionCountCacheExpiration));
            return countAsync.Count;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "count transaction err,chainId:{c}", chainId);
            return 0;
        }
    }

    public async Task<long> GetAddressCount(string chainId)
    {
        try
        {
            await ConnectAsync();
            var redisValue = RedisDatabase.StringGet(AddressCountRedisKey);
            if (!redisValue.IsNullOrEmpty)
            {
                return Convert.ToInt64(redisValue);
            }

            var countAsync = await _addressIndexRepository.CountAsync(null,
                indexPrefix: BlockChainIndexNameHelper.GenerateAddressIndexName(chainId));

            if (!countAsync.IsValid)
            {
                _logger.LogError("count address err:{m},chainId:{c}", countAsync.DebugInformation, chainId);
                return 0;
            }

            RedisDatabase.StringSet(AddressCountRedisKey, countAsync.Count,
                TimeSpan.FromSeconds(_blockChainOptions.AddressCountCacheExpiration));

            return countAsync.Count;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "count address err,chainId:{c}", chainId);
            return 0;
        }
    }
}