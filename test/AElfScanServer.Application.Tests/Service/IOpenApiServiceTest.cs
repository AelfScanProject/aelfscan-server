using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.ThirdPart.Exchange;
using AElfScanServer.HttpApi.Dtos.ChartData;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.HttpApi.Service;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer.Service;

public class IOpenApiServiceTest : AElfScanServerApplicationTestBase
{
    private readonly IOpenApiService _openApiService;
    private readonly IChartDataService _chartDataService;
    private readonly ITokenIndexerProvider _indexerTokenProvider;
    private readonly IEntityMappingRepository<DailyTransactionCountIndex, string> _transactionCountRepository;
    private readonly IEntityMappingRepository<DailySupplyGrowthIndex, string> _dailySupplyGrowthIndexRepository;
    private readonly IEntityMappingRepository<DailyActiveAddressCountIndex, string> _activeAddressRepository;


    public IOpenApiServiceTest(ITestOutputHelper output) : base(output)
    {
        _openApiService = GetRequiredService<IOpenApiService>();
        _chartDataService = GetRequiredService<IChartDataService>();
        _indexerTokenProvider = GetRequiredService<ITokenIndexerProvider>();
        _dailySupplyGrowthIndexRepository =
            GetRequiredService<IEntityMappingRepository<DailySupplyGrowthIndex, string>>();
        _activeAddressRepository = GetRequiredService<IEntityMappingRepository<DailyActiveAddressCountIndex, string>>();
        _transactionCountRepository =
            GetRequiredService<IEntityMappingRepository<DailyTransactionCountIndex, string>>();
    }


    [Fact]
    //pass
    public async Task GetSupplyAsync_ShouldReturnCorrectSupplyData()
    {
        // Arrange
        var mockData = new List<DailySupplyGrowthIndex>
        {
            new()
            {
                Date = 20230101,
                ChainId = "AELF",
                TotalBurnt = 500000,
                DailyOrganizationBalance = 100000000,
                TotalOrganizationBalance = 200000000
            },
            new()
            {
                Date = 20230102,
                ChainId = "AELF",
                TotalBurnt = 600000,
                DailyOrganizationBalance = 100000000,
                TotalOrganizationBalance = 200000000
            }
        };

        await _dailySupplyGrowthIndexRepository.AddOrUpdateManyAsync(mockData);

        // Act
        var result = await _openApiService.GetSupplyAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000000000, result.MaxSupply);
        Assert.Equal(600000, result.Burn);
        Assert.Equal(999400000, result.TotalSupply);
    }

    [Fact]
    public async Task GetDailyActivityAddressAsync_ShouldReturnCorrectActivityAddressData_MultipleDays()
    {
        // Arrange
        var startDate =
            new DateTime(2023, 1, 1).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-01的毫秒时间戳
        var secondDate =
            new DateTime(2023, 1, 2).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-02的毫秒时间戳
        var thirdDate =
            new DateTime(2023, 1, 3).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-03的毫秒时间戳
        var fourthDate =
            new DateTime(2023, 1, 4).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-04的毫秒时间戳

        var mockDatas = new List<DailyActiveAddressCountIndex>()
        {
            new()
            {
                Date = (long)startDate, DateStr = "2023-01-01", ChainId = "AELF", AddressCount = 100,
                ReceiveAddressCount = 60,
                SendAddressCount = 40
            },
            new()
            {
                Date = (long)secondDate, DateStr = "2023-01-02", ChainId = "AELF", AddressCount = 150,
                ReceiveAddressCount = 90,
                SendAddressCount = 60
            },
            new()
            {
                Date = (long)thirdDate, DateStr = "2023-01-03", ChainId = "AELF", AddressCount = 120,
                ReceiveAddressCount = 70,
                SendAddressCount = 50
            },
            new()
            {
                Date = (long)fourthDate, DateStr = "2023-01-04", ChainId = "AELF", AddressCount = 180,
                ReceiveAddressCount = 100,
                SendAddressCount = 80
            },
            new()
            {
                Date = (long)startDate, DateStr = "2023-01-01", ChainId = "tDVW", AddressCount = 50,
                ReceiveAddressCount = 30, SendAddressCount = 20
            },
            new()
            {
                Date = (long)secondDate, DateStr = "2023-01-02", ChainId = "tDVW", AddressCount = 75,
                ReceiveAddressCount = 45, SendAddressCount = 30
            },
            new()
            {
                Date = (long)thirdDate, DateStr = "2023-01-03", ChainId = "tDVW", AddressCount = 60,
                ReceiveAddressCount = 35, SendAddressCount = 25
            },
            new()
            {
                Date = (long)fourthDate, DateStr = "2023-01-04", ChainId = "tDVW", AddressCount = 90,
                ReceiveAddressCount = 48, SendAddressCount = 37
            }
        };

        await _activeAddressRepository.AddOrUpdateManyAsync(mockDatas);

        // Act
        var result = await _openApiService.GetDailyActivityAddressAsync("2023-01-01", "2023-01-04");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150, result.MainChain.Max);
        Assert.Equal(100, result.MainChain.Min);
        Assert.Equal(123, result.MainChain.Avg);
    }


    [Fact]
    //pass
    public async Task GetDailyTransactionCountAsync_ShouldReturnCorrectTransactionCountData_MultipleDays()
    {
        // Arrange
        var startDate =
            new DateTime(2023, 1, 1).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-01的毫秒时间戳
        var secondDate =
            new DateTime(2023, 1, 2).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-02的毫秒时间戳
        var thirdDate =
            new DateTime(2023, 1, 3).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-03的毫秒时间戳
        var fourthDate =
            new DateTime(2023, 1, 4).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // 2023-01-04的毫秒时间戳

        var mockDatas = new List<DailyTransactionCountIndex>()
        {
            new() { Date = (long)startDate, DateStr = "2023-01-01", ChainId = "AELF", TransactionCount = 100 },
            new() { Date = (long)secondDate, DateStr = "2023-01-02", ChainId = "AELF", TransactionCount = 200 },
            new() { Date = (long)thirdDate, DateStr = "2023-01-03", ChainId = "AELF", TransactionCount = 300 },
            new() { Date = (long)fourthDate, DateStr = "2023-01-04", ChainId = "AELF", TransactionCount = 400 },
            new() { Date = (long)startDate, DateStr = "2023-01-01", ChainId = "tDVW", TransactionCount = 50 },
            new() { Date = (long)secondDate, DateStr = "2023-01-02", ChainId = "tDVW", TransactionCount = 80 },
            new() { Date = (long)thirdDate, DateStr = "2023-01-03", ChainId = "tDVW", TransactionCount = 120 },
            new() { Date = (long)fourthDate, DateStr = "2023-01-04", ChainId = "tDVW", TransactionCount = 160 }
        };

        await _transactionCountRepository.AddOrUpdateManyAsync(mockDatas);

        // Act
        var result = await _openApiService.GetDailyTransactionCountAsync("2023-01-01", "2023-01-04");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(166, result.MainChain.TransactionAvgByAllType);
        Assert.Equal(166, result.MainChain.TransactionAvgByExcludeSystem);
    }


    [Fact]
    public async Task GetCurrencyPriceAsyncTest()
    {
        var mockData = new List<DailySupplyGrowthIndex>
        {
            new()
            {
                Date = 20230101,
                ChainId = "AELF",
                TotalBurnt = 500000,
                DailyOrganizationBalance = 100000000,
                TotalOrganizationBalance = 200000000
            },
            new()
            {
                Date = 20230102,
                ChainId = "AELF",
                TotalBurnt = 600000,
                DailyOrganizationBalance = 100000000,
                TotalOrganizationBalance = 200000000
            }
        };

        await _dailySupplyGrowthIndexRepository.AddOrUpdateManyAsync(mockData);
        
        var result = await _openApiService.GetCurrencyPriceAsync();
        
        Assert.NotNull(result);
    }
}