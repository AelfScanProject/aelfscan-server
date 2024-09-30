using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Vote;
using AElf.CSharp.Core.Extension;
using AElf.EntityMapping.Repositories;
using AElf.Standards.ACS0;
using AElf.Types;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.EsIndex;
using AElfScanServer.Worker.Core.Provider;
using Elasticsearch.Net;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.NodeProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using Binance.Spot;
using DeviceDetectorNET;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Nito.AsyncEx;
using NUglify.Helpers;
using StackExchange.Redis;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using AddressIndex = AElfScanServer.Common.Dtos.ChartData.AddressIndex;
using Interval = Binance.Spot.Models.Interval;
using LogEventIndex = AElfScanServer.Common.Dtos.LogEventIndex;
using Math = System.Math;
using Timer = System.Timers.Timer;
using VotedItems = AElf.Client.Vote.VotedItems;

namespace AElfScanServer.Worker.Core.Service;

public interface ITransactionService
{
    public Task UpdateTransactionRatePerMinuteTaskAsync();


    public Task UpdateDailyNetwork();


    public Task BatchUpdateNodeNetworkTask();


    public Task UpdateElfPrice();

    public Task BatchPullTransactionTask();

    public Task MergeAddress();

    public Task UpdateMonthlyActiveAddress();

    public Task BatchPullLogEventTask();

    public Task DelLogEventTask();

    public Task FixDailyData();

    public Task BlockSizeTask();
}

public class TransactionService : AbpRedisCache, ITransactionService, ITransientDependency
{
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly BlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly HomePageProvider _homePageProvider;
    private readonly IOptionsMonitor<AELFIndexerOptions> _aelfIndexerOptions;
    private readonly SecretOptions _secretOptions;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly IStorageProvider _storageProvider;
    private readonly IElasticClient _elasticClient;
    private readonly IOptionsMonitor<PullTransactionChainIdsOptions> _workerOptions;

    private readonly IEntityMappingRepository<RoundIndex, string> _roundIndexRepository;
    private readonly IEntityMappingRepository<TransactionIndex, string> _transactionIndexRepository;
    private readonly IEntityMappingRepository<ElfPriceIndex, string> _priceRepository;
    private readonly IEntityMappingRepository<NodeBlockProduceIndex, string> _nodeBlockProduceRepository;
    private readonly IEntityMappingRepository<DailyBlockProduceCountIndex, string> _blockProduceRepository;
    private readonly IEntityMappingRepository<DailyBlockProduceDurationIndex, string> _blockProduceDurationRepository;
    private readonly IEntityMappingRepository<DailyCycleCountIndex, string> _cycleCountRepository;


    private readonly IEntityMappingRepository<DailyAvgTransactionFeeIndex, string> _avgTransactionFeeRepository;
    private readonly IEntityMappingRepository<DailyAvgBlockSizeIndex, string> _avgBlockSizeRepository;
    private readonly IEntityMappingRepository<DailyBlockRewardIndex, string> _blockRewardRepository;
    private readonly IEntityMappingRepository<DailyTotalBurntIndex, string> _totalBurntRepository;
    private readonly IEntityMappingRepository<DailyDeployContractIndex, string> _deployContractRepository;


    private readonly IEntityMappingRepository<DailyTransactionCountIndex, string> _transactionCountRepository;
    private readonly IEntityMappingRepository<DailyUniqueAddressCountIndex, string> _uniqueAddressRepository;
    private readonly IEntityMappingRepository<DailyActiveAddressCountIndex, string> _activeAddressRepository;
    private readonly IEntityMappingRepository<DailyAvgBlockSizeIndex, string> _blockSizeRepository;
    private readonly IEntityMappingRepository<DailyTransactionRecordIndex, string> _recordIndexRepository;
    private readonly IEntityMappingRepository<DailyHasFeeTransactionIndex, string> _hasFeeTransactionRepository;
    private readonly IEntityMappingRepository<DailyContractCallIndex, string> _dailyContractCallRepository;
    private readonly IEntityMappingRepository<DailyTotalContractCallIndex, string> _dailyTotalContractCallRepository;

    private readonly IEntityMappingRepository<DailySupplyGrowthIndex, string> _dailySupplyGrowthIndexRepository;
    private readonly IEntityMappingRepository<DailyStakedIndex, string> _dailyStakedIndexRepository;
    private readonly IEntityMappingRepository<DailyVotedIndex, string> _dailyVotedIndexRepository;
    private readonly IEntityMappingRepository<DailyWithDrawnIndex, string> _dailyWithDrawnIndexRepository;
    private readonly IEntityMappingRepository<TransactionErrInfoIndex, string> _transactionErrInfoIndexRepository;
    private readonly IEntityMappingRepository<DailySupplyChange, string> _dailySupplyChangeRepository;
    private readonly IEntityMappingRepository<DailyTVLIndex, string> _dailyTVLIndexRepository;

    private readonly IEntityMappingRepository<MonthlyActiveAddressInfoIndex, string>
        _monthlyActiveAddressInfoRepository;

    private readonly IEntityMappingRepository<MonthlyActiveAddressIndex, string> _monthlyActiveAddressRepository;

    private readonly IEntityMappingRepository<LogEventIndex, string> _logEventRepository;
    private readonly IPriceServerProvider _priceServerProvider;
    private readonly IAwakenIndexerProvider _awakenIndexerProvider;
    private readonly IIndexerGenesisProvider _indexerGenesisProvider;


    private readonly IEntityMappingRepository<AddressIndex, string> _addressRepository;
    private readonly IEntityMappingRepository<MergeAddressIndex, string> _mergeAddressRepository;

    private readonly IEntityMappingRepository<DailyMergeUniqueAddressCountIndex, string>
        _dailyMergeUniqueAddressCountRepository;

    private readonly IDistributedCache<string> _cache;

    private readonly NodeProvider _nodeProvider;

    private readonly ILogger<TransactionService> _logger;
    private static bool FinishInitChartData = false;
    private static int BatchPullRoundCount = 5;
    private static int ContractListMax = 10000;
    private static int BlockSizeInterval = 1;
    private static long BpStakedAmount = 100000;
    private static int BatchUpdateMaxSize = 1000;
    private static object _lock = new object();
    private static long PullLogEventTransactionInterval = 100 - 1;
    private static Timer timer;
    private static long PullTransactioninterval = 500 - 1;


    public TransactionService(IOptions<RedisCacheOptions> optionsAccessor, AELFIndexerProvider aelfIndexerProvider,
        IOptionsMonitor<AELFIndexerOptions> aelfIndexerOptions,
        ILogger<TransactionService> logger, IObjectMapper objectMapper,
        IOptionsMonitor<GlobalOptions> blockChainOptions,
        HomePageProvider homePageProvider, IStorageProvider storageProvider,
        IOptionsMonitor<ElasticsearchOptions> options, BlockChainIndexerProvider blockChainIndexerProvider,
        IOptionsMonitor<PullTransactionChainIdsOptions> workerOptions,
        IEntityMappingRepository<RoundIndex, string> roundIndexRepository,
        IEntityMappingRepository<NodeBlockProduceIndex, string> nodeBlockProduceRepository,
        IEntityMappingRepository<DailyBlockProduceCountIndex, string> blockProduceRepository,
        IEntityMappingRepository<DailyBlockProduceDurationIndex, string> blockProduceDurationRepository,
        IEntityMappingRepository<DailyCycleCountIndex, string> cycleCountRepository,
        IEntityMappingRepository<TransactionIndex, string> transactionIndexRepository,
        IEntityMappingRepository<ElfPriceIndex, string> priceRepository,
        IEntityMappingRepository<DailyAvgTransactionFeeIndex, string> avgTransactionFeeRepository,
        IEntityMappingRepository<DailyAvgBlockSizeIndex, string> avgBlockSizeRepository,
        IEntityMappingRepository<DailyBlockRewardIndex, string> blockRewardRepository,
        IEntityMappingRepository<DailyTotalBurntIndex, string> totalBurntRepository,
        IEntityMappingRepository<DailyDeployContractIndex, string> deployContractRepository,
        IEntityMappingRepository<DailyTransactionCountIndex, string> transactionCountRepository,
        IEntityMappingRepository<DailyUniqueAddressCountIndex, string> uniqueAddressRepository,
        IEntityMappingRepository<DailyActiveAddressCountIndex, string> activeAddressRepository,
        IEntityMappingRepository<DailyTransactionRecordIndex, string> recordIndexRepository,
        NodeProvider nodeProvide,
        IEntityMappingRepository<DailyAvgBlockSizeIndex, string> blockSizeRepository,
        IEntityMappingRepository<AddressIndex, string> addressRepository,
        IEntityMappingRepository<DailyHasFeeTransactionIndex, string> hasFeeTransactionRepository,
        IEntityMappingRepository<DailyContractCallIndex, string> dailyContractCallRepository,
        IEntityMappingRepository<DailyTotalContractCallIndex, string> dailyTotalContractCallRepository,
        IEntityMappingRepository<DailySupplyGrowthIndex, string> dailySupplyGrowthIndexRepository,
        IEntityMappingRepository<DailyStakedIndex, string> dailyStakedIndexRepository,
        IEntityMappingRepository<DailyVotedIndex, string> dailyVotedIndexRepository,
        IEntityMappingRepository<TransactionErrInfoIndex, string> transactionErrInfoIndexRepository,
        IEntityMappingRepository<DailySupplyChange, string> dailySupplyChangeRepository,
        IEntityMappingRepository<DailyTVLIndex, string> dailyTVLIndexRepository,
        IAwakenIndexerProvider awakenIndexerProvider,
        IIndexerGenesisProvider indexerGenesisProvider,
        IEntityMappingRepository<DailyWithDrawnIndex, string> dailyWithDrawnIndexRepository,
        IEntityMappingRepository<LogEventIndex, string> logEventRepository,
        IOptionsMonitor<SecretOptions> secretOptions,
        IEntityMappingRepository<MonthlyActiveAddressInfoIndex, string> monthlyActiveAddressInfoRepository,
        IEntityMappingRepository<MonthlyActiveAddressIndex, string> monthlyActiveAddressRepository,
        IEntityMappingRepository<MergeAddressIndex, string> mergeAddressRepository,
        IEntityMappingRepository<DailyMergeUniqueAddressCountIndex, string> dailyMergeUniqueAddressCountRepository,
        IDistributedCache<string> cache,
        IPriceServerProvider priceServerProvider) :
        base(optionsAccessor)
    {
        _aelfIndexerProvider = aelfIndexerProvider;
        _aelfIndexerOptions = aelfIndexerOptions;
        _logger = logger;
        _objectMapper = objectMapper;

        _globalOptions = blockChainOptions;
        _homePageProvider = homePageProvider;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _storageProvider = storageProvider;
        var uris = options.CurrentValue.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        _elasticClient = new ElasticClient(settings);
        _workerOptions = workerOptions;
        _roundIndexRepository = roundIndexRepository;
        _nodeBlockProduceRepository = nodeBlockProduceRepository;
        _blockProduceRepository = blockProduceRepository;
        _blockProduceDurationRepository = blockProduceDurationRepository;
        _cycleCountRepository = cycleCountRepository;
        _transactionIndexRepository = transactionIndexRepository;
        _priceRepository = priceRepository;
        _logEventRepository = logEventRepository;
        EsIndex.SetElasticClient(_elasticClient);
        _avgTransactionFeeRepository = avgTransactionFeeRepository;
        _avgBlockSizeRepository = avgBlockSizeRepository;
        _blockRewardRepository = blockRewardRepository;
        _totalBurntRepository = totalBurntRepository;
        _deployContractRepository = deployContractRepository;
        _transactionCountRepository = transactionCountRepository;
        _uniqueAddressRepository = uniqueAddressRepository;
        _activeAddressRepository = activeAddressRepository;
        _recordIndexRepository = recordIndexRepository;
        _blockSizeRepository = blockSizeRepository;
        _nodeProvider = nodeProvide;
        _addressRepository = addressRepository;
        _hasFeeTransactionRepository = hasFeeTransactionRepository;
        _dailyContractCallRepository = dailyContractCallRepository;
        _dailyTotalContractCallRepository = dailyTotalContractCallRepository;
        _dailySupplyGrowthIndexRepository = dailySupplyGrowthIndexRepository;
        _awakenIndexerProvider = awakenIndexerProvider;
        _priceServerProvider = priceServerProvider;
        _dailyStakedIndexRepository = dailyStakedIndexRepository;
        _dailyVotedIndexRepository = dailyVotedIndexRepository;
        _transactionErrInfoIndexRepository = transactionErrInfoIndexRepository;
        _dailySupplyChangeRepository = dailySupplyChangeRepository;
        _dailyTVLIndexRepository = dailyTVLIndexRepository;
        _indexerGenesisProvider = indexerGenesisProvider;
        _logEventRepository = logEventRepository;
        _dailyWithDrawnIndexRepository = dailyWithDrawnIndexRepository;
        _secretOptions = secretOptions.CurrentValue;
        _monthlyActiveAddressInfoRepository = monthlyActiveAddressInfoRepository;
        _monthlyActiveAddressRepository = monthlyActiveAddressRepository;
        _mergeAddressRepository = mergeAddressRepository;
        _dailyMergeUniqueAddressCountRepository = dailyMergeUniqueAddressCountRepository;
        _cache = cache;
    }


    public async Task MergeAddress()
    {
        var key = "address_date";

        await ConnectAsync();
        try
        {
            while (true)
            {
                await Task.Delay(3000);
                var mergeUniqueAddressInsertList = new List<DailyMergeUniqueAddressCountIndex>();
                var updateDate = await _cache.GetAsync(key);
                if (updateDate.IsNullOrEmpty())
                {
                    updateDate = _globalOptions.CurrentValue.AddressStartDate;
                }

                var nowDay = DateTime.Now.ToString("yyyy-MM-dd");
                if (nowDay == updateDate) break;

                var updateDateLong = DateTimeHelper.ParseDateToLong(updateDate);
                var queryableAsync = await _dailyMergeUniqueAddressCountRepository.GetQueryableAsync();
                var queryable = await _addressRepository.GetQueryableAsync();
                var nowAddressIndexList = queryable.Where(c => c.Date == updateDate)
                    .Take(10000).ToList();

                var beforeMergeAddressCountList = await GetBeforeMergeAddressCount(queryableAsync, updateDate);

                if (nowAddressIndexList.IsNullOrEmpty())
                {
                    ProcessNoNewAddress(beforeMergeAddressCountList, mergeUniqueAddressInsertList, updateDateLong,
                        updateDate);
                    await SaveResults(mergeUniqueAddressInsertList, null, updateDate, key);
                    continue;
                }

                var (mainChainUniqueAddressCount, sideChainUniqueAddressCount, mergeAddressIndices) =
                    ProcessNewAddresses(nowAddressIndexList, updateDateLong);

                mergeUniqueAddressInsertList.AddRange(await GetMergeAddressCounts(mainChainUniqueAddressCount,
                    sideChainUniqueAddressCount, updateDateLong, updateDate, beforeMergeAddressCountList));

                await SaveResults(mergeUniqueAddressInsertList, mergeAddressIndices, updateDate, key);
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"MergeAddress error: {e}");
        }
    }

    private async Task<List<DailyMergeUniqueAddressCountIndex>> GetBeforeMergeAddressCount(
        IQueryable<DailyMergeUniqueAddressCountIndex> queryableAsync, string updateDate)
    {
        var beforeDate = DateTimeHelper.GetBeforeDayDate(updateDate);
        var result = queryableAsync.Where(c => c.DateStr == beforeDate)
            .Take(2).ToList();
        if (result.IsNullOrEmpty())
        {
            result = queryableAsync.Where(c => c.DateStr == updateDate)
                .Take(2).ToList();
        }

        return result;
    }

    private void ProcessNoNewAddress(List<DailyMergeUniqueAddressCountIndex> beforeMergeAddressCountList,
        List<DailyMergeUniqueAddressCountIndex> mergeUniqueAddressInsertList, long updateDateLong, string updateDate)
    {
        foreach (var addressInfo in beforeMergeAddressCountList)
        {
            mergeUniqueAddressInsertList.Add(new DailyMergeUniqueAddressCountIndex()
            {
                Date = updateDateLong,
                ChainId = addressInfo.ChainId,
                DateStr = updateDate,
                AddressCount = addressInfo.TotalUniqueAddressees == 0 ? 0 : addressInfo.TotalUniqueAddressees,
                TotalUniqueAddressees = addressInfo.TotalUniqueAddressees
            });
        }
    }


    private (int mainChainUniqueAddressCount, int sideChainUniqueAddressCount, List<MergeAddressIndex>
        list)
        ProcessNewAddresses(List<AddressIndex> nowAddressIndexList, long updateDateLong)
    {
        var sideNewAddressCount = nowAddressIndexList.Where(c => c.ChainId == _globalOptions.CurrentValue.SideChainId)
            .ToList();


        var mainFilterNewAddress = FilterNewAddress(nowAddressIndexList.Select(c => c.Address).Distinct().ToList(),
            updateDateLong).Result;

        var mainChainUniqueAddressCount = mainFilterNewAddress.Count;
        var sideChainUniqueAddressCount = sideNewAddressCount.Count;

        var mergeAddressIndices = new List<MergeAddressIndex>();
        foreach (var addressIndex in sideNewAddressCount)
        {
            mergeAddressIndices.Add(new MergeAddressIndex
            {
                Address = addressIndex.Address,
                Date = updateDateLong,
                ChainId = _globalOptions.CurrentValue.SideChainId
            });
        }

        foreach (var address in mainFilterNewAddress)
        {
            mergeAddressIndices.Add(new MergeAddressIndex
            {
                Address = address,
                Date = updateDateLong,
                ChainId = "AELF"
            });
        }


        return (mainChainUniqueAddressCount, sideChainUniqueAddressCount, mergeAddressIndices);
    }

    private async Task<List<string>> FilterNewAddress(List<string> nowAddressIndexList, long date)
    {
        var mergeAddressIndexList = await GetMergeAddressIndexList(nowAddressIndexList, date);

        var mergeAddressIndexDic =
            mergeAddressIndexList.Select(c => c.Address).Distinct().ToDictionary(c => c, c => c);

        var filterAddressList = new List<string>();
        foreach (var address in nowAddressIndexList)
        {
            if (!mergeAddressIndexDic.ContainsKey(address))
            {
                filterAddressList.Add(address);
            }
        }

        return filterAddressList;
    }

    private async Task<List<DailyMergeUniqueAddressCountIndex>> GetMergeAddressCounts(int mainChainCount,
        int sideChainCount, long updateDateLong, string updateDate,
        List<DailyMergeUniqueAddressCountIndex> beforeMergeAddressCountList)
    {
        var result = new List<DailyMergeUniqueAddressCountIndex>
        {
            await GetMergeAddressIndex(mainChainCount, "AELF", updateDateLong, updateDate, beforeMergeAddressCountList),
            await GetMergeAddressIndex(sideChainCount, _globalOptions.CurrentValue.SideChainId, updateDateLong,
                updateDate, beforeMergeAddressCountList)
        };
        return result;
    }

    private async Task SaveResults(List<DailyMergeUniqueAddressCountIndex> mergeUniqueAddressInsertList,
        List<MergeAddressIndex> newAddressInsertList, string updateDate, string key)
    {
        if (!mergeUniqueAddressInsertList.IsNullOrEmpty())
        {
            await _dailyMergeUniqueAddressCountRepository.AddOrUpdateManyAsync(mergeUniqueAddressInsertList);
        }

        if (!newAddressInsertList.IsNullOrEmpty())
        {
            await _mergeAddressRepository.AddOrUpdateManyAsync(newAddressInsertList);
            _logger.LogInformation($"MergeAddress date {updateDate}: new addresses: {newAddressInsertList.Count}");
        }

        await _cache.SetAsync(key, DateTimeHelper.GetAfterDayDate(updateDate), new DistributedCacheEntryOptions
            { SlidingExpiration = null, AbsoluteExpiration = DateTimeOffset.MaxValue });
    }


    // public async Task MergeAddress()
    // {
    //     var key = "address_date";
    //
    //     await ConnectAsync();
    //     try
    //     {
    //         while (true)
    //         {
    //             await Task.Delay(3 * 1000);
    //             var mergeUniqueAddressInsertList = new List<DailyMergeUniqueAddressCountIndex>();
    //             var updateDate = RedisDatabase.StringGet(key);
    //
    //             if (updateDate.IsNullOrEmpty)
    //             {
    //                 updateDate = _globalOptions.CurrentValue.AddressStartDate;
    //             }
    //
    //             var nowDay = DateTime.Now.ToString("yyyy-MM-dd");
    //             if (nowDay == updateDate)
    //             {
    //                 break;
    //             }
    //
    //             var updateDateLong = DateTimeHelper.ParseDateToLong(updateDate);
    //             var queryableAsync = await _dailyMergeUniqueAddressCountRepository.GetQueryableAsync();
    //             var beforeMergeAddressCountList =
    //                 queryableAsync.Where(c => c.DateStr == DateTimeHelper.GetBeforeDayDate(updateDate)).Take(2)
    //                     .ToList();
    //
    //             var queryable = await _addressRepository.GetQueryableAsync();
    //             var nowAddressIndexList = queryable.Where(c => c.Date == updateDate.ToString()).Take(10000).ToList();
    //             if (nowAddressIndexList.IsNullOrEmpty())
    //             {
    //                 if (beforeMergeAddressCountList.IsNullOrEmpty())
    //                 {
    //                     beforeMergeAddressCountList =
    //                         queryableAsync.Where(c => c.DateStr == updateDate).Take(2).ToList();
    //                     foreach (var addressInfo in beforeMergeAddressCountList)
    //                     {
    //                         mergeUniqueAddressInsertList.Add(
    //                             new DailyMergeUniqueAddressCountIndex()
    //                             {
    //                                 Date = updateDateLong,
    //                                 ChainId = addressInfo.ChainId,
    //                                 DateStr = updateDate,
    //                                 AddressCount = addressInfo.TotalUniqueAddressees,
    //                                 TotalUniqueAddressees = addressInfo.TotalUniqueAddressees
    //                             });
    //                     }
    //                 }
    //                 else
    //                 {
    //                     foreach (var addressInfo in beforeMergeAddressCountList)
    //                     {
    //                         mergeUniqueAddressInsertList.Add(
    //                             new DailyMergeUniqueAddressCountIndex()
    //                             {
    //                                 Date = updateDateLong,
    //                                 ChainId = addressInfo.ChainId,
    //                                 DateStr = updateDate,
    //                                 AddressCount = 0,
    //                                 TotalUniqueAddressees = addressInfo.TotalUniqueAddressees
    //                             });
    //                     }
    //                 }
    //
    //
    //                 if (!mergeUniqueAddressInsertList.IsNullOrEmpty())
    //                 {
    //                     await _dailyMergeUniqueAddressCountRepository
    //                         .AddOrUpdateManyAsync(mergeUniqueAddressInsertList);
    //                 }
    //
    //                 _logger.LogInformation($"MergeAddress date{updateDate} no new address");
    //                 var afterDayDate = DateTimeHelper.GetAfterDayDate(updateDate);
    //                 RedisDatabase.StringSet(key, afterDayDate);
    //                 continue;
    //             }
    //
    //
    //             var nowAddressList = nowAddressIndexList.Select(c => c.Address).Distinct().ToList();
    //             var mergeAddressIndexList = await GetMergeAddressIndexList(nowAddressList, updateDateLong);
    //
    //             var dictionary = new Dictionary<string, MergeAddressIndex>();
    //             var mergeAddressIndexDic =
    //                 mergeAddressIndexList.Select(c => c.Address).Distinct().ToDictionary(c => c, c => c);
    //
    //             var filterAddressList = new List<AddressIndex>();
    //             foreach (var index in nowAddressIndexList)
    //             {
    //                 if (!mergeAddressIndexDic.ContainsKey(index.Address))
    //                 {
    //                     filterAddressList.Add(index);
    //                 }
    //             }
    //
    //
    //             var mainChainUniqueAddressCount = 0;
    //             var sideChainUniqueAddressCount = 0;
    //             if (!filterAddressList.IsNullOrEmpty())
    //             {
    //                 // all address is new 
    //                 foreach (var index in filterAddressList)
    //                 {
    //                     if (index.ChainId == "AELF")
    //                     {
    //                         mainChainUniqueAddressCount++;
    //                     }
    //                     else
    //                     {
    //                         sideChainUniqueAddressCount++;
    //                     }
    //
    //                     dictionary[index.Address + index.ChainId] = new MergeAddressIndex()
    //                     {
    //                         Address = index.Address,
    //                         Date = updateDateLong,
    //                         ChainId = index.ChainId
    //                     };
    //
    //                     if (index.ChainId == _globalOptions.CurrentValue.SideChainId)
    //                     {
    //                         if (!dictionary.ContainsKey(index.Address + "AELF"))
    //                         {
    //                             mainChainUniqueAddressCount++;
    //                         }
    //
    //                         dictionary[index.Address + "AELF"] = new MergeAddressIndex()
    //                         {
    //                             Address = index.Address,
    //                             Date = updateDateLong,
    //                             ChainId = "AELF"
    //                         };
    //                     }
    //                 }
    //             }
    //
    //
    //             mergeUniqueAddressInsertList.Add(await GetMergeAddressIndex(mainChainUniqueAddressCount, "AELF",
    //                 updateDateLong, updateDate,
    //                 beforeMergeAddressCountList));
    //             mergeUniqueAddressInsertList.Add(await GetMergeAddressIndex(sideChainUniqueAddressCount,
    //                 _globalOptions.CurrentValue.SideChainId,
    //                 updateDateLong, updateDate,
    //                 beforeMergeAddressCountList));
    //
    //             await _dailyMergeUniqueAddressCountRepository.AddOrUpdateManyAsync(mergeUniqueAddressInsertList);
    //             var newAddressInsertList = dictionary.Values.ToList();
    //             if (!newAddressInsertList.IsNullOrEmpty())
    //             {
    //                 await _mergeAddressRepository.AddOrUpdateManyAsync(newAddressInsertList);
    //             }
    //
    //             RedisDatabase.StringSet(key, DateTimeHelper.GetAfterDayDate(updateDate));
    //             _logger.LogInformation($"MergeAddress date{updateDate}  new address:{newAddressInsertList.Count}");
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError($"MergeAddress err:{e}");
    //     }
    // }


    public async Task<DailyMergeUniqueAddressCountIndex> GetMergeAddressIndex(int count, string chainId, long date,
        string dateSting,
        List<DailyMergeUniqueAddressCountIndex> list)
    {
        var dailyMergeUniqueAddressCountIndex = new DailyMergeUniqueAddressCountIndex()
        {
            ChainId = chainId,
            Date = date,
            DateStr = dateSting,
            AddressCount = count,
            TotalUniqueAddressees = count
        };

        var addressList = list.Where(c => c.ChainId == chainId).ToList();

        if (!addressList.IsNullOrEmpty())
        {
            dailyMergeUniqueAddressCountIndex.TotalUniqueAddressees += addressList.First().TotalUniqueAddressees;
        }

        return dailyMergeUniqueAddressCountIndex;
    }

    public async Task BlockSizeTask()
    {
        _logger.LogInformation("start BlockSizeTask");

        var tasks = new List<Task>();

        foreach (var chanId in _globalOptions.CurrentValue.ChainIds)
        {
            tasks.Add(BatchPullBlockSize(chanId));
        }

        await tasks.WhenAll();
    }


    public async Task BatchPullLogEventTask()
    {
        foreach (var v in _globalOptions.CurrentValue.LogEventStartBlockHeightInit)
        {
            await ConnectAsync();
            RedisDatabase.StringSet(RedisKeyHelper.LogEventTransactionLastBlockHeight(v.Key), v.Value);
            _logger.LogInformation("Init log event {logEventKey},{logEventValue}", v.Key, v.Value);
        }

        var tasks = new List<Task>();
        foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
        {
            tasks.Add(BatchParseLogEventJob(chainId));
        }

        await tasks.WhenAll();
    }

    public async Task DelLogEventTask()
    {
        foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
        {
            var getContractListResult =
                await _indexerGenesisProvider.GetContractListAsync(chainId,
                    0, 100, "", "", "");
            var list = getContractListResult.ContractList.Items.Select(s => s.Address).ToList();
            var tasks = new List<Task>();
            foreach (var address in list)
            {
                tasks.Add(DeleteManyEvent(chainId, address));
            }

            await tasks.WhenAll();
        }
    }

    public async Task DeleteManyEvent(string chainId, string contractAddress)
    {
        var searchResponse = _elasticClient.Search<LogEventIndex>(s => s
                .Index("logeventindex").Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term("chainId", chainId),
                            m => m.Term("toAddress", contractAddress)
                        )
                    )
                )
                .Sort(ss => ss
                    .Descending("timeStamp")
                )
                .From(ContractListMax + 1)
                .Size(1) // 
        );

        long starblockTime;
        if (searchResponse.IsValid)
        {
            if (searchResponse.Documents.Count > 0)
            {
                starblockTime = searchResponse.Documents.ToList().First().TimeStamp;
            }
            else
            {
                return;
            }
        }
        else
        {
            _logger.LogError("Error: {Reason}", searchResponse.ServerError.Error.Reason);
            return;
        }


        var resp = await _elasticClient.DeleteByQueryAsync<LogEventIndex>(q => q
            .Index("logeventindex")
            .Query(qb => qb
                .Bool(bb => bb
                    .Must(
                        m => m.Term("chainId", chainId),
                        m => m.Term("toAddress", contractAddress),
                        m => m.Range(r => r.Field(f => f.TimeStamp).LessThanOrEquals(starblockTime)
                        )
                    )
                )
            ).Size(4000)
        );
        if (resp.Deleted > 0)
        {
            _logger.LogInformation("delete contract event:{chainId},{contractAddress},{isDeleted}", chainId,
                contractAddress, resp.Deleted);
        }
    }

    public async Task UpdateMonthlyActiveAddress()
    {
        try
        {
            foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
            {
                var needFindDateMonth = await GetNeedFindDateMonth(chainId);

                var batchUpdate = await GetMonthData(needFindDateMonth, chainId);
                if (!batchUpdate.IsNullOrEmpty())
                {
                    await _monthlyActiveAddressRepository.AddOrUpdateManyAsync(batchUpdate);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("UpdateMonthlyActiveAddress:{e}", e.ToString());
        }
    }


    public async Task<int> GetNeedFindDateMonth(string chainId)
    {
        var needFindDateMonth = 0;
        var activeAddressIndices = _monthlyActiveAddressRepository.GetQueryableAsync().Result
            .Where(c => c.ChainId == chainId).OrderByDescending(c => c.DateMonth).Take(1);
        var infoQuery = _monthlyActiveAddressInfoRepository.GetQueryableAsync().Result
            .Where(c => c.ChainId == chainId);

        var batchUpdate = new List<MonthlyActiveAddressIndex>();
        if (activeAddressIndices.IsNullOrEmpty())
        {
            _logger.LogInformation("UpdateMonthlyActiveAddress first");
            var list = infoQuery
                .OrderBy(c => c.DateMonth).Take(1);

            if (list.IsNullOrEmpty())
            {
                return 0;
            }

            needFindDateMonth = list.First().DateMonth;

            return needFindDateMonth;
        }
        else
        {
            needFindDateMonth = activeAddressIndices.First().DateMonth;
            needFindDateMonth = DateTimeHelper.GetNextYYMMDD(needFindDateMonth);
        }

        return needFindDateMonth;
    }


    public async Task<List<MonthlyActiveAddressIndex>> GetMonthData(int needFindDateMonth, string chainId)
    {
        var query = _monthlyActiveAddressInfoRepository.GetQueryableAsync().Result
            .Where(c => c.ChainId == chainId);
        var infoQuery = _monthlyActiveAddressInfoRepository.GetQueryableAsync().Result
            .Where(c => c.ChainId == chainId);

        var monthlyActiveAddressInfoIndices = infoQuery.OrderByDescending(c => c.DateMonth).Take(1).ToList();

        if (monthlyActiveAddressInfoIndices.IsNullOrEmpty())
        {
            return null;
        }

        var maxMonth = monthlyActiveAddressInfoIndices.First().DateMonth;

        var list = new List<MonthlyActiveAddressIndex>();
        while (needFindDateMonth < maxMonth)
        {
            _logger.LogInformation(
                "UpdateMonthlyActiveAddress needFindDateMonth:{needFindDateMonth},maxMonth:{maxMonth}",
                needFindDateMonth,
                maxMonth);
            var count = await GetMonthlyActiveAddressCount(needFindDateMonth, chainId);
            var sendCount = query.Where(c => c.DateMonth == needFindDateMonth).Where(c => c.Type == "from")
                .Count();
            var toCount = query.Where(c => c.DateMonth == needFindDateMonth).Where(c => c.Type == "to")
                .Count();

            list.Add(new MonthlyActiveAddressIndex()
            {
                ChainId = chainId,
                DateMonth = needFindDateMonth,
                AddressCount = count,
                SendAddressCount = sendCount,
                ReceiveAddressCount = toCount
            });

            needFindDateMonth = DateTimeHelper.GetNextYYMMDD(needFindDateMonth);
        }

        return list;
    }

    public async Task<long> GetMonthlyActiveAddressCount(int month, string chainId)
    {
        try
        {
            var searchResponse = _elasticClient.Search<MonthlyActiveAddressInfoIndex>(s => s
                .Index("monthlyactiveaddressinfoindex").Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field("dateMonth").Value(month)),
                            m => m.Term(t => t.Field("chainId").Value(chainId))
                        )
                    )
                )
                .Aggregations(a => a
                    .Cardinality("unique_addresses", t => t.Field("address"))
                )
            );

            var uniqueAddressesCount = searchResponse.Aggregations.Cardinality("unique_addresses").Value;
            return (int)uniqueAddressesCount;
        }
        catch (Exception e)
        {
            _logger.LogError("GetMonthlyActiveAddressCount {e}", e.ToString());
        }

        return 0;
    }

    public async Task BatchPullBlockSize(string chainId)
    {
        var dic = new Dictionary<string, DailyAvgBlockSizeIndex>();
        await ConnectAsync();
        var redisValue = RedisDatabase.StringGet(RedisKeyHelper.BlockSizeLastBlockHeight(chainId));
        var lastBlockHeight = redisValue.IsNullOrEmpty ? 0 : long.Parse(redisValue);
        while (true)
        {
            var tasks = new List<Task>();
            var blockSizeIndices = new List<BlockSizeDto>();
            var _lock = new object();
            int failCount = 0;
            var startNew = Stopwatch.StartNew();
            try
            {
                for (int i = 0; i < BlockSizeInterval; i++)
                {
                    lastBlockHeight++;
                    tasks.Add(_nodeProvider.GetBlockSize(chainId, lastBlockHeight).ContinueWith(task =>
                    {
                        lock (_lock)
                        {
                            if (task.Result != null)
                            {
                                blockSizeIndices.Add(task.Result);
                            }
                        }
                    }));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "BatchPullBlockSize err:{c},err msg:{e},startBlockHeight:{s1},endBlockHeight:{s2}",
                    chainId, e, lastBlockHeight - BlockSizeInterval,
                    lastBlockHeight);
                break;
            }

            await tasks.WhenAll();
            foreach (var blockSize in blockSizeIndices)
            {
                if (blockSize.Header == null)
                {
                    _logger.LogInformation("Block size index header is null:{chainId}", chainId);
                    failCount++;
                    if (failCount == 10)
                    {
                        return;
                    }

                    continue;
                }


                string date = "";
                if (long.Parse(blockSize.Header.Height) == 1)
                {
                    date = _globalOptions.CurrentValue.OneBlockTime[chainId];
                }
                else
                {
                    date = DateTimeHelper.FormatDateStr(blockSize.Header.Time);

                    if (date == DateTimeHelper.GetDateStr(DateTime.UtcNow))
                    {
                        break;
                    }
                }

                if (dic.TryGetValue(date, out var v))
                {
                    v.TotalSize += blockSize.BlockSize;
                    v.EndBlockHeight = Math.Max(int.Parse(blockSize.Header.Height), v.EndBlockHeight);
                    v.StartBlockHeight = Math.Min(int.Parse(blockSize.Header.Height), v.StartBlockHeight);
                    v.BlockCount++;
                }
                else
                {
                    dic[date] = new DailyAvgBlockSizeIndex()
                    {
                        ChainId = chainId,
                        DateStr = date,
                        TotalSize = blockSize.BlockSize,
                        StartBlockHeight = int.Parse(blockSize.Header.Height),
                        StartTime = DateTime.UtcNow,
                        EndBlockHeight = int.Parse(blockSize.Header.Height),
                        BlockCount = 1
                    };
                }
            }

            startNew.Stop();
            _logger.LogInformation(
                "BatchPullBlockSize :{chainId},count:{count},time:{costTime},startBlockHeight:{startBlockHeight},endBlockHeight:{endBlockHeight}",
                chainId, blockSizeIndices.Count, startNew.Elapsed.TotalSeconds, lastBlockHeight - BlockSizeInterval,
                lastBlockHeight);
            if (dic.Count >= 2)
            {
                var sizeIndices = dic.Values.OrderBy(c => c.DateStr).ToList();

                var blockSizeIndex = sizeIndices[0];
                blockSizeIndex.AvgBlockSize = (blockSizeIndex.TotalSize / blockSizeIndex.BlockCount).ToString();
                blockSizeIndex.EndTime = DateTime.UtcNow;
                blockSizeIndex.Date = DateTimeHelper.ConvertYYMMDD(blockSizeIndex.DateStr);
                await _blockSizeRepository.AddOrUpdateAsync(sizeIndices[0]);
                RedisDatabase.StringSet(RedisKeyHelper.BlockSizeLastBlockHeight(chainId),
                    sizeIndices[0].EndBlockHeight);
                dic.Remove(blockSizeIndex.DateStr);
            }

            lastBlockHeight++;
        }
    }


    public async Task UpdateElfPrice()
    {
        try
        {
            var market = new Market();
            var data1 =
                await market.KlineCandlestickData("ELFUSDT", Interval.ONE_DAY, 1631030400000, 1662566400000);


            var data2 =
                await market.KlineCandlestickData("ELFUSDT", Interval.ONE_DAY, 1662566400000, 1694102400000);

            var data3 =
                await market.KlineCandlestickData("ELFUSDT", Interval.ONE_DAY, 1694102400000, 1725724800000);

            List<string[]> dataList1 = JsonConvert.DeserializeObject<List<string[]>>(data1);

            List<string[]> dataList2 = JsonConvert.DeserializeObject<List<string[]>>(data2);

            List<string[]> dataList3 = JsonConvert.DeserializeObject<List<string[]>>(data3);


            var dataList = dataList1.Concat(dataList2).Concat(dataList3).ToList();

            var batch = new List<ElfPriceIndex>();
            foreach (var strings in dataList)
            {
                var elfPriceIndex = new ElfPriceIndex()
                {
                    OpenTime = long.Parse(strings[0]),
                    Open = strings[1],
                    High = strings[2],
                    Low = strings[3],
                    Close = strings[3],
                };
                elfPriceIndex.DateStr = DateTimeHelper.GetDateTimeString(elfPriceIndex.OpenTime);
                batch.Add(elfPriceIndex);
            }


            await _priceRepository.AddOrUpdateManyAsync(batch);
        }
        catch (Exception e)
        {
            _logger.LogError("UpdateElfPrice err:{e}", e);
        }
    }


    public async Task BatchPullTransactionTask()
    {
        await ConnectAsync();

        if (_globalOptions.CurrentValue.NeedInitLastHeight && !FinishInitChartData)
        {
            foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
            {
                await ConnectAsync();
                RedisDatabase.KeyDelete(RedisKeyHelper.TransactionLastBlockHeight(chainId));
            }

            FinishInitChartData = true;
        }

        var tasks = new List<Task>();
        foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
        {
            tasks.Add(BatchPullTransactionJob(chainId));
        }

        await tasks.WhenAll();
    }


    public async Task FixDailyData()
    {
        await ConnectAsync();
        var redisValue = RedisDatabase.StringGet(RedisKeyHelper.FixDailyData());
        if (redisValue.IsNullOrEmpty)
        {
            _logger.LogInformation("No fix data");
            return;
        }

        var fixDailyData = JsonConvert.DeserializeObject<FixDailyData>(redisValue);
        var queryable = await _recordIndexRepository.GetQueryableAsync();
        foreach (var keyValuePair in fixDailyData.FixDate)
        {
            var chainId = keyValuePair.Key;
            queryable = queryable.Where(c => c.ChainId == chainId);

            foreach (var date in keyValuePair.Value)
            {
                var recordList = queryable.Where(c => c.DateStr == date).Take(1).ToList();
                if (recordList.IsNullOrEmpty())
                {
                    continue;
                }

                var record = recordList.First();

                var startBlockHeight = record.StartBlockHeight;
                await FixDailyDataByStartBlockHeight(chainId, startBlockHeight);
            }
        }

        await ConnectAsync();
        RedisDatabase.KeyDelete(RedisKeyHelper.FixDailyData());
    }


    public async Task FixDailyDataByStartBlockHeight(string chainId, long startBlockHeight)
    {
        var dic = new Dictionary<string, DailyTransactionsChartSet>();
        while (true)
        {
            try
            {
                var batchTransactionList =
                    await GetBatchTransactionList(chainId, startBlockHeight,
                        startBlockHeight + PullTransactioninterval);
                if (batchTransactionList.IsNullOrEmpty())
                {
                    continue;
                }

                batchTransactionList = batchTransactionList.OrderBy(c => c.BlockHeight).Select(s =>
                {
                    var totalMilliseconds = DateTimeHelper.GetTotalMilliseconds(s.BlockTime);
                    if (totalMilliseconds == 0 && s.BlockHeight == 1)
                    {
                        s.DateStr = _globalOptions.CurrentValue.OneBlockTime[chainId];
                        s.BlockTime =
                            DateTimeHelper.GetDateTimeFromYYMMDD(_globalOptions.CurrentValue.OneBlockTime[chainId]);
                    }
                    else
                    {
                        s.DateStr = DateTimeHelper.GetDateStr(s.BlockTime);
                    }

                    return s;
                }).ToList();


                _logger.LogInformation("Fix daily data:{chainId},start:{startBlockHeight}", chainId, startBlockHeight);
                var updateDailyTransactionData = await UpdateDailyTransactionData(batchTransactionList, chainId, dic);

                if (updateDailyTransactionData != null)
                {
                    _logger.LogInformation("Fix daily data finished:{chainId},{dateStr}", chainId,
                        updateDailyTransactionData.DateStr);
                    break;
                }

                startBlockHeight += PullTransactioninterval + 1;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Fix daily data err:{chainId}", chainId);
                break;
            }
        }
    }

    public async Task BatchParseLogEventJob(string chainId)
    {
        var lastBlockHeight = 0l;
        await ConnectAsync();
        var redisValue = RedisDatabase.StringGet(RedisKeyHelper.LogEventTransactionLastBlockHeight(chainId));
        lastBlockHeight = redisValue.IsNullOrEmpty ? 2 : long.Parse(redisValue) + 1;
        _logger.LogInformation(
            "BatchParseLogEventJob {ChainId} lastBlockHeight {LastBlockHeight} PullLogEventTransactionInterval {PullLogEventTransactionInterval}",
            chainId, lastBlockHeight, PullLogEventTransactionInterval);
        while (true)
        {
            try
            {
                if (PullLogEventTransactionInterval != 0)
                {
                    var latestBlocksAsync = await _aelfIndexerProvider.GetLatestSummariesAsync(chainId);
                    if (lastBlockHeight >= latestBlocksAsync.First().LatestBlockHeight)
                    {
                        PullLogEventTransactionInterval = 0;
                    }

                    _logger.LogInformation(
                        "Set log event PullLogEventTransactionInterval to 0:{chainId},{lastBlockHeight}", chainId,
                        lastBlockHeight);
                }

                var batchTransactionList =
                    await GetBatchTransactionList(chainId, lastBlockHeight,
                        lastBlockHeight + PullLogEventTransactionInterval);


                if (batchTransactionList.IsNullOrEmpty())
                {
                    await Task.Delay(1000 * 1);
                    continue;
                }

                _logger.LogInformation("BatchParseLogEventJob :{chainId},start:{startBlockHeight}", chainId,
                    lastBlockHeight);
                await ParseLogEventList(batchTransactionList, chainId);
                lastBlockHeight += PullLogEventTransactionInterval + 1;
                RedisDatabase.StringSet(RedisKeyHelper.LogEventTransactionLastBlockHeight(chainId),
                    lastBlockHeight + PullLogEventTransactionInterval);
                await Task.Delay(1000 * 1);
            }

            catch (Exception e)
            {
                _logger.LogError(
                    "BatchParseLogEventJob err:{chainId},err msg:{msg},startBlockHeight:{startBlockHeight},endBlockHeight:{endBlockHeight}",
                    chainId,
                    e, lastBlockHeight,
                    lastBlockHeight + PullLogEventTransactionInterval);

                await Task.Delay(1000 * 2);
            }
        }
    }


    public async Task BatchPullTransactionJob(string chainId)
    {
        var dic = new Dictionary<string, DailyTransactionsChartSet>();

        var lastBlockHeight = 0l;
        await ConnectAsync();
        var redisValue = RedisDatabase.StringGet(RedisKeyHelper.TransactionLastBlockHeight(chainId));
        lastBlockHeight = redisValue.IsNullOrEmpty ? 1 : long.Parse(redisValue) + 1;

        while (true)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                var batchTransactionList =
                    await GetBatchTransactionList(chainId, lastBlockHeight, lastBlockHeight + PullTransactioninterval);

                if (batchTransactionList.IsNullOrEmpty())
                {
                    continue;
                }

                var dateSet = new HashSet<string>();
                batchTransactionList = batchTransactionList.OrderBy(c => c.BlockHeight).Select(s =>
                {
                    var totalMilliseconds = DateTimeHelper.GetTotalMilliseconds(s.BlockTime);
                    if (totalMilliseconds == 0 && s.BlockHeight == 1)
                    {
                        s.DateStr = _globalOptions.CurrentValue.OneBlockTime[chainId];
                        s.BlockTime =
                            DateTimeHelper.GetDateTimeFromYYMMDD(_globalOptions.CurrentValue.OneBlockTime[chainId]);
                    }
                    else
                    {
                        s.DateStr = DateTimeHelper.GetDateStr(s.BlockTime);
                    }

                    dateSet.Add(s.DateStr);
                    return s;
                }).ToList();

                if (dateSet.Min(c => c) == DateTimeHelper.GetDateStr(DateTime.UtcNow))
                {
                    break;
                }


                stopwatch.Stop();
                _logger.LogInformation(
                    "BatchPullTransactionTask:{dateList} end date:{chainId},count:{count},time:{costTime},startBlockHeight:{startBlockHeight},endBlockHeight:{endBlockHeight}",
                    dateSet.ToList(),
                    chainId, batchTransactionList.Count, stopwatch.Elapsed.TotalSeconds, lastBlockHeight,
                    lastBlockHeight + PullTransactioninterval);


                var updateDailyTransactionData = await UpdateDailyTransactionData(batchTransactionList, chainId, dic);

                if (updateDailyTransactionData != null)
                {
                    await ConnectAsync();
                    RedisDatabase.StringSet(RedisKeyHelper.TransactionLastBlockHeight(chainId),
                        updateDailyTransactionData.EndBlockHeight);
                    var dailyTransactionRecordIndex = new DailyTransactionRecordIndex()
                    {
                        ChainId = chainId,
                        StartBlockHeight = updateDailyTransactionData.StartBlockHeight,
                        EndBlockHeight = updateDailyTransactionData.EndBlockHeight,
                        DateStr = updateDailyTransactionData.DateStr,
                        StartTime = updateDailyTransactionData.StartTime,
                        WriteCostTime = updateDailyTransactionData.CostTime,
                        DataWriteFinishTime = updateDailyTransactionData.WirteFinishiTime,
                        Id = Guid.NewGuid().ToString()
                    };

                    await _recordIndexRepository.AddOrUpdateAsync(dailyTransactionRecordIndex);
                }

                lastBlockHeight += PullTransactioninterval + 1;
                await Task.Delay(1000);
            }

            catch (Exception e)
            {
                _logger.LogError(
                    "BatchPullTransactionTask err:{chainId},err msg:{msg},startBlockHeight:{startBlockHeight},endBlockHeight:{endBlockHeight}",
                    chainId,
                    e, lastBlockHeight,
                    lastBlockHeight + PullTransactioninterval);


                dic = new Dictionary<string, DailyTransactionsChartSet>();

                await ConnectAsync();
                redisValue = RedisDatabase.StringGet(RedisKeyHelper.TransactionLastBlockHeight(chainId));
                lastBlockHeight = redisValue.IsNullOrEmpty ? 1 : long.Parse(redisValue) + 1;
            }
        }
    }

    public async Task ParseLogEventList(List<TransactionData> transactionList, string chainId)
    {
        var logEventIndices = new List<LogEventIndex>();
        foreach (var txn in transactionList)
        {
            if (_globalOptions.CurrentValue.SkipContractAddress[chainId].Contains(txn.To) && txn.BlockHeight <
                _globalOptions.CurrentValue.SkipContractAddressStartBlockHeight[chainId])
            {
                continue;
            }

            for (var i = 0; i < txn.LogEvents.Count; i++)
            {
                var curEvent = txn.LogEvents[i];
                curEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
                curEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);
                var logEvent = new LogEventIndex()
                {
                    TransactionId = txn.TransactionId,
                    ChainId = chainId,
                    BlockHeight = txn.BlockHeight,
                    MethodName = curEvent.EventName,
                    BlockTime = txn.BlockTime,
                    TimeStamp = txn.BlockTime.ToUtcMilliSeconds(),
                    ToAddress = curEvent.ContractAddress,
                    ContractAddress = curEvent.ContractAddress,
                    EventName = curEvent.EventName,
                    NonIndexed = nonIndexed,
                    Indexed = indexed,
                    Index = i
                };
                logEventIndices.Add(logEvent);
            }
        }

        var total = logEventIndices.Count;

        while (logEventIndices.Count > 10000)
        {
            var eventIndices = logEventIndices.Take(10000).ToList();
            await _logEventRepository.AddOrUpdateManyAsync(eventIndices);
            logEventIndices = logEventIndices.Skip(10000).ToList();
        }

        if (!logEventIndices.IsNullOrEmpty())
        {
            await _logEventRepository.AddOrUpdateManyAsync(logEventIndices);
        }

        _logger.LogInformation("ParseLogEventList,insert:{chainId},{total}", chainId, total);
    }

    public async Task<DailyTransactionsChartSet> UpdateDailyTransactionData(List<TransactionData> transactionList,
        string chainId,
        Dictionary<string, DailyTransactionsChartSet> dic)
    {
        if (transactionList.IsNullOrEmpty())
        {
            _logger.LogInformation("Date str list is null:{chainId}", chainId);
            return null;
        }


        foreach (var transaction in transactionList)
        {
            var totalMilliseconds = DateTimeHelper.GetDateTotalMilliseconds(transaction.BlockTime);
            var date = DateTimeHelper.GetDateStr(transaction.BlockTime);

            var totalDeployCount = 0;
            var hasBurntBlockCount = 0;
            var totalBurnt = 0l;


            if (!dic.ContainsKey(date))
            {
                var dailyTransactionsChartSet =
                    new DailyTransactionsChartSet(chainId, totalMilliseconds, date);
                dailyTransactionsChartSet.StartTime = DateTime.UtcNow;
                dailyTransactionsChartSet.DateStr = date;
                dailyTransactionsChartSet.ChainId = chainId;
                dailyTransactionsChartSet.DateTimeStamp = totalMilliseconds;
                dic[date] = dailyTransactionsChartSet;
            }

            var dailyData = dic[date];
            var transactionFees = LogEventHelper.ParseTransactionFees(transaction.ExtraProperties);
            if (transactionFees > 0)
            {
                dailyData.TransactionFeeRecords.Add(transaction.TransactionId + "_" + transactionFees);
                dailyData.TotalFee += transactionFees;
                dailyData.DailyAvgTransactionFeeIndex.HasFeeTransactionCount++;
            }

            if (transaction.MethodName == "AnnounceElection" || transaction.MethodName == "AnnounceElectionFor")
            {
                dailyData.TotalBpStaked += BpStakedAmount;
            }
            else if (transaction.MethodName == "QuitElection")

            {
                dailyData.TotalBpStaked -= BpStakedAmount;
            }


            foreach (var txLogEvent in transaction.LogEvents)
            {
                var logEvent = LogEventHelper.ParseLogEventExtraProperties(txLogEvent.ExtraProperties);
                switch (txLogEvent.EventName)
                {
                    case nameof(ContractDeployed):
                        dailyData.DailyDeployContractIndex.Count++;
                        break;
                    case nameof(Burned):
                        var burned = new Burned();
                        burned.MergeFrom(logEvent);
                        await SetAddressSet(burned.Burner.ToBase58(), "", dailyData);
                        if (burned.Symbol == "ELF")
                        {
                            dailyData.DailyBurnt += burned.Amount;
                            await CalculateSupplyByAddress(burned.Burner.ToBase58(),
                                "", burned.Amount, dailyData, transaction.TransactionId);
                        }

                        break;
                    case nameof(Transferred):
                        var transferred = new Transferred();
                        transferred.MergeFrom(logEvent);
                        var from = transferred.From == null ? "" : transferred.From.ToBase58();
                        var to = transferred.To.ToBase58();
                        transferred.MergeFrom(logEvent);
                        await SetAddressSet(from, to, dailyData);
                        if (chainId == "AELF" && transferred.Symbol == "ELF")
                        {
                            await CalculateSupplyByAddress(from,
                                to, transferred.Amount, dailyData, transaction.TransactionId);
                        }

                        break;

                    case nameof(TransactionFeeCharged):
                        var transactionFeeCharged = new TransactionFeeCharged();
                        transactionFeeCharged.MergeFrom(logEvent);
                        var address = "";
                        if (transactionFeeCharged.ChargingAddress == null ||
                            transactionFeeCharged.ChargingAddress.ToBase58().IsNullOrEmpty())
                        {
                            address = transaction.From;
                        }
                        else
                        {
                            address = transactionFeeCharged.ChargingAddress.ToBase58();
                        }

                        await SetAddressSet(address, "", dailyData);
                        if (chainId == "AELF" && transactionFeeCharged.Symbol == "ELF")
                        {
                            await CalculateSupplyByAddress(address,
                                "", transactionFeeCharged.Amount, dailyData, transaction.TransactionId);
                        }

                        break;

                    case nameof(CrossChainReceived):
                        var crossChainReceived = new CrossChainReceived();
                        crossChainReceived.MergeFrom(logEvent);
                        await SetAddressSet(crossChainReceived.From.ToBase58(), crossChainReceived.To.ToBase58(),
                            dailyData);

                        if (crossChainReceived.Symbol == "ELF")
                        {
                            dailyData.DailyUnReceived -= crossChainReceived.Amount;

                            if (chainId == "AELF")
                            {
                                await CalculateSupplyByAddress(crossChainReceived.From.ToBase58(),
                                    crossChainReceived.To.ToBase58(),
                                    crossChainReceived.Amount, dailyData, transaction.TransactionId);
                            }
                        }

                        break;

                    case nameof(CrossChainTransferred):
                        var crossChainTransferred = new CrossChainTransferred();
                        crossChainTransferred.MergeFrom(logEvent);

                        if (crossChainTransferred.Symbol == "ELF")
                        {
                            dailyData.DailyBurnt -= crossChainTransferred.Amount;
                            dailyData.DailyUnReceived += crossChainTransferred.Amount;
                        }

                        break;
                    case nameof(Issued):
                        var issued = new Issued();
                        issued.MergeFrom(logEvent);
                        await SetAddressSet("", issued.To.ToBase58(), dailyData);
                        if (dailyData.ChainId == "AELF" && issued.Symbol == "ELF")
                        {
                            await CalculateSupplyByAddress("",
                                issued.To.ToBase58(),
                                issued.Amount, dailyData, transaction.TransactionId);
                        }

                        break;
                    case nameof(Voted):
                        if (_globalOptions.CurrentValue.ContractAddressElection != transaction.To)
                        {
                            continue;
                        }

                        var voted = new Voted();
                        voted.MergeFrom(logEvent);
                        var votedAmount = (double)voted.Amount / 1e8;
                        dailyData.TotalVotedStaked += votedAmount;
                        dailyData.DailyVotedIndexDic[voted.VoteId.ToString()] = new DailyVotedIndex()
                        {
                            ChainId = chainId,
                            Date = totalMilliseconds,
                            DateStr = date,
                            TransactionId = transaction.TransactionId,
                            VoteId = voted.VoteId.ToString(),
                            VoteAmount = votedAmount
                        };
                        break;
                    case nameof(Withdrawn):
                        var withdrawn = new Withdrawn();
                        withdrawn.MergeFrom(logEvent);
                        dailyData.WithDrawVotedIds.Add(withdrawn.VoteId.ToString());
                        dailyData.DailyWithDrawnList.Add(
                            new DailyWithDrawnIndex()
                            {
                                ChainId = chainId,
                                Date = totalMilliseconds,
                                DateStr = date,
                                TransactionId = transaction.TransactionId,
                                VoteId = withdrawn.VoteId.ToString(),
                            });
                        break;
                }
            }

            await SetAddressSet(transaction.From, transaction.To, dailyData);

            dailyData.DailyTransactionCountIndex.TransactionCount++;
            dailyData.DailyAvgTransactionFeeIndex.TransactionCount++;
            dailyData.DailyTotalContractCallIndex.CallCount++;

            if (dailyData.DailyContractCallIndexDic.TryGetValue(transaction.To, out var v))
            {
                v.CallCount++;
                v.CallerSet.Add(transaction.From);
                dailyData.CallersDic[transaction.To].Add(transaction.From);
            }
            else
            {
                dailyData.DailyContractCallIndexDic[transaction.To] = new DailyContractCallIndex()
                {
                    Date = totalMilliseconds,
                    DateStr = date,
                    ChainId = chainId,
                    CallCount = 1,
                    ContractAddress = transaction.To,
                    CallerSet = new HashSet<string>() { transaction.From }
                };
                dailyData.CallersDic[transaction.To] = new HashSet<string>() { transaction.From };
            }

            if (dailyData.StartBlockHeight == 0)
            {
                dailyData.StartBlockHeight = transaction.BlockHeight;
            }
            else if (dailyData.EndBlockHeight == 0)
            {
                dailyData.EndBlockHeight = transaction.BlockHeight;
            }
            else if (transaction.BlockHeight > dailyData.EndBlockHeight)
            {
                dailyData.EndBlockHeight = transaction.BlockHeight;
            }
        }

        if (dic.Count == 2)
        {
            var firstDate = _globalOptions.CurrentValue.OneBlockTime[chainId];
            var minDate = dic.Keys.Min();
            var needUpdateData = dic[minDate];
            double firstSupply = 0;


            await HandleSupplyChart(needUpdateData);


            var dailyElfPrice = await GetElfPrice(minDate);


            var totalBlockCount = (int)(needUpdateData.EndBlockHeight - needUpdateData.StartBlockHeight + 1);

            needUpdateData.DailyAvgTransactionFeeIndex.TotalFeeElf = needUpdateData.TotalFee.ToString("F6");

            needUpdateData.DailyAvgTransactionFeeIndex.AvgFeeElf =
                (needUpdateData.TotalFee / needUpdateData.DailyAvgTransactionFeeIndex.TransactionCount).ToString("F6");
            needUpdateData.DailyAvgTransactionFeeIndex.AvgFeeUsdt =
                ((needUpdateData.TotalFee / needUpdateData.DailyAvgTransactionFeeIndex.TransactionCount) *
                 dailyElfPrice).ToString();

            needUpdateData.DailyBlockRewardIndex.TotalBlockCount =
                needUpdateData.EndBlockHeight - needUpdateData.StartBlockHeight + 1;
            // needUpdateData.DailyBlockRewardIndex.BlockReward =
            //     ((double)needUpdateData.DailyConsensusBalance).ToString("F6");

            needUpdateData.DailyTotalBurntIndex.Burnt = needUpdateData.DailyBurnt.ToString("F6");

            needUpdateData.DailyTransactionCountIndex.BlockCount = totalBlockCount;

            needUpdateData.DailyActiveAddressCountIndex.AddressCount = needUpdateData.AddressSet.Count;
            needUpdateData.DailyActiveAddressCountIndex.SendAddressCount = needUpdateData.AddressFromSet.Count;
            needUpdateData.DailyActiveAddressCountIndex.ReceiveAddressCount = needUpdateData.AddressToSet.Count;

            needUpdateData.DailyTotalContractCallIndex.CallAddressCount = needUpdateData.AddressFromSet.Count;


            var dailyContractCallIndexList = needUpdateData.DailyContractCallIndexDic.Values.Select(c =>
            {
                c.CallAddressCount = needUpdateData.CallersDic[c.ContractAddress].Count;
                return c;
            }).ToList();


            var addressList = await GetAddressIndexList(needUpdateData.AddressSet.ToList(), chainId);
            var addressIndices = new List<AddressIndex>();

            var mergeAddressIndices = new List<MergeAddressIndex>();
            if (addressList.IsNullOrEmpty())
            {
                needUpdateData.DailyUniqueAddressCountIndex.AddressCount = needUpdateData.AddressSet.Count;
                foreach (var s in needUpdateData.AddressSet)
                {
                    addressIndices.Add(new AddressIndex()
                    {
                        Date = minDate,
                        Address = s,
                        ChainId = chainId
                    });
                }
            }
            else
            {
                needUpdateData.DailyUniqueAddressCountIndex.AddressCount =
                    needUpdateData.AddressSet.Count - addressList.Count;
                var findedAddressList = new List<string>();
                foreach (var addressIndex in addressList)
                {
                    needUpdateData.DailyUniqueAddressCountIndex.AddressCount += addressIndex.Date == minDate ? 1 : 0;
                    findedAddressList.Add(addressIndex.Address);
                }

                foreach (var s in needUpdateData.AddressSet)
                {
                    if (!findedAddressList.Contains(s))
                    {
                        addressIndices.Add(new AddressIndex()
                        {
                            Date = minDate,
                            Address = s,
                            ChainId = chainId
                        });
                    }
                }
            }


            needUpdateData.DailyStakedIndex.BpStaked = needUpdateData.TotalBpStaked.ToString("F4");
            var ids = new List<string>();
            foreach (var withDrawVotedId in needUpdateData.WithDrawVotedIds)
            {
                if (needUpdateData.DailyVotedIndexDic.TryGetValue(withDrawVotedId, out var v))
                {
                    needUpdateData.TotalVotedStaked -= v.VoteAmount;
                }
                else
                {
                    ids.Add(withDrawVotedId);
                }
            }

            var withDrawVotedAmount = await GetWithDrawVotedAmount(chainId, ids);
            needUpdateData.TotalVotedStaked -= withDrawVotedAmount;
            needUpdateData.DailyStakedIndex.VoteStaked = needUpdateData.TotalVotedStaked.ToString("F4");
            needUpdateData.DailyTVLIndex.DailyPrice = dailyElfPrice;


            needUpdateData.DailyTVLIndex.BPLockedAmount = needUpdateData.TotalBpStaked;
            needUpdateData.DailyTVLIndex.VoteLockedAmount = needUpdateData.TotalVotedStaked;
            if (chainId != "AELF")
            {
                var awakenTvl = await _awakenIndexerProvider.GetAwakenTvl(chainId, needUpdateData.DateTimeStamp);
                needUpdateData.DailyTVLIndex.AwakenLocked = awakenTvl == null ? 0 : awakenTvl.Value;
            }

            var dailyUniqueAddressCountIndices = _uniqueAddressRepository.GetQueryableAsync().Result
                .Where(c => c.ChainId == chainId)
                .Where(c => c.DateStr == DateTimeHelper.GetBeforeDayDate(needUpdateData.DateStr)).ToList();

            if (dailyUniqueAddressCountIndices.IsNullOrEmpty())
            {
                needUpdateData.DailyUniqueAddressCountIndex.TotalUniqueAddressees =
                    needUpdateData.DailyUniqueAddressCountIndex.AddressCount;
            }
            else
            {
                needUpdateData.DailyUniqueAddressCountIndex.TotalUniqueAddressees =
                    needUpdateData.DailyUniqueAddressCountIndex.AddressCount +
                    dailyUniqueAddressCountIndices.First().TotalUniqueAddressees;
            }

            var startNew = Stopwatch.StartNew();
            await _avgTransactionFeeRepository.AddOrUpdateAsync(needUpdateData.DailyAvgTransactionFeeIndex);
            await _blockRewardRepository.AddOrUpdateAsync(needUpdateData.DailyBlockRewardIndex);
            await _totalBurntRepository.AddOrUpdateAsync(needUpdateData.DailyTotalBurntIndex);
            await _deployContractRepository.AddAsync(needUpdateData.DailyDeployContractIndex);
            await _transactionCountRepository.AddOrUpdateAsync(needUpdateData.DailyTransactionCountIndex);
            await _uniqueAddressRepository.AddOrUpdateAsync(needUpdateData.DailyUniqueAddressCountIndex);
            await _activeAddressRepository.AddOrUpdateAsync(needUpdateData.DailyActiveAddressCountIndex);
            await _dailyTotalContractCallRepository.AddOrUpdateAsync(needUpdateData.DailyTotalContractCallIndex);
            await _dailySupplyGrowthIndexRepository.AddOrUpdateAsync(needUpdateData.DailySupplyGrowthIndex);
            await _dailyStakedIndexRepository.AddOrUpdateAsync(needUpdateData.DailyStakedIndex);
            await _dailyTVLIndexRepository.AddOrUpdateAsync(needUpdateData.DailyTVLIndex);
            if (!dailyContractCallIndexList.IsNullOrEmpty())
            {
                await _dailyContractCallRepository.AddOrUpdateManyAsync(dailyContractCallIndexList);
            }

            if (!addressIndices.IsNullOrEmpty())
            {
                await _addressRepository.AddOrUpdateManyAsync(addressIndices);
            }

            if (!needUpdateData.DailyVotedIndexDic.IsNullOrEmpty())
            {
                var dailyVotedIndices = needUpdateData.DailyVotedIndexDic.Values.ToList();
                await _dailyVotedIndexRepository.AddOrUpdateManyAsync(dailyVotedIndices);
            }


            if (!needUpdateData.DailyWithDrawnList.IsNullOrEmpty())
            {
                await _dailyWithDrawnIndexRepository.AddOrUpdateManyAsync(needUpdateData.DailyWithDrawnList);
            }

            await _dailySupplyChangeRepository.AddOrUpdateAsync(needUpdateData.DailySupplyChange);

            if (!needUpdateData.MonthlyAddressDic.IsNullOrEmpty())
            {
                var monthlyActiveAddressIndices = needUpdateData.MonthlyAddressDic.Values.ToList();
                await _monthlyActiveAddressInfoRepository.AddOrUpdateManyAsync(monthlyActiveAddressIndices);
            }

            await BatchUpdateMonthlyActiveAddressInfo(needUpdateData);

            startNew.Stop();
            needUpdateData.WirteFinishiTime = DateTime.UtcNow;
            needUpdateData.CostTime = startNew.Elapsed.TotalSeconds;
            dic.Remove(minDate);

            _logger.LogInformation("Update daily transaction data,chainId:{chainId} date:{minDate},", chainId, minDate);
            return needUpdateData;
        }


        return null;
    }


    public async Task BatchUpdateMonthlyActiveAddressInfo(DailyTransactionsChartSet dailyData)
    {
        if (dailyData.MonthlyAddressDic.IsNullOrEmpty())
        {
            return;
        }

        var list = dailyData.MonthlyAddressDic.Values.ToList();
        while (list.Count() > BatchUpdateMaxSize)
        {
            await _monthlyActiveAddressInfoRepository.AddOrUpdateManyAsync(list.Take(BatchUpdateMaxSize).ToList());
            list = list.Skip(BatchUpdateMaxSize).ToList();
        }

        if (!list.IsNullOrEmpty())
        {
            await _monthlyActiveAddressInfoRepository.AddOrUpdateManyAsync(list);
        }
    }

    public async Task HandleSupplyChart(DailyTransactionsChartSet dailyData)
    {
        dailyData.DailyConsensusBalance /= 1e8;
        dailyData.DailyBurnt /= 1e8;
        dailyData.DailyOrganizationBalance /= 1e8;
        dailyData.DailyUnReceived /= 1e8;

        dailyData.DailySupplyGrowthIndex.DailyBurnt = dailyData.DailyBurnt;
        dailyData.DailySupplyGrowthIndex.DailyConsensusBalance = dailyData.DailyConsensusBalance;
        dailyData.DailySupplyGrowthIndex.DailyOrganizationBalance = dailyData.DailyOrganizationBalance;
        dailyData.DailySupplyGrowthIndex.DailyUnReceived = dailyData.DailyUnReceived;

        var beforeDaySupply = _dailySupplyGrowthIndexRepository.GetQueryableAsync().Result
            .Where(c => c.ChainId == dailyData.ChainId)
            .Where(c => c.DateStr == DateTimeHelper.GetBeforeDayDate(dailyData.DateStr)).Take(1).ToList();

        if (!beforeDaySupply.IsNullOrEmpty())
        {
            dailyData.DailySupplyGrowthIndex.TotalBurnt = beforeDaySupply.First().TotalBurnt + dailyData.DailyBurnt;

            dailyData.DailySupplyGrowthIndex.TotalConsensusBalance =
                beforeDaySupply.First().TotalConsensusBalance + dailyData.DailyConsensusBalance;

            dailyData.DailySupplyGrowthIndex.TotalOrganizationBalance =
                beforeDaySupply.First().TotalOrganizationBalance + dailyData.DailyOrganizationBalance;

            dailyData.DailySupplyGrowthIndex.TotalUnReceived =
                beforeDaySupply.First().TotalUnReceived + dailyData.DailyUnReceived;
        }
        else
        {
            dailyData.DailySupplyGrowthIndex.TotalBurnt = dailyData.DailyBurnt;
            dailyData.DailySupplyGrowthIndex.TotalConsensusBalance = dailyData.DailyConsensusBalance;
            dailyData.DailySupplyGrowthIndex.TotalOrganizationBalance = dailyData.DailyOrganizationBalance;
            dailyData.DailySupplyGrowthIndex.TotalUnReceived = dailyData.DailyUnReceived;
        }

        await _dailySupplyGrowthIndexRepository.AddOrUpdateAsync(dailyData.DailySupplyGrowthIndex);
    }

    public async Task SetAddressSet(string from, string to, DailyTransactionsChartSet dailyData)
    {
        if (!from.IsNullOrEmpty())
        {
            dailyData.AddressFromSet.Add(from);
            dailyData.AddressSet.Add(from);

            var key = "from" + from;
            if (!dailyData.MonthlyAddressDic.ContainsKey(key))
            {
                dailyData.MonthlyAddressDic[key] = new MonthlyActiveAddressInfoIndex()
                {
                    ChainId = dailyData.ChainId,
                    Address = from,
                    Type = "from",
                    DateMonth = DateTimeHelper.ConvertToYYYYMM(dailyData.DateTimeStamp),
                    Date = dailyData.DateTimeStamp
                };
            }
        }

        if (!to.IsNullOrEmpty())
        {
            dailyData.AddressToSet.Add(to);
            dailyData.AddressSet.Add(to);

            var key = "to" + to;
            if (!dailyData.MonthlyAddressDic.ContainsKey(key))
            {
                dailyData.MonthlyAddressDic[key] = new MonthlyActiveAddressInfoIndex()
                {
                    ChainId = dailyData.ChainId,
                    Address = to,
                    Type = "to",
                    DateMonth = DateTimeHelper.ConvertToYYYYMM(dailyData.DateTimeStamp),
                    Date = dailyData.DateTimeStamp
                };
            }
        }
    }


    public async Task CalculateSupplyByAddress(string send, string receive, long amount,
        DailyTransactionsChartSet dailyData, string transactionId)
    {
        if (!send.IsNullOrEmpty())
        {
            if (_globalOptions.CurrentValue.OrganizationAddress == send)
            {
                dailyData.DailyOrganizationBalance -= amount;
                var record = transactionId + "_" + "transferredFrom" + "_" +
                             send +
                             "_" + amount / 1e8;
                dailyData.DailySupplyChange.SupplyChange.Add(record);
            }

            if (_globalOptions.CurrentValue.ContractAddressConsensus[dailyData.ChainId] ==
                send)
            {
                dailyData.DailyConsensusBalance -= amount;
                var record = transactionId + "_" + "transferredFrom" + "_" +
                             send +
                             "_" + amount / 1e8;
                dailyData.DailySupplyChange.SupplyChange.Add(record);
            }
        }

        if (!receive.IsNullOrEmpty())
        {
            if (_globalOptions.CurrentValue.OrganizationAddress == receive)
            {
                dailyData.DailyOrganizationBalance += amount;
                var record = transactionId + "_" + "transferredTo" + "_" +
                             receive +
                             "_" + amount / 1e8;
                dailyData.DailySupplyChange.SupplyChange.Add(record);
            }


            if (_globalOptions.CurrentValue.ContractAddressConsensus[dailyData.ChainId] ==
                receive)
            {
                dailyData.DailyConsensusBalance += amount;
                var record = amount + "_" + "transferredTo" + "_" +
                             receive +
                             "_" + amount / 1e8;
                dailyData.DailySupplyChange.SupplyChange.Add(record);
            }
        }
    }


    public async Task<List<AddressIndex>> GetAddressIndexList(List<string> list, string chainId)
    {
        var query = await _addressRepository.GetQueryableAsync();
        query = query.Where(c => c.ChainId == chainId);

        var addressIndices = new List<AddressIndex>();

        var chunkSize = 100;
        var chunks = list.Select((value, index) => new { Index = index, Value = value })
            .GroupBy(x => x.Index / chunkSize)
            .Select(grp => grp.Select(x => x.Value).ToList())
            .ToList();

        foreach (var chunk in chunks)
        {
            var predicates = chunk.Select(s => (Expression<Func<AddressIndex, bool>>)(o => o.Address == s));
            var combinedPredicate = predicates.Aggregate((prev, next) => prev.Or(next));

            var chunkQuery = query.Where(combinedPredicate).Take(1000).ToList();

            if (chunkQuery.Any())
            {
                addressIndices.AddRange(chunkQuery);
            }
        }

        return addressIndices;
    }


    public async Task<List<MergeAddressIndex>> GetMergeAddressIndexList(List<string> list, long date)
    {
        var query = await _mergeAddressRepository.GetQueryableAsync();

        var addressIndices = new List<MergeAddressIndex>();

        var chunkSize = 100;
        var chunks = list.Select((value, index) => new { Index = index, Value = value })
            .GroupBy(x => x.Index / chunkSize)
            .Select(grp => grp.Select(x => x.Value).ToList())
            .ToList();
        query = query.Where(c => c.Date < date);
        foreach (var chunk in chunks)
        {
            var predicates = chunk.Select(s => (Expression<Func<MergeAddressIndex, bool>>)(o => o.Address == s));
            var combinedPredicate = predicates.Aggregate((prev, next) => prev.Or(next));

            var chunkQuery = query.Where(combinedPredicate).Take(1000).ToList();

            if (chunkQuery.Any())
            {
                addressIndices.AddRange(chunkQuery);
            }
        }


        return addressIndices;
    }

    public async Task<double> GetWithDrawVotedAmount(string chainId, List<string> voteIds)
    {
        try
        {
            if (voteIds.IsNullOrEmpty())
            {
                return 0;
            }

            var queryable = await _dailyVotedIndexRepository.GetQueryableAsync();


            var predicates = voteIds.Select(s =>
                (Expression<Func<DailyVotedIndex, bool>>)(o => o.VoteId == s));
            var predicate = predicates.Aggregate((prev, next) => prev.Or(next));
            queryable = queryable.Where(predicate);

            var list = queryable.Where(c => c.ChainId == chainId).Take(1000).ToList();


            if (list.IsNullOrEmpty())
            {
                return 0;
            }


            var sum = list.Sum(c => c.VoteAmount);

            return sum;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetWithDrawVotedAmount {chainId},{list}", chainId, voteIds);
            return 0;
        }
    }


    public async Task UpdateDailyNetwork()
    {
        foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
        {
            try
            {
                var queryable = await _roundIndexRepository.GetQueryableAsync();


                var roundIndices = queryable.Where(c => c.ChainId == chainId).OrderByDescending(c => c.StartTime)
                    .Take(1);

                if (roundIndices.IsNullOrEmpty())
                {
                    return;
                }

                var startTime = roundIndices.First().StartTime;

                var endTime = DateTimeHelper.GetDateTimeLong(startTime);
                startTime = DateTimeHelper.GetBeforeDayMilliSeconds(endTime);

                var list = queryable.Where(c => c.ChainId == chainId).Where(c => c.StartTime >= startTime)
                    .Where(c => c.StartTime < endTime).Take(10000).ToList();

                if (list.IsNullOrEmpty())
                {
                    return;
                }

                var blockProduceIndex = new DailyBlockProduceCountIndex()
                {
                    Date = startTime,
                    ChainId = chainId
                };

                var dailyCycleCountIndex = new DailyCycleCountIndex()
                {
                    Date = startTime,
                    ChainId = chainId
                };

                var dailyBlockProduceDurationIndex = new DailyBlockProduceDurationIndex()
                {
                    Date = startTime,
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
                        _logger.LogWarning(
                            "Round duration or blocks is zero,chainId:{chainId},round number:{roundNumber}", chainId,
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

                await _blockProduceRepository.AddOrUpdateAsync(blockProduceIndex);
                await _blockProduceDurationRepository.AddOrUpdateAsync(dailyBlockProduceDurationIndex);
                await _cycleCountRepository.AddOrUpdateAsync(dailyCycleCountIndex);
                _logger.LogInformation("Insert daily network statistic count index chainId:{chainId},date:{dateStr}",
                    chainId,
                    DateTimeHelper.GetDateTimeString(startTime));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UpdateDailyNetwork err，UpdateDailyNetwork {chainId}", chainId);
            }
        }
    }

    public async Task BatchUpdateNodeNetworkTask()
    {
        if (_globalOptions.CurrentValue.InitRound)
        {
            await ConnectAsync();
            foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
            {
                var currentRound = await GetCurrentRound(chainId);
                var initStartRound = currentRound.RoundNumber - 40900;

                RedisDatabase.StringSet(RedisKeyHelper.LatestRound(chainId), initStartRound);
                _logger.LogInformation("Init round:{chainId},round:{initStartRound}", chainId, initStartRound);
            }
        }

        var tasks = new List<Task>();
        foreach (var chainId in _globalOptions.CurrentValue.ChainIds)
        {
            tasks.Add(UpdateRound(chainId));
        }

        await tasks.WhenAll();
    }


    public async Task UpdateRound(string chainId)
    {
        while (true)
        {
            try
            {
                var currentRound = await GetCurrentRound(chainId);
                Stopwatch stopwatch = Stopwatch.StartNew();
                stopwatch.Start();
                var tasks = new List<Task>();

                await ConnectAsync();
                var redisValue = RedisDatabase.StringGet(RedisKeyHelper.LatestRound(chainId));
                if (redisValue.IsNullOrEmpty)
                {
                    _logger.LogError("BatchUpdateNetwork redisValue is null chainId:{c}", chainId);
                    return;
                }

                var startRoundNumber = (long)redisValue;


                if (startRoundNumber >= currentRound.RoundNumber ||
                    startRoundNumber + BatchPullRoundCount - 1 >= currentRound.RoundNumber)
                {
                    BatchPullRoundCount = 1;
                    _logger.LogInformation("BatchUpdateNetwork Stop update round:{chainId},{startRoundNumber}", chainId,
                        startRoundNumber);
                    await Task.Delay(1000 * 60 * 5);
                    continue;
                }

                var rounds = new List<Round>();

                var _lock = new object();

                for (long i = startRoundNumber; i < startRoundNumber + BatchPullRoundCount; i++)
                {
                    tasks.Add(GetRound(i, chainId).ContinueWith(task =>
                    {
                        lock (_lock)
                        {
                            rounds.Add(task.Result);
                        }
                    }));
                }

                await Task.WhenAll(tasks);


                stopwatch.Stop();
                var findCost = stopwatch.Elapsed.TotalSeconds;
                var roundIndices = new List<RoundIndex>();
                var nodeBlockProduceIndices = new List<NodeBlockProduceIndex>();


                foreach (var round in rounds)
                {
                    var statisticRound = await StatisticRound(round, chainId);
                    roundIndices.Add(statisticRound.r);
                    nodeBlockProduceIndices.AddRange(statisticRound.n);
                }

                stopwatch = Stopwatch.StartNew();


                await _roundIndexRepository.AddOrUpdateManyAsync(roundIndices);
                await _nodeBlockProduceRepository.AddOrUpdateManyAsync(nodeBlockProduceIndices);
                _logger.LogInformation(
                    "Insert batch round index chainId:{chainId},round number:{startRoundNumber},date:{dateStr}",
                    chainId,
                    startRoundNumber, DateTimeHelper.GetDateTimeString(roundIndices.First().StartTime));
                stopwatch.Stop();
                var insertCost = stopwatch.Elapsed.TotalSeconds;
                _logger.LogInformation(
                    "BatchUpdateNetwork cost time,round index find cost time:{findCost},insert cost time:{insertCost},start:{startRoundNumber},end:{endRoundNumber},chainId:{chainId},,round count:{count},node produce count:{nodeProduceCount}",
                    findCost, insertCost, startRoundNumber, startRoundNumber + BatchPullRoundCount - 1,
                    chainId, roundIndices.Count, nodeBlockProduceIndices.Count);

                RedisDatabase.StringSet(RedisKeyHelper.LatestRound(chainId), startRoundNumber + BatchPullRoundCount);
                await Task.Delay(1000 * 10);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "BatchUpdateNetwork err:{chainId}", chainId);
                await Task.Delay(1000 * 10);
            }
        }
    }

    internal async Task<Round> GetRound(long roundNumber, string chainId)
    {
        var client = new AElfClient(_globalOptions.CurrentValue.ChainNodeHosts[chainId]);

        var param = new Int64Value()
        {
            Value = roundNumber
        };


        var transaction = await client.GenerateTransactionAsync(
            client.GetAddressFromPrivateKey(GlobalOptions.PrivateKey),
            _globalOptions.CurrentValue.ContractAddressConsensus[chainId],
            "GetRoundInformation", param);


        var signTransaction = client.SignTransaction(GlobalOptions.PrivateKey, transaction);

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto()
        {
            RawTransaction = HexByteConvertorExtensions.ToHex(signTransaction.ToByteArray())
        });

        var round = Round.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
        return round;
    }

    internal async Task<Round> GetCurrentRound(string chainId)
    {
        var client = new AElfClient(_globalOptions.CurrentValue.ChainNodeHosts[chainId]);

        var param = new Empty()
        {
        };


        var transaction = await client.GenerateTransactionAsync(
            client.GetAddressFromPrivateKey(GlobalOptions.PrivateKey),
            _globalOptions.CurrentValue.ContractAddressConsensus[chainId],
            "GetCurrentRoundInformation", param);


        var signTransaction = client.SignTransaction(GlobalOptions.PrivateKey, transaction);

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto()
        {
            RawTransaction = HexByteConvertorExtensions.ToHex(signTransaction.ToByteArray())
        });

        var round = Round.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
        return round;
    }


    internal async Task<(RoundIndex r, List<NodeBlockProduceIndex> n)> StatisticRound(Round round, string chainId)
    {
        var roundIndex = new RoundIndex()
        {
            ChainId = chainId,
            RoundNumber = round.RoundNumber,
            ProduceBlockBpAddresses = new List<string>(),
            NotProduceBlockBpAddresses = new List<string>()
        };

        var batch = new List<NodeBlockProduceIndex>();
        var client = new AElfClient(_globalOptions.CurrentValue.ChainNodeHosts[chainId]);
        foreach (var minerInRound in round.RealTimeMinersInformation)
        {
            var bpAddress = client.GetAddressFromPubKey(minerInRound.Key);
            var nodeInfo = new NodeBlockProduceIndex()
            {
                ChainId = chainId,
                RoundNumber = round.RoundNumber,
                NodeAddress = bpAddress
            };

            roundIndex.Blcoks += minerInRound.Value.ActualMiningTimes.Count;
            nodeInfo.Blcoks = minerInRound.Value.ActualMiningTimes.Count;

            var expectBlocks = minerInRound.Value.ActualMiningTimes.Count > 8 ? 15 : 8;

            roundIndex.MissedBlocks += expectBlocks - minerInRound.Value.ActualMiningTimes.Count;
            nodeInfo.MissedBlocks = expectBlocks - minerInRound.Value.ActualMiningTimes.Count;
            nodeInfo.IsExtraBlockProducer = minerInRound.Value.IsExtraBlockProducer;


            if (minerInRound.Value.ActualMiningTimes.IsNullOrEmpty())
            {
                roundIndex.NotProduceBlockBpCount++;
                roundIndex.NotProduceBlockBpAddresses.Add(bpAddress);
            }
            else
            {
                roundIndex.ProduceBlockBpCount++;
                roundIndex.ProduceBlockBpAddresses.Add(bpAddress);

                var min = minerInRound.Value.ActualMiningTimes.Select(c => c.Seconds).Min() * 1000;
                var max = minerInRound.Value.ActualMiningTimes.Select(c => c.Seconds).Max() * 1000;

                nodeInfo.StartTime = min;
                nodeInfo.EndTime = max;
                if (roundIndex.StartTime == 0)
                {
                    roundIndex.StartTime = min;
                }
                else
                {
                    roundIndex.StartTime = Math.Min(roundIndex.StartTime, min);
                }

                roundIndex.EndTime = Math.Max(roundIndex.EndTime, max);
            }

            nodeInfo.DurationSeconds = nodeInfo.EndTime - nodeInfo.StartTime;


            batch.Add(nodeInfo);
        }

        roundIndex.DurationSeconds = roundIndex.EndTime - roundIndex.StartTime;


        return (roundIndex, batch);
    }


    public async Task<List<TransactionData>> GetBatchTransactionList(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        object _lock = new object();
        var batchList = new List<TransactionData>();

        Stopwatch stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (long i = startBlockHeight; i <= endBlockHeight; i += 100)
        {
            var start = i;
            var end = start + 99 > endBlockHeight ? endBlockHeight : start + 99;
            var findTsk = _aelfIndexerProvider.GetTransactionsDataAsync(chainId, start, end, "")
                .ContinueWith(task =>
                {
                    lock (_lock)
                    {
                        if (task.Result.IsNullOrEmpty())
                        {
                            _logger.LogError(
                                "Get batch transaction list is null,chainId:{chainId},start:{startBlockHeight},end:{endBlockHeight}",
                                chainId, start, end);
                            return;
                        }

                        batchList.AddRange(task.Result);
                    }
                });
            tasks.Add(findTsk);
        }

        await tasks.WhenAll();


        return batchList;
    }


    public async Task<List<TransactionData>> GetBatchLogEventList(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        object _lock = new object();
        var batchList = new List<TransactionData>();

        Stopwatch stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (long i = startBlockHeight; i <= endBlockHeight; i += 100)
        {
            var start = i;
            var end = start + 99 > endBlockHeight ? endBlockHeight : start + 99;
            var findTsk = _aelfIndexerProvider.GetTransactionsDataAsync(chainId, start, end, "")
                .ContinueWith(task =>
                {
                    lock (_lock)
                    {
                        if (task.Result.IsNullOrEmpty())
                        {
                            _logger.LogError(
                                "Get batch transaction list is null,chainId:{chainId},start:{startBlockHeight},end:{endBlockHeight}",
                                chainId, start, end);
                            return;
                        }

                        batchList.AddRange(task.Result);
                    }
                });
            tasks.Add(findTsk);
        }

        await tasks.WhenAll();


        return batchList;
    }


    public async Task UpdateTransactionRatePerMinuteTaskAsync()
    {
        await ConnectAsync();

        var chainIds = new List<string>();
        var mergeList = new List<List<TransactionCountPerMinuteDto>>();
        try
        {
            chainIds = _workerOptions.CurrentValue.ChainIds;


            foreach (var chainId in chainIds)
            {
                var chartDataKey = RedisKeyHelper.TransactionChartData(chainId);

                var nowMilliSeconds = DateTimeHelper.GetNowMilliSeconds();
                var beforeHoursMilliSeconds = DateTimeHelper.GetBeforeHoursMilliSeconds(3);


                var input = new TransactionsRequestDto()
                {
                    ChainId = chainId,
                    SkipCount = 0,
                    MaxResultCount = 1000,
                    StartTime = beforeHoursMilliSeconds,
                    EndTime = nowMilliSeconds
                };
                input.SetDefaultSort();
                var transactionsAsync =
                    await _blockChainIndexerProvider.GetTransactionsAsync(input);
                if (transactionsAsync == null || transactionsAsync.Items.Count <= 0)
                {
                    input.StartTime = 0;
                    input.EndTime = 0;
                    transactionsAsync =
                        await _blockChainIndexerProvider.GetTransactionsAsync(input);
                }

                if (transactionsAsync == null)
                {
                    _logger.LogError("Not query transaction list from blockchain app plugin,chainId:{chainId}",
                        chainId);
                    continue;
                }

                if (transactionsAsync.Items.IsNullOrEmpty())
                {
                    _logger.LogWarning("transaction is null,chainId:{chainId}", chainId);
                    continue;
                }


                var transactionChartData =
                    await ParseToTransactionChartDataAsync(chartDataKey, transactionsAsync.Items);


                if (transactionChartData.IsNullOrEmpty())
                {
                    _logger.LogInformation("merge transaction data is null:{chainId}", chainId);
                    continue;
                }

                _logger.LogInformation("transaction chart data:{chainId},count:{count}", chainId,
                    transactionChartData.Count);

                if (transactionChartData.Count > 180)
                {
                    transactionChartData = transactionChartData.Skip(transactionChartData.Count - 180).ToList();
                }

                _logger.LogInformation("sub transaction chart data:{chainId},count:{count}", chainId,
                    transactionChartData.Count);

                mergeList.Add(transactionChartData);
                var serializeObject = JsonConvert.SerializeObject(transactionChartData);


                await RedisDatabase.StringSetAsync(chartDataKey, serializeObject);
                _logger.LogInformation("Set transaction count per minute to cache success!!,redis key:{chartDataKey}",
                    chartDataKey);
            }


            var merge = mergeList.SelectMany(c => c).GroupBy(c => c.Start).Select(c =>
                new TransactionCountPerMinuteDto()
                {
                    Start = c.Key,
                    End = c.Key + 60000,
                    Count = c.Sum(d => d.Count)
                }).ToList();

            if (merge.Count > 180)
            {
                merge = merge.Skip(merge.Count - 180).ToList();
            }

            _logger.LogInformation("merge count {count}", merge.Count);

            var mergeSerializeObject = JsonConvert.SerializeObject(merge);
            var mergeKey = RedisKeyHelper.TransactionChartData("merge");
            await RedisDatabase.StringSetAsync(RedisKeyHelper.TransactionChartData("merge"), mergeSerializeObject);

            _logger.LogInformation("Set transaction count per minute to cache success!!,redis key:{mergeKey}",
                mergeKey);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Update transaction count per minute error");
        }
    }


    public async Task<List<TransactionCountPerMinuteDto>> ParseToTransactionChartDataAsync(
        string key, List<IndexerTransactionInfoDto> list)
    {
        var dictionary = new Dictionary<long, TransactionCountPerMinuteDto>();
        foreach (var indexerTransactionDto in list)
        {
            var t = indexerTransactionDto.Metadata.Block.BlockTime;
            var timestamp =
                DateTimeHelper.GetTotalMilliseconds(new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, 0));


            if (dictionary.ContainsKey(timestamp))
            {
                dictionary[timestamp].Count++;
            }
            else
            {
                dictionary.Add(timestamp,
                    new TransactionCountPerMinuteDto() { Start = timestamp, End = timestamp + 60000, Count = 1 });
            }
        }

        var newList = dictionary.Values.OrderBy(c => c.Start).ToList();
        try
        {
            await ConnectAsync();
            var redisValue = RedisDatabase.StringGet(key);
            if (redisValue.IsNullOrEmpty)
            {
                return newList;
            }

            var oldList = JsonConvert.DeserializeObject<List<TransactionCountPerMinuteDto>>(redisValue);

            var last = oldList.Last();
            var subOldList = oldList.GetRange(0, oldList.Count - 1);

            var subNewList = newList.Where(c => c.Start >= last.Start).ToList();

            if (!subNewList.IsNullOrEmpty() && subNewList.Count > 0)
            {
                subOldList.AddRange(subNewList);
            }

            if (subOldList.Count == 0)
            {
                subOldList = newList;
            }

            _logger.LogInformation(
                "Merge transaction per minute data chainId:{chainId},oldList:{oldList},newList:{newList}", key,
                oldList.Count, newList.Count);

            return subOldList;
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Parse key:{key} data to transaction err", key);
        }

        return newList;
    }

    public async Task<double> GetElfPrice(string date)
    {
        try
        {
            var queryableAsync = await _priceRepository.GetQueryableAsync();
            var elfPriceIndices = queryableAsync.Where(c => c.DateStr == date).ToList();

            if (!elfPriceIndices.IsNullOrEmpty())
            {
                var dailyElfPrice = double.Parse(elfPriceIndices[0].Close);
                return dailyElfPrice;
            }


            var res = await _priceServerProvider.GetDailyPriceAsync(new GetDailyPriceRequestDto()
            {
                TokenPair = "elf-usdt",
                TimeStamp = date.Replace("-", "")
            });

            var s = ((double)res.Data.Price / 1e8).ToString();
            _priceRepository.AddOrUpdateAsync(new ElfPriceIndex()
            {
                DateStr = date,
                Close = s
            });

            _logger.LogInformation("GetElfPrice date:{dateStr},price{price}", date, s);
            return (double)res.Data.Price / 1e8;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetElfPrice err,date:{dateStr}", date.Replace("-", ""));
            return 0;
        }
    }
}