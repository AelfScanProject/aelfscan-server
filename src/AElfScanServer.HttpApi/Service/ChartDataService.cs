using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.Domain.Shared.Common;
using AElfScanServer.HttpApi.Dtos.ChartData;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using Elasticsearch.Net;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.HttpApi.Service;

public interface IChartDataService
{
    public Task<DailyTransactionCountResp> GetDailyTransactionCountAsync(ChartDataRequest request);


    public Task<UniqueAddressCountResp> GetUniqueAddressCountAsync(ChartDataRequest request);


    public Task<ActiveAddressCountResp> GetActiveAddressCountAsync(ChartDataRequest request);

    public Task<BlockProduceRateResp> GetBlockProduceRateAsync(ChartDataRequest request);

    public Task<AvgBlockDurationResp> GetAvgBlockDurationRespAsync(ChartDataRequest request);

    public Task<CycleCountResp> GetCycleCountRespAsync(ChartDataRequest request);

    public Task<NodeBlockProduceResp> GetNodeBlockProduceRespAsync(ChartDataRequest request);

    public Task<DailyAvgTransactionFeeResp> GetDailyAvgTransactionFeeRespAsync(ChartDataRequest request);

    public Task<DailyTransactionFeeResp> GetDailyTransactionFeeRespAsync(ChartDataRequest request);

    public Task<DailyTotalBurntResp> GetDailyTotalBurntRespAsync(ChartDataRequest request);

    public Task<DailyDeployContractResp> GetDailyDeployContractRespAsync(ChartDataRequest request);

    public Task<ElfPriceIndexResp> GetElfPriceIndexRespAsync(ChartDataRequest request);


    public Task<DailyBlockRewardResp> GetDailyBlockRewardRespAsync(ChartDataRequest request);

    public Task<DailyAvgBlockSizeResp> GetDailyAvgBlockSizeRespRespAsync(ChartDataRequest request);


    public Task<DailyTotalContractCallResp> GetDailyTotalContractCallRespRespAsync(ChartDataRequest request);

    public Task<TopContractCallResp> GetTopContractCallRespAsync(ChartDataRequest request);

    public Task<DailyMarketCapResp> GetDailyMarketCapRespAsync(ChartDataRequest request);

    public Task<DailySupplyGrowthResp> GetDailySupplyGrowthRespAsync(ChartDataRequest request);

    public Task<DailyStakedResp> GetDailyStakedRespAsync(ChartDataRequest request);

    public Task<DailyHolderResp> GetDailyHolderRespAsync(ChartDataRequest request);

    public Task<InitRoundResp> InitDailyNetwork(SetRoundRequest request);

    public Task<JonInfoResp> GetJobInfo(SetJob request);
}

public class ChartDataService : AbpRedisCache, IChartDataService, ITransientDependency
{
    private readonly ILogger<ChartDataService> _logger;
    private readonly IEntityMappingRepository<RoundIndex, string> _roundIndexRepository;
    private readonly IEntityMappingRepository<NodeBlockProduceIndex, string> _nodeBlockProduceIndex;
    private readonly IEntityMappingRepository<DailyBlockProduceCountIndex, string> _blockProduceIndexRepository;
    private readonly IEntityMappingRepository<DailyBlockProduceDurationIndex, string> _blockProduceDurationRepository;
    private readonly IEntityMappingRepository<HourNodeBlockProduceIndex, string> _hourNodeBlockProduceRepository;
    private readonly IEntityMappingRepository<DailyCycleCountIndex, string> _cycleCountRepository;
    private readonly IEntityMappingRepository<DailyAvgTransactionFeeIndex, string> _avgTransactionFeeRepository;
    private readonly IEntityMappingRepository<ElfPriceIndex, string> _elfPriceRepository;
    private readonly IEntityMappingRepository<DailyBlockRewardIndex, string> _blockRewardRepository;
    private readonly IEntityMappingRepository<DailyTotalBurntIndex, string> _totalBurntRepository;
    private readonly IEntityMappingRepository<DailyDeployContractIndex, string> _deployContractRepository;
    private readonly IEntityMappingRepository<DailyAvgBlockSizeIndex, string> _blockSizeRepository;
    private readonly IEntityMappingRepository<TransactionIndex, string> _transactionsRepository;
    private readonly IElasticClient _elasticClient;

    private readonly IEntityMappingRepository<DailyTransactionCountIndex, string> _transactionCountRepository;
    private readonly IEntityMappingRepository<DailyUniqueAddressCountIndex, string> _uniqueAddressRepository;
    private readonly IEntityMappingRepository<DailyActiveAddressCountIndex, string> _activeAddressRepository;
    private readonly IEntityMappingRepository<DailyContractCallIndex, string> _dailyContractCallRepository;
    private readonly IEntityMappingRepository<DailyTotalContractCallIndex, string> _dailyTotalContractCallRepository;
    private readonly IEntityMappingRepository<DailyMarketCapIndex, string> _dailyMarketCapIndexRepository;
    private readonly IEntityMappingRepository<DailySupplyGrowthIndex, string> _dailySupplyGrowthIndexRepository;
    private readonly IEntityMappingRepository<DailyStakedIndex, string> _dailyStakedIndexRepository;
    private readonly IDailyHolderProvider _dailyHolderProvider;
    private readonly IEntityMappingRepository<DailyTransactionRecordIndex, string> _transactionRecordIndexRepository;


    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly IObjectMapper _objectMapper;


    public ChartDataService(IOptions<RedisCacheOptions> optionsAccessor, ILogger<ChartDataService> logger,
        IEntityMappingRepository<RoundIndex, string> roundIndexRepository,
        IEntityMappingRepository<NodeBlockProduceIndex, string> nodeBlockProduceIndex,
        IEntityMappingRepository<DailyBlockProduceCountIndex, string> blockProduceIndexRepository,
        IObjectMapper objectMapper,
        IEntityMappingRepository<DailyBlockProduceDurationIndex, string> blockProduceDurationRepository,
        IEntityMappingRepository<DailyCycleCountIndex, string> cycleCountRepository,
        IOptionsMonitor<GlobalOptions> globalOptions,
        IEntityMappingRepository<HourNodeBlockProduceIndex, string> hourNodeBlockProduceRepository,
        IEntityMappingRepository<DailyAvgTransactionFeeIndex, string> avgTransactionFeeRepository,
        IEntityMappingRepository<ElfPriceIndex, string> elfPriceRepository,
        IEntityMappingRepository<DailyBlockRewardIndex, string> blockRewardRepository,
        IEntityMappingRepository<DailyTotalBurntIndex, string> totalBurntRepository,
        IEntityMappingRepository<DailyDeployContractIndex, string> deployContractRepository,
        IEntityMappingRepository<DailyAvgBlockSizeIndex, string> blockSizeRepository,
        IEntityMappingRepository<DailyTransactionCountIndex, string> transactionCountRepository,
        IEntityMappingRepository<DailyUniqueAddressCountIndex, string> uniqueAddressRepository,
        IEntityMappingRepository<DailyActiveAddressCountIndex, string> activeAddressRepository,
        IEntityMappingRepository<TransactionIndex, string> transactionsRepository,
        IEntityMappingRepository<DailyContractCallIndex, string> dailyContractCallRepository,
        IEntityMappingRepository<DailyTotalContractCallIndex, string> dailyTotalContractCallRepository,
        IEntityMappingRepository<DailyMarketCapIndex, string> dailyMarketCapIndexRepository,
        IEntityMappingRepository<DailySupplyGrowthIndex, string> dailySupplyGrowthIndexRepository,
        IEntityMappingRepository<DailyStakedIndex, string> dailyStakedIndexRepository,
        IEntityMappingRepository<DailyTransactionRecordIndex, string> transactionRecordIndexRepository,
        IDailyHolderProvider dailyHolderProvider,
        IOptionsMonitor<ElasticsearchOptions> options) : base(
        optionsAccessor)
    {
        _logger = logger;
        _roundIndexRepository = roundIndexRepository;
        _nodeBlockProduceIndex = nodeBlockProduceIndex;
        _blockProduceIndexRepository = blockProduceIndexRepository;
        _objectMapper = objectMapper;
        _blockProduceDurationRepository = blockProduceDurationRepository;
        _cycleCountRepository = cycleCountRepository;
        _globalOptions = globalOptions;
        _hourNodeBlockProduceRepository = hourNodeBlockProduceRepository;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool);
        _elasticClient = new ElasticClient(settings);
        _avgTransactionFeeRepository = avgTransactionFeeRepository;
        _elfPriceRepository = elfPriceRepository;
        _blockRewardRepository = blockRewardRepository;
        _totalBurntRepository = totalBurntRepository;
        _deployContractRepository = deployContractRepository;
        EsIndex.SetElasticClient(_elasticClient);
        _blockSizeRepository = blockSizeRepository;
        _transactionCountRepository = transactionCountRepository;
        _uniqueAddressRepository = uniqueAddressRepository;
        _activeAddressRepository = activeAddressRepository;
        _transactionsRepository = transactionsRepository;
        _dailyContractCallRepository = dailyContractCallRepository;
        _dailyTotalContractCallRepository = dailyTotalContractCallRepository;
        _dailyMarketCapIndexRepository = dailyMarketCapIndexRepository;
        _dailySupplyGrowthIndexRepository = dailySupplyGrowthIndexRepository;
        _dailyStakedIndexRepository = dailyStakedIndexRepository;
        _transactionRecordIndexRepository = transactionRecordIndexRepository;
        _dailyHolderProvider = dailyHolderProvider;
    }


    public async Task<JonInfoResp> GetJobInfo(SetJob request)
    {
        await ConnectAsync();
        var jonInfoResp = new JonInfoResp() { };

        var v1 = RedisDatabase.StringGet(RedisKeyHelper.TransactionLastBlockHeight(request.ChainId));
        var v2 = RedisDatabase.StringGet(RedisKeyHelper.BlockSizeLastBlockHeight(request.ChainId));
        var v3 = RedisDatabase.StringGet(RedisKeyHelper.LatestRound(request.ChainId));


        var queryable1 = await _roundIndexRepository.GetQueryableAsync();
        var roundIndices = queryable1.Where(c => c.ChainId == request.ChainId)
            .OrderByDescending(c => c.RoundNumber).Take(1).ToList();


        jonInfoResp.RedisLastBlockHeight = long.Parse(v1);
        jonInfoResp.BlockSizeBlockHeight = long.Parse(v2);
        jonInfoResp.RedisLastRound = long.Parse(v3);


        jonInfoResp.EsLastRound = roundIndices[0].RoundNumber;
        jonInfoResp.EsLastRoundDate = roundIndices[0].DateStr;

        if (request.SetBlockHeight > 0)
        {
            RedisDatabase.StringSet(RedisKeyHelper.TransactionLastBlockHeight(request.ChainId), request.SetBlockHeight);
        }

        if (request.SetSizBlockHeight > 0)
        {
            RedisDatabase.StringSet(RedisKeyHelper.BlockSizeLastBlockHeight(request.ChainId),
                request.SetSizBlockHeight);
        }

        if (request.SetLastRound > 0)
        {
            RedisDatabase.StringSet(RedisKeyHelper.LatestRound(request.ChainId),
                request.SetLastRound);
        }


        var queryable2 = await _transactionRecordIndexRepository.GetQueryableAsync();
        queryable2 = queryable2.Where(c => c.ChainId == request.ChainId);
        var count = queryable2.Count();
        var dailyTransactionRecordIndices = queryable2.OrderByDescending(c => c.DateStr).Take(1);
        jonInfoResp.TransactionLastDate = dailyTransactionRecordIndices.First().DateStr;
        jonInfoResp.TransactionDateCount = count;

        return jonInfoResp;
    }


    public async Task<DailyHolderResp> GetDailyHolderRespAsync(ChartDataRequest request)
    {
        var dailyHolderListAsync = await _dailyHolderProvider.GetDailyHolderListAsync(request.ChainId);
        var dailyHolderDtos = dailyHolderListAsync.DailyHolder;

        var dailyHolders = new List<DailyHolder>();

        dailyHolderDtos = dailyHolderDtos.OrderBy(c => c.DateStr).ToList();


        for (var i = 0; i < dailyHolderDtos.Count; i++)
        {
            dailyHolders.Add(new DailyHolder()
            {
                Date = DateTimeHelper.ConvertYYMMDD(dailyHolderDtos[i].DateStr),
                DateStr = dailyHolderDtos[i].DateStr,
                Count = dailyHolderDtos[i].Count
            });

            var curCount = dailyHolderDtos[i].Count;
            var nextDate = DateTimeHelper.GetNextDayDate(dailyHolderDtos[i].DateStr);

            while (i < dailyHolderDtos.Count - 1 && nextDate != dailyHolderDtos[i + 1].DateStr)
            {
                dailyHolders.Add(new DailyHolder()
                {
                    Date = DateTimeHelper.ConvertYYMMDD(nextDate),
                    DateStr = nextDate,
                    Count = curCount
                });
                nextDate = DateTimeHelper.GetNextDayDate(nextDate);
            }
        }

        return new DailyHolderResp()
        {
            List = dailyHolders,
            Total = dailyHolders.Count,
            Highest = dailyHolders.MaxBy(c => c.Count),
            Lowest = dailyHolders.MinBy(c => c.Count),
        };
    }

    public async Task<DailyMarketCapResp> GetDailyMarketCapRespAsync(ChartDataRequest request)
    {
        var queryable = await _dailyMarketCapIndexRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000)
            .ToList();

        var datList = _objectMapper.Map<List<DailyMarketCapIndex>, List<DailyMarketCap>>(indexList);

        datList[0].TotalMarketCap = datList[0].IncrMarketCap;
        for (var i = 1; i < datList.Count; i++)
        {
            var supply1 = double.Parse(datList[i].IncrMarketCap);
            var supply2 = double.Parse(datList[i - 1].TotalMarketCap);
            datList[i].TotalMarketCap = (supply1 + supply2).ToString("F6");
        }


        var resp = new DailyMarketCapResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.IncrMarketCap)),
            Lowest = datList.MinBy(c => double.Parse(c.IncrMarketCap)),
        };

        return resp;
    }


    public async Task<DailyStakedResp> GetDailyStakedRespAsync(ChartDataRequest request)
    {
        var queryable = await _dailyStakedIndexRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();


        var datList = _objectMapper.Map<List<DailyStakedIndex>, List<DailyStaked>>(indexList);


        if (_globalOptions.CurrentValue.IsMainNet)
        {
            datList[0].TotalStaked = "1700000";
            datList[0].BpStaked = "1700000";
        }
        else
        {
            datList[0].TotalStaked = "500000";
            datList[0].BpStaked = "500000";
        }

        var totalStaked = double.Parse(datList[0].BpStaked) + double.Parse(datList[0].VoteStaked);

        datList[0].Rate = (totalStaked / double.Parse(datList[0].Supply) * 100).ToString("F4");
        for (var i = 1; i < datList.Count; i++)
        {
            var supply = double.Parse(datList[i - 1].Supply) + double.Parse(datList[i].Supply);
            datList[i].Supply = supply.ToString();

            var curtBpStaked = double.Parse(datList[i].BpStaked) + double.Parse(datList[i - 1].BpStaked);
            datList[i].BpStaked = curtBpStaked.ToString();

            var curtVoteStaked = double.Parse(datList[i].VoteStaked) + double.Parse(datList[i - 1].VoteStaked);
            datList[i].VoteStaked = curtVoteStaked.ToString();

            var curTotalStaked = curtBpStaked + curtVoteStaked;

            datList[i].Rate = (curTotalStaked / supply * 100).ToString("F4");
            datList[i].TotalStaked = curTotalStaked.ToString("f4");
        }

        var resp = new DailyStakedResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
        };

        return resp;
    }

    public async Task<DailySupplyGrowthResp> GetDailySupplyGrowthRespAsync(ChartDataRequest request)
    {
        var queryable = await _dailySupplyGrowthIndexRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailySupplyGrowthIndex>, List<DailySupplyGrowth>>(indexList);

        datList[0].TotalSupply = datList[0].IncrSupply;
        for (var i = 1; i < datList.Count; i++)
        {
            var supply1 = double.Parse(datList[i].IncrSupply);
            var supply2 = double.Parse(datList[i - 1].TotalSupply);
            datList[i].TotalSupply = (supply1 + supply2).ToString("F6");
        }

        var resp = new DailySupplyGrowthResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.IncrSupply)),
            Lowest = datList.MinBy(c => double.Parse(c.IncrSupply)),
        };

        return resp;
    }

    public async Task<DailyAvgBlockSizeResp> GetDailyAvgBlockSizeRespRespAsync(ChartDataRequest request)
    {
        var queryable = await _blockSizeRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailyAvgBlockSizeIndex>, List<DailyAvgBlockSize>>(indexList);

        var resp = new DailyAvgBlockSizeResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.AvgBlockSize)),
            Lowest = datList.MinBy(c => double.Parse(c.AvgBlockSize)),
        };

        return resp;
    }


    public async Task<DailyTotalContractCallResp> GetDailyTotalContractCallRespRespAsync(ChartDataRequest request)
    {
        var queryable = await _dailyTotalContractCallRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var dataList = _objectMapper.Map<List<DailyTotalContractCallIndex>, List<DailyTotalContractCall>>(indexList);

        var resp = new DailyTotalContractCallResp()
        {
            List = dataList.OrderBy(c => c.DateStr).ToList(),
            Total = dataList.Count,
            Highest = dataList.MaxBy(c => c.CallCount),
            Lowest = dataList.MinBy(c => c.CallCount),
        };

        return resp;
    }

    public async Task<TopContractCallResp> GetTopContractCallRespAsync(ChartDataRequest request)
    {
        var queryable = await _dailyContractCallRepository.GetQueryableAsync();
        queryable = queryable.Where(c => c.ChainId == request.ChainId);

        var lastIndex = queryable.OrderByDescending(c => c.Date).OrderBy(c => c.Date).Take(1).ToList().First();

        var end = lastIndex.Date;
        var start = DateTimeHelper.GetPreviousDayMilliseconds(end, request.DateInterval);

        var indexList = queryable.Where(c => c.Date >= start && c.Date <= end).Take(1000)
            .ToList();


        var dic = new Dictionary<string, TopContractCall>();
        var addressDic = new Dictionary<string, HashSet<string>>();

        var totalCall = 0l;
        foreach (var dailyContractCall in indexList)
        {
            if (dic.TryGetValue(dailyContractCall.ContractAddress, out var v))
            {
                v.CallCount += dailyContractCall.CallCount;
                totalCall += dailyContractCall.CallCount;
                foreach (var s in dailyContractCall.CallerSet)
                {
                    addressDic[dailyContractCall.ContractAddress].Add(s);
                }
            }
            else
            {
                dic[dailyContractCall.ContractAddress] = new TopContractCall()
                {
                    ContractAddress = dailyContractCall.ContractAddress,
                    ContractName = await GetContractName(request.ChainId, dailyContractCall.ContractAddress),
                    CallCount = dailyContractCall.CallCount,
                };
                totalCall += dailyContractCall.CallCount;

                addressDic[dailyContractCall.ContractAddress] = new HashSet<string>(dailyContractCall.CallerSet);
            }
        }


        var list = dic.Values.Select(c =>
        {
            c.CallAddressCount = addressDic[c.ContractAddress].Count;
            c.CallRate = ((double)c.CallCount / totalCall * 100).ToString("F2");
            return c;
        }).OrderByDescending(c => c.CallCount);


        var resp = new TopContractCallResp()
        {
            List = list.ToList(),
            Total = list.Count(),
            Highest = list.MaxBy(c => c.CallCount),
            Lowest = list.MinBy(c => c.CallCount),
        };

        return resp;
    }

    public async Task<DailyTransactionFeeResp> GetDailyTransactionFeeRespAsync(ChartDataRequest request)
    {
        var queryable = await _avgTransactionFeeRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailyAvgTransactionFeeIndex>, List<DailyTransactionFee>>(indexList);


        foreach (var dailyAvgTransactionFee in datList)
        {
            dailyAvgTransactionFee.TotalFeeElf = double.Parse(dailyAvgTransactionFee.TotalFeeElf).ToString("F6");
        }

        var resp = new DailyTransactionFeeResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.TotalFeeElf)),
            Lowest = datList.MinBy(c => double.Parse(c.TotalFeeElf)),
        };

        return resp;
    }

    public async Task<DailyAvgTransactionFeeResp> GetDailyAvgTransactionFeeRespAsync(ChartDataRequest request)
    {
        var queryable = await _avgTransactionFeeRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailyAvgTransactionFeeIndex>, List<DailyAvgTransactionFee>>(indexList);


        foreach (var dailyAvgTransactionFee in datList)
        {
            dailyAvgTransactionFee.AvgFeeElf = double.Parse(dailyAvgTransactionFee.AvgFeeElf).ToString("F6");
            dailyAvgTransactionFee.TotalFeeElf = double.Parse(dailyAvgTransactionFee.TotalFeeElf).ToString("F6");
            dailyAvgTransactionFee.AvgFeeUsdt = double.Parse(dailyAvgTransactionFee.AvgFeeUsdt).ToString("F6");
        }

        var resp = new DailyAvgTransactionFeeResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.AvgFeeElf)),
            Lowest = datList.MinBy(c => double.Parse(c.AvgFeeElf)),
        };

        return resp;
    }

    public async Task<DailyTotalBurntResp> GetDailyTotalBurntRespAsync(ChartDataRequest request)
    {
        var queryable = await _totalBurntRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailyTotalBurntIndex>, List<DailyTotalBurnt>>(indexList);

        foreach (var data in datList)
        {
            data.Burnt = double.Parse(data.Burnt).ToString("F6");
        }


        var resp = new DailyTotalBurntResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.Burnt)),
            Lowest = datList.MinBy(c => double.Parse(c.Burnt)),
        };

        return resp;
    }

    public async Task<DailyDeployContractResp> GetDailyDeployContractRespAsync(ChartDataRequest request)
    {
        var queryable = await _deployContractRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var dataList = _objectMapper.Map<List<DailyDeployContractIndex>, List<DailyDeployContract>>(indexList);

        dataList = dataList.OrderBy(c => c.DateStr).ToList();


        dataList[0].TotalCount = dataList[0].Count;

        for (int i = 1; i < dataList.Count; i++)
        {
            var count1 = int.Parse(dataList[i - 1].TotalCount);
            var count2 = dataList[i].Count.IsNullOrEmpty() ? 0 : int.Parse(dataList[i].Count);
            dataList[i].TotalCount = (count1 + count2).ToString();
        }


        var resp = new DailyDeployContractResp()
        {
            List = dataList,
            Total = dataList.Count,
            Highest = dataList.MaxBy(c => double.Parse(c.Count)),
            Lowest = dataList.MinBy(c => double.Parse(c.Count)),
        };

        return resp;
    }

    public async Task<ElfPriceIndexResp> GetElfPriceIndexRespAsync(ChartDataRequest request)
    {
        var queryable = await _elfPriceRepository.GetQueryableAsync();
        var indexList = queryable.Take(10000).ToList();

        var datList = _objectMapper.Map<List<ElfPriceIndex>, List<ElfPrice>>(indexList);

        foreach (var data in datList)
        {
            data.Price = double.Parse(data.Price).ToString("F6");
            if (data.Date == 0)
            {
                data.Date = DateTimeHelper.ConvertYYMMDD(data.DateStr);
            }
        }


        var resp = new ElfPriceIndexResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.Price)),
            Lowest = datList.MinBy(c => double.Parse(c.Price)),
        };

        return resp;
    }

    public async Task<DailyBlockRewardResp> GetDailyBlockRewardRespAsync(ChartDataRequest request)
    {
        var queryable = await _blockRewardRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailyBlockRewardIndex>, List<DailyBlockReward>>(indexList);

        foreach (var data in datList)
        {
            data.BlockReward = double.Parse(data.BlockReward).ToString("F6");
        }

        var resp = new DailyBlockRewardResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            Highest = datList.MaxBy(c => double.Parse(c.BlockReward)),
            Lowest = datList.MinBy(c => double.Parse(c.BlockReward)),
        };

        return resp;
    }

    public async Task<InitRoundResp> InitDailyNetwork(SetRoundRequest request)
    {
        var startDate = DateTimeHelper.ConvertYYMMDD(request.StartDate);

        var endDate = DateTimeHelper.ConvertYYMMDD(request.EndDate);
        if (request.SetNumber > 0)
        {
            await ConnectAsync();
            RedisDatabase.StringSet(RedisKeyHelper.LatestRound(request.ChainId), request.SetNumber);
        }


        var dailyTimestamps = DateTimeHelper.GetRangeDayList(startDate, endDate);
        var list = new List<string>();
        foreach (var dailyTimestamp in dailyTimestamps)
        {
            list.Add(DateTimeHelper.GetDateTimeString(dailyTimestamp));
        }


        var queryable = await _roundIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(c => c.ChainId == request.ChainId);
        var roundIndices = queryable.Where(c => c.StartTime >= startDate)
            .Where(c => c.StartTime <= endDate);
        var count = roundIndices.Count();


        var initRoundResp = new InitRoundResp()
        {
            RoundCount = count,
            UpdateDate = new List<string>(),
            UpdateDateNodeBlockProduce = new List<string>()
        };


        if (!request.UpdateData)
        {
            return initRoundResp;
        }

        await UpdateHourNodeBlockProduce(request.ChainId, startDate, endDate);

        for (var i = 0; i < dailyTimestamps.Count - 1; i++)
        {
            var start = dailyTimestamps[i];
            var end = dailyTimestamps[i + 1];
            var indices = queryable.Where(c => c.StartTime >= start).Where(c => c.StartTime < end)
                .Take(10000)
                .OrderBy(c => c.RoundNumber).ToList();

            _logger.LogInformation("InitDailyNetwork chainId:{c},date:{d},end:{e},endstr:{e}", request.ChainId,
                DateTimeHelper.GetDateTimeString(start) + "_count:" + indices.Count, end,
                DateTimeHelper.GetDateTimeString(end));
            if (!indices.IsNullOrEmpty())
            {
                await UpdateDailyNetwork(request.ChainId, start, indices);
                initRoundResp.UpdateDate.Add(DateTimeHelper.GetDateTimeString(start) + "_count:" +
                                             indices.Count);
            }
        }


        return initRoundResp;
    }

    public async Task UpdateHourNodeBlockProduce(string chainId, long start, long end)
    {
        var rangeDayList = DateTimeHelper.GetRangeDayList(start, end);


        var hourNodeBlockProduceIndices = new List<HourNodeBlockProduceIndex>();

        foreach (var dayTotalSeconds in rangeDayList)
        {
            var dayHourList = DateTimeHelper.GetDayHourList(dayTotalSeconds);

            var queryable = await _nodeBlockProduceIndex.GetQueryableAsync();
            queryable = queryable.Where(c => c.ChainId == chainId);


            var batch = new List<HourNodeBlockProduceIndex>();

            for (var i = 0; i < dayHourList.Count - 1; i++)
            {
                start = dayHourList[i];
                end = dayHourList[i + 1];
                var indexList = queryable.Where(c => c.StartTime >= start)
                    .Where(c => c.EndTime < end).ToList();

                if (indexList.IsNullOrEmpty())
                {
                    continue;
                }

                var dic = new Dictionary<string, HourNodeBlockProduceIndex>();

                foreach (var index in indexList)
                {
                    if (dic.TryGetValue(index.NodeAddress, out var value))
                    {
                        value.MissedBlocks = index.MissedBlocks;
                        value.Blocks = index.Blcoks;
                        value.TotalCycle++;
                    }
                    else
                    {
                        dic[index.NodeAddress] = new HourNodeBlockProduceIndex()
                        {
                            Date = dayHourList[i],
                            DateStr = DateTimeHelper.GetDateTimeString(dayHourList[i]),
                            ChainId = chainId,
                            Hour = i,
                            TotalCycle = 1,
                            NodeAddress = index.NodeAddress
                        };
                    }
                }

                batch.AddRange(dic.Values.Select(c => c).ToList());
            }

            hourNodeBlockProduceIndices.AddRange(batch);
        }

        await _hourNodeBlockProduceRepository.AddOrUpdateManyAsync(hourNodeBlockProduceIndices);
        _logger.LogInformation("Insert hour node block produce index chainId:{c},start date:{d1},end date{d2}", chainId,
            DateTimeHelper.GetDateTimeString(start), DateTimeHelper.GetDateTimeString(end));
    }

    public async Task UpdateDailyNetwork(string chainId, long todayTotalSeconds, List<RoundIndex> list)
    {
        if (list.IsNullOrEmpty())
        {
            return;
        }

        var blockProduceIndex = new DailyBlockProduceCountIndex()
        {
            Date = todayTotalSeconds,
            ChainId = chainId
        };

        var dailyCycleCountIndex = new DailyCycleCountIndex()
        {
            Date = todayTotalSeconds,
            ChainId = chainId
        };

        var dailyBlockProduceDurationIndex = new DailyBlockProduceDurationIndex()
        {
            Date = todayTotalSeconds,
            ChainId = chainId
        };


        var totalDuration = 0l;
        decimal longestBlockDuration = 0;
        decimal shortestBlockDuration = 0;
        foreach (var round in list)
        {
            blockProduceIndex.BlockCount += round.Blcoks;
            blockProduceIndex.MissedBlockCount += round.MissedBlocks;

            dailyCycleCountIndex.CycleCount++;
            totalDuration += round.DurationSeconds;
            if (round.Blcoks == 0)
            {
                dailyCycleCountIndex.MissedCycle++;
            }

            if (round.Blcoks == 0 || round.DurationSeconds == 0)
            {
                _logger.LogWarning("Round duration or blocks is zero,chainId:{0},round number:{1}", chainId,
                    round.RoundNumber);
                continue;
            }

            var roundDurationSeconds = round.DurationSeconds / (decimal)round.Blcoks;

            if (longestBlockDuration == 0)
            {
                longestBlockDuration = roundDurationSeconds;
            }
            else
            {
                longestBlockDuration =
                    Math.Max(longestBlockDuration, roundDurationSeconds);
            }


            if (shortestBlockDuration == 0)
            {
                shortestBlockDuration = roundDurationSeconds;
            }
            else
            {
                shortestBlockDuration =
                    Math.Min(shortestBlockDuration, roundDurationSeconds);
            }
        }

        dailyCycleCountIndex.MissedBlockCount = blockProduceIndex.MissedBlockCount;
        dailyBlockProduceDurationIndex.AvgBlockDuration =
            (totalDuration / 1000 / (decimal)blockProduceIndex.BlockCount).ToString("F2");
        dailyBlockProduceDurationIndex.LongestBlockDuration = (longestBlockDuration / 1000).ToString("F2");
        dailyBlockProduceDurationIndex.ShortestBlockDuration = (shortestBlockDuration / 1000).ToString("F2");

        decimal result = blockProduceIndex.BlockCount /
            (decimal)(blockProduceIndex.BlockCount + blockProduceIndex.MissedBlockCount) * 100;
        blockProduceIndex.BlockProductionRate = result.ToString("F2");

        await _blockProduceIndexRepository.AddOrUpdateAsync(blockProduceIndex);
        await _blockProduceDurationRepository.AddOrUpdateAsync(dailyBlockProduceDurationIndex);
        await _cycleCountRepository.AddOrUpdateAsync(dailyCycleCountIndex);
        _logger.LogInformation("Insert daily network statistic count index chainId:{0},date:{1}", chainId,
            DateTimeHelper.GetDateTimeString(todayTotalSeconds));
    }


    public async Task<NodeBlockProduceResp> GetNodeBlockProduceRespAsync(ChartDataRequest request)
    {
        var hourProduceQue = await _hourNodeBlockProduceRepository.GetQueryableAsync();

        if (request.StartDate > 0 && request.EndDate > 0)
        {
            hourProduceQue = hourProduceQue.Where(c => c.Date >= request.StartDate)
                .Where(c => c.Date < request.EndDate);
        }

        var nodeBlockProduceResp = new NodeBlockProduceResp();

        var nodeBlockProduces = new Dictionary<string, NodeBlockProduce>();

        var nodeBlockProduceIndices = hourProduceQue.ToList();
        var hourNodeBlockProduceIndices = hourProduceQue.Where(c => c.ChainId == request.ChainId).Take(10000).ToList();
        foreach (var hourNodeBlockProduceIndex in hourNodeBlockProduceIndices)
        {
            var address = hourNodeBlockProduceIndex.NodeAddress;
            if (nodeBlockProduces.TryGetValue(address, out var v))
            {
                v.TotalCycle += hourNodeBlockProduceIndex.TotalCycle;
                v.Blocks += hourNodeBlockProduceIndex.Blocks;
                v.MissedBlocks += hourNodeBlockProduceIndex.MissedBlocks;
            }
            else
            {
                nodeBlockProduces[address] = new NodeBlockProduce()
                {
                    NodeAddress = address,
                    NodeName = await GetBpName(request.ChainId, address),
                    TotalCycle = hourNodeBlockProduceIndex.TotalCycle,
                    Blocks = hourNodeBlockProduceIndex.Blocks,
                    MissedBlocks = hourNodeBlockProduceIndex.MissedBlocks
                };
            }
        }

        var roundQuery = await _roundIndexRepository.GetQueryableAsync();
        if (request.StartDate > 0 && request.EndDate > 0)
        {
            roundQuery = roundQuery.Where(c => c.StartTime >= request.StartDate && c.StartTime < request.EndDate);
        }

        var roundIndices = roundQuery.Where(c => c.ChainId == request.ChainId).Take(10000).ToList();


        var ints = new Dictionary<string, int>();
        foreach (var roundIndex in roundIndices)
        {
            var dateTimeString = DateTimeHelper.GetDateTimeString(DateTimeHelper.GetDateTimeLong(roundIndex.StartTime));
            if (ints.ContainsKey(dateTimeString))
            {
                ints[dateTimeString]++;
            }
            else
            {
                ints[dateTimeString] = 1;
            }
        }

        var dictionary = new Dictionary<string, long>();

        foreach (var roundIndex in roundIndices)
        {
            foreach (var address in roundIndex.ProduceBlockBpAddresses)
            {
                if (dictionary.TryGetValue(address, out var count))
                {
                    dictionary[address]++;
                }
                else
                {
                    dictionary[address] = 1;
                }
            }
        }

        var indices = roundIndices.ToList();

        var totalCycle = indices.Count;


        var blockProduces = nodeBlockProduces.Values.Select(c => c).OrderByDescending(c => c.Blocks).ToList();

        foreach (var nodeBlockProduce in blockProduces)
        {
            if (dictionary.TryGetValue(nodeBlockProduce.NodeAddress, out var count))
            {
                nodeBlockProduce.InRound = count;
            }

            nodeBlockProduce.BlocksRate =
                ((double)nodeBlockProduce.Blocks / (double)(nodeBlockProduce.Blocks + nodeBlockProduce.MissedBlocks) *
                 100)
                .ToString("F2");
            nodeBlockProduce.CycleRate =
                ((double)nodeBlockProduce.InRound / (double)totalCycle * 100).ToString("F2");
        }


        nodeBlockProduceResp.List = blockProduces;
        nodeBlockProduceResp.Total = blockProduces.Count;
        nodeBlockProduceResp.TotalCycle = roundIndices.Count();
        return nodeBlockProduceResp;
    }

    public async Task<BlockProduceRateResp> GetBlockProduceRateAsync(ChartDataRequest request)
    {
        var queryableAsync = await _blockProduceIndexRepository.GetQueryableAsync();


        var list = queryableAsync.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Skip(0).Take(1000)
            .ToList();


        var destination = _objectMapper.Map<List<DailyBlockProduceCountIndex>, List<DailyBlockProduceCount>>(list);

        var orderList = destination.OrderBy(c => c.BlockProductionRate);


        foreach (var i in destination)
        {
            i.DateStr = DateTimeHelper.GetDateTimeString(i.Date);
        }

        destination.RemoveAt(destination.Count - 1);
        var blockProduceRateResp = new BlockProduceRateResp()
        {
            List = destination,
            HighestBlockProductionRate = orderList.Last(),
            lowestBlockProductionRate = orderList.First(),
            Total = destination.Count()
        };

        return blockProduceRateResp;
    }

    public async Task<AvgBlockDurationResp> GetAvgBlockDurationRespAsync(ChartDataRequest request)
    {
        var queryableAsync = await _blockProduceDurationRepository.GetQueryableAsync();


        var list = queryableAsync.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Take(10000)
            .ToList();


        var destination =
            _objectMapper.Map<List<DailyBlockProduceDurationIndex>, List<DailyBlockProduceDuration>>(list);


        foreach (var i in destination)
        {
            i.DateStr = DateTimeHelper.GetDateTimeString(i.Date);
        }

        var orderList = destination.OrderBy(c => c.AvgBlockDuration);

        var durationResp = new AvgBlockDurationResp()
        {
            List = destination,
            HighestAvgBlockDuration = orderList.Last(),
            LowestBlockProductionRate = orderList.First(),
            Total = destination.Count()
        };

        return durationResp;
    }

    public async Task<CycleCountResp> GetCycleCountRespAsync(ChartDataRequest request)
    {
        var queryableAsync = await _cycleCountRepository.GetQueryableAsync();


        var list = queryableAsync.Where(c => c.ChainId == request.ChainId).OrderBy(c => c.Date).Skip(0).Take(1000)
            .ToList();


        var destination =
            _objectMapper.Map<List<DailyCycleCountIndex>, List<DailyCycleCount>>(list);

        var orderList = destination.OrderBy(c => c.CycleCount).ToList();
        orderList.RemoveAt(orderList.Count - 1);
        foreach (var i in destination)
        {
            i.DateStr = DateTimeHelper.GetDateTimeString(i.Date);
        }


        var cycleCountResp = new CycleCountResp()
        {
            List = destination,
            HighestMissedCycle = orderList.Last(),
            Total = destination.Count()
        };

        return cycleCountResp;
    }

    public async Task<DailyTransactionCountResp> GetDailyTransactionCountAsync(ChartDataRequest request)
    {
        var queryable = await _transactionCountRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailyTransactionCountIndex>, List<DailyTransactionCount>>(indexList);

        var resp = new DailyTransactionCountResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList().GetRange(1, datList.Count - 1),
            Total = datList.Count,
            HighestTransactionCount = datList.MaxBy(c => c.TransactionCount),
            LowesTransactionCount = datList.MinBy(c => c.TransactionCount),
        };

        return resp;
    }

    public async Task<UniqueAddressCountResp> GetUniqueAddressCountAsync(ChartDataRequest request)
    {
        var queryable = await _uniqueAddressRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).Take(10000).ToList();

        var dataList = _objectMapper.Map<List<DailyUniqueAddressCountIndex>, List<DailyUniqueAddressCount>>(indexList);

        dataList[0].TotalUniqueAddressees = dataList[0].AddressCount;

        for (int i = 1; i < dataList.Count; i++)
        {
            var count1 = dataList[i - 1].TotalUniqueAddressees;
            var count2 = dataList[i].AddressCount;
            dataList[i].TotalUniqueAddressees = count1 + count2;
        }

        var resp = new UniqueAddressCountResp()
        {
            List = dataList.OrderBy(c => c.DateStr).ToList(),
            Total = dataList.Count,
            HighestIncrease = dataList.MaxBy(c => c.AddressCount),
            LowestIncrease = dataList.MinBy(c => c.AddressCount),
        };

        return resp;
    }

    public async Task<ActiveAddressCountResp> GetActiveAddressCountAsync(ChartDataRequest request)
    {
        var queryable = await _activeAddressRepository.GetQueryableAsync();
        var indexList = queryable.Where(c => c.ChainId == request.ChainId).Take(10000).ToList();

        var datList = _objectMapper.Map<List<DailyActiveAddressCountIndex>, List<DailyActiveAddressCount>>(indexList);

        var resp = new ActiveAddressCountResp()
        {
            List = datList.OrderBy(c => c.DateStr).ToList(),
            Total = datList.Count,
            HighestActiveCount = datList.MaxBy(c => c.AddressCount),
            LowestActiveCount = datList.MinBy(c => c.AddressCount),
        };

        return resp;
    }


    public async Task<string> GetBpName(string chainId, string address)
    {
        _globalOptions.CurrentValue.BPNames.TryGetValue(chainId, out var contractNames);
        if (contractNames == null)
        {
            return "";
        }

        contractNames.TryGetValue(address, out var contractName);

        return contractName;
    }

    public async Task<string> GetContractName(string chainId, string address)
    {
        _globalOptions.CurrentValue.ContractNames.TryGetValue(chainId, out var contractNames);
        if (contractNames == null)
        {
            return "";
        }

        contractNames.TryGetValue(address, out var contractName);

        return contractName;
    }
}