using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElfScanServer;
using Moq;
using Xunit;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Dtos.ChartData;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

public class ChartDataServiceTests : AElfScanServerApplicationTestBase
{
    private readonly IChartDataService _chartDataService;
    private const string SideChainId = "tDVW";
    private readonly IEntityMappingRepository<DailyMergeUniqueAddressCountIndex, string> _uniqueMergeAddressRepository;
    private readonly IEntityMappingRepository<DailyActiveAddressCountIndex, string> _activeAddressRepository;
    private readonly IEntityMappingRepository<MonthlyActiveAddressIndex, string> _monthlyActiveAddressIndexRepository;
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
    private readonly IEntityMappingRepository<DailyTransactionCountIndex, string> _transactionCountRepository;
    private readonly IEntityMappingRepository<DailyUniqueAddressCountIndex, string> _uniqueAddressRepository;
    private readonly IEntityMappingRepository<DailyContractCallIndex, string> _dailyContractCallRepository;
    private readonly IEntityMappingRepository<DailyTotalContractCallIndex, string> _dailyTotalContractCallRepository;
    private readonly IEntityMappingRepository<DailySupplyGrowthIndex, string> _dailySupplyGrowthIndexRepository;
    private readonly IEntityMappingRepository<DailyStakedIndex, string> _dailyStakedIndexRepository;
    private readonly IEntityMappingRepository<DailyTVLIndex, string> _dailyTVLRepository;


    public ChartDataServiceTests(ITestOutputHelper output) : base(output)
    {
        _chartDataService = GetRequiredService<IChartDataService>();
        _transactionCountRepository =
            GetRequiredService<IEntityMappingRepository<DailyTransactionCountIndex, string>>();
        _roundIndexRepository = GetRequiredService<IEntityMappingRepository<RoundIndex, string>>();
        _nodeBlockProduceIndex = GetRequiredService<IEntityMappingRepository<NodeBlockProduceIndex, string>>();
        _blockProduceIndexRepository =
            GetRequiredService<IEntityMappingRepository<DailyBlockProduceCountIndex, string>>();
        _blockProduceDurationRepository =
            GetRequiredService<IEntityMappingRepository<DailyBlockProduceDurationIndex, string>>();
        _hourNodeBlockProduceRepository =
            GetRequiredService<IEntityMappingRepository<HourNodeBlockProduceIndex, string>>();
        _cycleCountRepository = GetRequiredService<IEntityMappingRepository<DailyCycleCountIndex, string>>();
        _avgTransactionFeeRepository =
            GetRequiredService<IEntityMappingRepository<DailyAvgTransactionFeeIndex, string>>();
        _elfPriceRepository = GetRequiredService<IEntityMappingRepository<ElfPriceIndex, string>>();
        _blockRewardRepository = GetRequiredService<IEntityMappingRepository<DailyBlockRewardIndex, string>>();
        _totalBurntRepository = GetRequiredService<IEntityMappingRepository<DailyTotalBurntIndex, string>>();
        _deployContractRepository = GetRequiredService<IEntityMappingRepository<DailyDeployContractIndex, string>>();
        _blockSizeRepository = GetRequiredService<IEntityMappingRepository<DailyAvgBlockSizeIndex, string>>();
        _transactionsRepository = GetRequiredService<IEntityMappingRepository<TransactionIndex, string>>();
        _uniqueAddressRepository = GetRequiredService<IEntityMappingRepository<DailyUniqueAddressCountIndex, string>>();
        _uniqueMergeAddressRepository =
            GetRequiredService<IEntityMappingRepository<DailyMergeUniqueAddressCountIndex, string>>();
        _activeAddressRepository = GetRequiredService<IEntityMappingRepository<DailyActiveAddressCountIndex, string>>();
        _dailyContractCallRepository = GetRequiredService<IEntityMappingRepository<DailyContractCallIndex, string>>();
        _dailyTotalContractCallRepository =
            GetRequiredService<IEntityMappingRepository<DailyTotalContractCallIndex, string>>();
        _dailySupplyGrowthIndexRepository =
            GetRequiredService<IEntityMappingRepository<DailySupplyGrowthIndex, string>>();
        _dailyStakedIndexRepository = GetRequiredService<IEntityMappingRepository<DailyStakedIndex, string>>();
        _dailyTVLRepository = GetRequiredService<IEntityMappingRepository<DailyTVLIndex, string>>();
        _monthlyActiveAddressIndexRepository =
            GetRequiredService<IEntityMappingRepository<MonthlyActiveAddressIndex, string>>();
    }

    [Fact]
    //pass
    public async Task GetDailyTransactionCountAsync_ShouldProcessDataCorrectly()
    {
        var mockDatas = new List<DailyTransactionCountIndex>()
        {
            new() { Date = 20230101, DateStr = "2023-01-01", ChainId = "AELF", TransactionCount = 100 },
            new() { Date = 20230102, DateStr = "2023-01-02", ChainId = "AELF", TransactionCount = 200 },
            new() { Date = 20230101, DateStr = "2023-01-01", ChainId = SideChainId, TransactionCount = 50 },
            new() { Date = 20230102, DateStr = "2023-01-02", ChainId = SideChainId, TransactionCount = 80 }
        };

        await _transactionCountRepository.AddOrUpdateManyAsync(mockDatas);

        var result = await _chartDataService.GetDailyTransactionCountAsync(new ChartDataRequest());


        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
        Assert.Equal(280, result.HighestTransactionCount.MergeTransactionCount);
        Assert.Equal(150, result.LowesTransactionCount.MergeTransactionCount);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    //pass
    public async Task GetUniqueAddressCountAsync_ShouldProcessDataCorrectly()
    {
        var mockDatas = new List<DailyMergeUniqueAddressCountIndex>()
        {
            new()
            {
                Date = 20230101, DateStr = "2023-01-01", ChainId = "AELF", AddressCount = 100,
                TotalUniqueAddressees = 120
            },
            new()
            {
                Date = 20230102, DateStr = "2023-01-02", ChainId = "AELF", AddressCount = 150,
                TotalUniqueAddressees = 180
            },
            new()
            {
                Date = 20230101, DateStr = "2023-01-01", ChainId = SideChainId, AddressCount = 50,
                TotalUniqueAddressees = 60
            },
            new()
            {
                Date = 20230102, DateStr = "2023-01-02", ChainId = SideChainId, AddressCount = 75,
                TotalUniqueAddressees = 90
            }
        };

        await _uniqueMergeAddressRepository.AddOrUpdateManyAsync(mockDatas);

        var result = await _chartDataService.GetUniqueAddressCountAsync(new ChartDataRequest());

        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);
        Assert.Equal(225, result.HighestIncrease.MergeAddressCount);
        Assert.Equal(150, result.LowestIncrease.MergeAddressCount);
        Assert.Equal(2, result.Total);
    }

    [Fact] 
    //pass
    public async Task GetActiveAddressCountAsync_ShouldProcessDataCorrectly()
    {
        var mockDatas = new List<DailyActiveAddressCountIndex>()
        {
            new()
            {
                Date = 20230101, DateStr = "2023-01-01", ChainId = "AELF", AddressCount = 100, ReceiveAddressCount = 60,
                SendAddressCount = 40
            },
            new()
            {
                Date = 20230102, DateStr = "2023-01-02", ChainId = "AELF", AddressCount = 150, ReceiveAddressCount = 90,
                SendAddressCount = 60
            },
            new()
            {
                Date = 20230101, DateStr = "2023-01-01", ChainId = SideChainId, AddressCount = 50,
                ReceiveAddressCount = 30, SendAddressCount = 20
            },
            new()
            {
                Date = 20230102, DateStr = "2023-01-02", ChainId = SideChainId, AddressCount = 75,
                ReceiveAddressCount = 45, SendAddressCount = 30
            }
        };

        await _activeAddressRepository.AddOrUpdateManyAsync(mockDatas);

        var result = await _chartDataService.GetActiveAddressCountAsync(new ChartDataRequest());

        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);
        Assert.Equal(225, result.HighestActiveCount.MergeAddressCount);
        Assert.Equal(150, result.LowestActiveCount.MergeAddressCount);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    //pass
    public async Task GetMonthlyActiveAddressCountAsync_ShouldProcessDataCorrectly()
    {
        var mockDatas = new List<MonthlyActiveAddressIndex>()
        {
            new()
            {
                DateMonth = 202301, ChainId = "AELF", AddressCount = 1000, ReceiveAddressCount = 600,
                SendAddressCount = 400
            },
            new()
            {
                DateMonth = 202302, ChainId = "AELF", AddressCount = 1200, ReceiveAddressCount = 720,
                SendAddressCount = 480
            },
            new()
            {
                DateMonth = 202301, ChainId = SideChainId, AddressCount = 500, ReceiveAddressCount = 300,
                SendAddressCount = 200
            },
            new()
            {
                DateMonth = 202302, ChainId = SideChainId, AddressCount = 600, ReceiveAddressCount = 360,
                SendAddressCount = 240
            }
        };

        await _monthlyActiveAddressIndexRepository.AddOrUpdateManyAsync(mockDatas);

        var result = await _chartDataService.GetMonthlyActiveAddressCountAsync(new ChartDataRequest());

        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);
        Assert.Equal(1800, result.HighestActiveCount.MergeAddressCount);
        Assert.Equal(1500, result.LowestActiveCount.MergeAddressCount);
        Assert.Equal(2, result.Total);
    }


    [Fact]
    //pass
    public async Task GetBlockProduceRateAsync_ShouldProcessDataCorrectly()
    {
        var mockDatas = new List<DailyBlockProduceCountIndex>()
        {
            new()
            {
                Date = 20230101, ChainId = "AELF", BlockCount = 100, MissedBlockCount = 10,
                BlockProductionRate = "90.00"
            },
            new()
            {
                Date = 20230102, ChainId = "AELF", BlockCount = 150, MissedBlockCount = 20,
                BlockProductionRate = "87.50"
            },
            new()
            {
                Date = 20230101, ChainId = SideChainId, BlockCount = 50, MissedBlockCount = 5,
                BlockProductionRate = "90.91"
            },
            new()
            {
                Date = 20230102, ChainId = SideChainId, BlockCount = 75, MissedBlockCount = 10,
                BlockProductionRate = "88.24"
            }
        };

        await _blockProduceIndexRepository.AddOrUpdateManyAsync(mockDatas);

        var result = await _chartDataService.GetBlockProduceRateAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
        Assert.Equal("90.91", result.HighestBlockProductionRate.MergeBlockProductionRate);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    //pass
    public async Task GetAvgBlockDurationRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailyBlockProduceDurationIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", AvgBlockDuration = "100" },
            new() { Date = 20230101, ChainId = SideChainId, AvgBlockDuration = "120" }
        };
        await _blockProduceDurationRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetAvgBlockDurationRespAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
    }

    [Fact]
    public async Task GetCycleCountRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailyCycleCountIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", CycleCount = 10 },
            new() { Date = 20230101, ChainId = SideChainId, CycleCount = 5 }
        };
        await _cycleCountRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetCycleCountRespAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
        Assert.Equal(15, result.List.First().MergeCycleCount);
    }

    [Fact]
    public async Task GetNodeBlockProduceRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<HourNodeBlockProduceIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", NodeAddress = "Address1", Blocks = 10 },
            new() { Date = 20230101, ChainId = SideChainId, NodeAddress = "Address1", Blocks = 5 }
        };
        await _hourNodeBlockProduceRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetNodeBlockProduceRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
        Assert.Equal(15, result.List.First().Blocks);
    }

    [Fact]
    public async Task GetDailyAvgTransactionFeeRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailyAvgTransactionFeeIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", AvgFeeUsdt = "100" },
            new() { Date = 20230101, ChainId = SideChainId, AvgFeeUsdt = "120" }
        };
        await _avgTransactionFeeRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetDailyAvgTransactionFeeRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
        Assert.Equal("110", result.List.First().MergeAvgFeeUsdt); // Assuming the merge logic is to take the average
    }

    [Fact]
    public async Task GetDailyTransactionFeeRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockMainData = new List<DailyAvgTransactionFeeIndex>
        {
            new() { Date = 20230101, DateStr = "2023-01-01", ChainId = "AELF", TotalFeeElf = "1000000" },
            new() { Date = 20230102, DateStr = "2023-01-02", ChainId = "AELF", TotalFeeElf = "2000000" }
        };
        var mockSideData = new List<DailyAvgTransactionFeeIndex>
        {
            new() { Date = 20230101, DateStr = "2023-01-01", ChainId = SideChainId, TotalFeeElf = "500000" },
            new() { Date = 20230102, DateStr = "2023-01-02", ChainId = SideChainId, TotalFeeElf = "1000000" }
        };

        await _avgTransactionFeeRepository.AddOrUpdateManyAsync(mockMainData.Concat(mockSideData).ToList());

        // Act
        var result = await _chartDataService.GetDailyTransactionFeeRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);

        // For the first date
        var firstDateData = result.List.First(d => d.DateStr == "2023-01-01");
        Assert.Equal("2023-01-01", firstDateData.DateStr);

        // For the second date
        var secondDateData = result.List.First(d => d.DateStr == "2023-01-02");
        Assert.Equal("2023-01-02", secondDateData.DateStr);
    }

    [Fact]
    public async Task GetDailyTotalBurntRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailyTotalBurntIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", Burnt = "100" },
            new() { Date = 20230101, ChainId = SideChainId, Burnt = "50" }
        };
        await _totalBurntRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetDailyTotalBurntRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
        Assert.Equal("150", result.List.First().MergeBurnt);
    }

    [Fact]
    public async Task GetDailyDeployContractRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockMainData = new List<DailyDeployContractIndex>
        {
            new() { Date = 20230101, ChainId = "AELF" },
            new() { Date = 20230102, ChainId = "AELF" }
        };
        var mockSideData = new List<DailyDeployContractIndex>
        {
            new() { Date = 20230101, ChainId = SideChainId },
            new() { Date = 20230102, ChainId = SideChainId }
        };

        await _deployContractRepository.AddOrUpdateManyAsync(mockMainData.Concat(mockSideData).ToList());

        // Act
        var result = await _chartDataService.GetDailyDeployContractRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);

        // For the first date
        var firstDateData = result.List.First(d => d.Date == 20230101);

        // For the second date
        var secondDateData = result.List.First(d => d.Date == 20230102);
    }

    [Fact]
    public async Task GetElfPriceIndexRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<ElfPriceIndex>
        {
            new() { DateStr = "2023-01-01", Close = "100.0" },
            new() { DateStr = "2023-01-02", Close = "120.0" }
        };

        await _elfPriceRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetElfPriceIndexRespAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);
        Assert.Equal("120.000000", result.Highest.Price); // Highest price
        Assert.Equal("100.000000", result.Lowest.Price); // Lowest price
    }

    [Fact]
    public async Task GetDailyBlockRewardRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockMainData = new List<DailyBlockRewardIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", BlockReward = "1000" },
            new() { Date = 20230102, ChainId = "AELF", BlockReward = "2000" }
        };
        var mockSideData = new List<DailyBlockRewardIndex>
        {
            new() { Date = 20230101, ChainId = SideChainId, BlockReward = "500" },
            new() { Date = 20230102, ChainId = SideChainId, BlockReward = "1000" }
        };

        await _blockRewardRepository.AddOrUpdateManyAsync(mockMainData.Concat(mockSideData).ToList());

        // Act
        var result = await _chartDataService.GetDailyBlockRewardRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);
    }

    [Fact]
    public async Task GetDailyAvgBlockSizeRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockMainData = new List<DailyAvgBlockSizeIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", AvgBlockSize = "1000" },
            new() { Date = 20230102, ChainId = "AELF", AvgBlockSize = "2000" }
        };
        var mockSideData = new List<DailyAvgBlockSizeIndex>
        {
            new() { Date = 20230101, ChainId = SideChainId, AvgBlockSize = "500" },
            new() { Date = 20230102, ChainId = SideChainId, AvgBlockSize = "1000" }
        };

        await _blockSizeRepository.AddOrUpdateManyAsync(mockMainData.Concat(mockSideData).ToList());

        // Act
        var result = await _chartDataService.GetDailyAvgBlockSizeRespRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);

        // For the first date
        var firstDateData = result.List.First(d => d.Date == 20230101);
        Assert.Equal("750", firstDateData.MergeAvgBlockSize); // (1000 + 500) / 2

        // For the second date
        var secondDateData = result.List.First(d => d.Date == 20230102);
        Assert.Equal("1500", secondDateData.MergeAvgBlockSize); // (2000 + 1000) / 2
    }

    [Fact]
    public async Task GetDailyTotalContractCallRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockMainData = new List<DailyTotalContractCallIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", CallCount = 100 },
            new() { Date = 20230102, ChainId = "AELF", CallCount = 200 }
        };
        var mockSideData = new List<DailyTotalContractCallIndex>
        {
            new() { Date = 20230101, ChainId = SideChainId, CallCount = 50 },
            new() { Date = 20230102, ChainId = SideChainId, CallCount = 100 }
        };

        await _dailyTotalContractCallRepository.AddOrUpdateManyAsync(mockMainData.Concat(mockSideData).ToList());

        // Act
        var result = await _chartDataService.GetDailyTotalContractCallRespRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);

        // For the first date
        var firstDateData = result.List.First(d => d.Date == 20230101);
        Assert.Equal(150, firstDateData.MergeCallCount); // 100 (main) + 50 (side)

        // For the second date
        var secondDateData = result.List.First(d => d.Date == 20230102);
        Assert.Equal(300, secondDateData.MergeCallCount); // 200 (main) + 100 (side)
    }

    [Fact]
    public async Task GetTopContractCallRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailyContractCallIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", ContractAddress = "Address1", CallCount = 100 },
            new() { Date = 20230101, ChainId = "AELF", ContractAddress = "Address2", CallCount = 200 },
            new() { Date = 20230101, ChainId = SideChainId, ContractAddress = "Address1", CallCount = 50 }
        };

        await _dailyContractCallRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetTopContractCallRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);

        // For Address1
        var address1Data = result.List.First(d => d.ContractAddress == "Address1");
        Assert.Equal(150, address1Data.CallCount); // 100 (main) + 50 (side)

        // For Address2
        var address2Data = result.List.First(d => d.ContractAddress == "Address2");
        Assert.Equal(200, address2Data.CallCount); // Only main chain data
    }

    [Fact]
    public async Task GetDailyMarketCapRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockSupplyGrowthData = new List<DailySupplyGrowthIndex>
        {
            new() { Date = 20230101, ChainId = "AELF" },
            new() { Date = 20230102, ChainId = "AELF" }
        };
        var mockPriceData = new List<ElfPriceIndex>
        {
            new() { DateStr = "2023-01-01", Close = "100.0" },
            new() { DateStr = "2023-01-02", Close = "120.0" }
        };

        await _dailySupplyGrowthIndexRepository.AddOrUpdateManyAsync(mockSupplyGrowthData);
        await _elfPriceRepository.AddOrUpdateManyAsync(mockPriceData);

        // Act
        var result = await _chartDataService.GetDailyMarketCapRespAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.List.Count);
        Assert.Equal("100000000.0000", result.List[0].TotalMarketCap); // 1000000 * 100.0
        Assert.Equal("144000000.0000", result.List[1].TotalMarketCap); // 1200000 * 120.0
    }

    [Fact]
    public async Task GetDailySupplyGrowthRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailySupplyGrowthIndex>
        {
            new() { Date = 20230101, ChainId = "AELF" },
            new() { Date = 20230102, ChainId = "AELF" }
        };

        await _dailySupplyGrowthIndexRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetDailySupplyGrowthRespAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
    }

    [Fact]
    public async Task GetDailyStakedRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailyStakedIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", VoteStaked = "1000000" },
            new() { Date = 20230102, ChainId = "AELF", VoteStaked = "1200000" }
        };

        await _dailyStakedIndexRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetDailyStakedRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
    }

    [Fact]
    public async Task GetDailyHolderRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        // var mockData = new List<DailyHolder>
        // {
        //     new() { Date = 20230101, ChainId = "AELF", Count = 1000 },
        //     new() { Date = 20230102, ChainId = "AELF", Count = 1200 }
        // };
        //
        // await _dailyHolderProvider.SetDailyHolderListAsync("AELF", mockData);
        //
        // // Act
        // var result = await _chartDataService.GetDailyHolderRespAsync(new ChartDataRequest());
        //
        // // Assert
        // Assert.NotNull(result);
        // Assert.Equal(2, result.List.Count);
        // Assert.Equal(1000, result.List[0].Count);
        // Assert.Equal(1200, result.List[1].Count);
    }

    [Fact]
    public async Task GetDailyTVLRespAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var mockData = new List<DailyTVLIndex>
        {
            new() { Date = 20230101, ChainId = "AELF", TVL = 1000000 },
            new() { Date = 20230102, ChainId = "AELF", TVL = 1200000 }
        };

        await _dailyTVLRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _chartDataService.GetDailyTVLRespAsync(new ChartDataRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
    }

    [Fact]
    public async Task GetNodeProduceBlockInfoRespAsync_ShouldReturnCorrectData()
    {
        // Arrange

        //
        // // Act
        // var result = await _chartDataService.GetNodeProduceBlockInfoRespAsync(new NodeProduceBlockRequest());
        //
        // // Assert
        // Assert.NotNull(result);
        // Assert.Equal(2, result.List.Count);
        // Assert.Equal("Node1", result.List[0].NodeAddress);
        // Assert.Equal(10, result.List[0].BlockCount);
    }

    [Fact]
    public async Task InitDailyNetwork_ShouldReturnCorrectData()
    {
        // Arrange
        // var mockData = new List<RoundIndex>
        // {
        //     new() { Date = 20230101, ChainId = "AELF", RoundNumber = 1 },
        //     new() { Date = 20230102, ChainId = "AELF", RoundNumber = 2 }
        // };
        //
        // await _roundIndexRepository.AddOrUpdateManyAsync(mockData);
        //
        // // Act
        // var result = await _chartDataService.InitDailyNetwork(new SetRoundRequest());
        //
        // // Assert
        // Assert.NotNull(result);
        // Assert.Equal(2, result.RoundCount);
    }

    [Fact]
    public async Task FixDailyData_ShouldSetCorrectData()
    {
        // Arrange
        // var request = new FixDailyData { Date = "2023-01-01" };
        //
        // // Act
        // await _chartDataService.FixDailyData(request);
        //
        // // Assert
        // // Assuming RedisDatabase.StringGet is available for testing
        // var result = await RedisDatabase.StringGet(RedisKeyHelper.FixDailyData());
        // Assert.Equal(JsonConvert.SerializeObject(request), result);
    }


    [Fact]
    public async Task FixTokenHolderAsync_ShouldSetCorrectData()
    {
        // Arrange
        // var request = new FixTokenHolderInput { Date = "2023-01-01" };
        //
        // // Act
        // await _chartDataService.FixTokenHolderAsync(request);
        //
        // // Assert
        // // Assuming _cache.SetAsync is available for testing
        // var result = await _cache.GetAsync(RedisKeyHelper.FixTokenHolder());
        // Assert.Equal(JsonConvert.SerializeObject(request), result);
    }
}