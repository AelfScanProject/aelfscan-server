using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.HttpApi.Service;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer.Service
{
    public class HomePageServiceTest : AElfScanServerApplicationTestBase
    {
        private readonly IHomePageService _homePageService;
        private readonly ICacheProvider _cacheProvider;

        public HomePageServiceTest(ITestOutputHelper output) : base(output)
        {
            _homePageService = GetRequiredService<IHomePageService>();
            _cacheProvider = GetRequiredService<ICacheProvider>();
        }

        [Fact]
        public async Task GetTransactionPerMinuteAsync_ShouldReturnExpectedResult()
        {
            // Arrange
            var chainId = "AELF";
            var key = RedisKeyHelper.TransactionChartData(chainId);
            await _cacheProvider.StringSetAsync(key, JsonConvert.SerializeObject(new List<TransactionCountPerMinuteDto>()
            {
                new ()
                {
                    Start = 10,
                    End = 20,
                    Count = 2
                }
            }),null);
            await _cacheProvider.StringSetAsync(RedisKeyHelper.TransactionChartData("merge"), JsonConvert.SerializeObject(new List<TransactionCountPerMinuteDto>()
            {
                new ()
                {
                    Start = 10,
                    End = 20,
                    Count = 2
                }
            }),null);
            // Act
            var response = await _homePageService.GetTransactionPerMinuteAsync(chainId);

            // Assert
            response.ShouldNotBeNull();
            response.Owner[0].Start.ShouldBe(10);
            response.Owner[0].End.ShouldBe(20);
            response.Owner[0].Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetFilterType_ShouldReturnExpectedFilterType()
        {
            // Act
            var filterTypeResponse = await _homePageService.GetFilterType();

            // Assert
            filterTypeResponse.ShouldNotBeNull();
        }
    }
}