using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using Moq;
using Xunit;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Dtos.ChartData;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Options;

public class ChartDataServiceTests
{
    [Fact]
    public async Task GetDailyTransactionCountAsync_ShouldProcessDataCorrectly()
    {
        var mockTransactionCountRepository = new Mock<IEntityMappingRepository<DailyTransactionCountIndex, string>>();
        var mockObjectMapper = new Mock<IObjectMapper>();
        var mockOptionsMonitor = new Mock<IOptionsMonitor<GlobalOptions>>();

        var globalOptions = new GlobalOptions
        {
            SideChainId = "SIDE_CHAIN"
        };
        mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(globalOptions);

        var mainChainData = new List<DailyTransactionCountIndex>
        {
            new() { Date = 20230101, DateStr = "2023-01-01", ChainId = "AELF", TransactionCount = 100 },
            new() { Date = 20230102, DateStr = "2023-01-02", ChainId = "AELF", TransactionCount = 200 }
        };

        var sideChainData = new List<DailyTransactionCountIndex>
        {
            new() { Date = 20230101, DateStr = "2023-01-01", ChainId = "SIDE_CHAIN", TransactionCount = 50 },
            new() { Date = 20230102, DateStr = "2023-01-02", ChainId = "SIDE_CHAIN", TransactionCount = 80 }
        };

        mockTransactionCountRepository
            .Setup(repo => repo.GetQueryableAsync(null, default))
            .ReturnsAsync(mainChainData.Concat(sideChainData).AsQueryable());

        mockObjectMapper
            .Setup(mapper =>
                mapper.Map<List<DailyTransactionCountIndex>, List<DailyTransactionCount>>(
                    It.IsAny<List<DailyTransactionCountIndex>>()))
            .Returns((List<DailyTransactionCountIndex> source) =>
                source.Select(x => new DailyTransactionCount
                {
                    Date = x.Date,
                    DateStr = x.DateStr,
                    TransactionCount = x.TransactionCount,
                    MergeTransactionCount = 0,
                    MainChainTransactionCount = 0,
                    SideChainTransactionCount = 0
                }).ToList());

        var mockOptionsAccessor = new Mock<IOptions<RedisCacheOptions>>();
        mockOptionsAccessor.Setup(o => o.Value).Returns(new RedisCacheOptions());
        var service = new ChartDataService(
            mockOptionsAccessor.Object,
            mockObjectMapper.Object,
            mockOptionsMonitor.Object,
            mockTransactionCountRepository.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null, null, null, null, null,
            null, null, null,
            null, null, null, null,
            null, null, null, null,
            null);

        var request = new ChartDataRequest();

        var result = await service.GetDailyTransactionCountAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.List.Count);
        Assert.Equal(280, result.HighestTransactionCount.MergeTransactionCount); // 主链 200 + 子链 100
        Assert.Equal(150, result.LowesTransactionCount.MergeTransactionCount); // 主链 100 + 子链 50
        Assert.Equal(2, result.Total);

        // 验证映射行为
        mockObjectMapper.Verify(mapper =>
            mapper.Map<List<DailyTransactionCountIndex>, List<DailyTransactionCount>>(
                It.IsAny<List<DailyTransactionCountIndex>>()), Times.Exactly(2));
    }
}