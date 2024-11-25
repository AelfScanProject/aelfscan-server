using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Mocks.Provider;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer
{
    public class DynamicTransactionServiceTest : AElfScanServerApplicationTestBase
    {
        private readonly IDynamicTransactionService _dynamicTransactionService;

        public DynamicTransactionServiceTest(ITestOutputHelper output) : base(output)
        {
            _dynamicTransactionService = GetRequiredService<IDynamicTransactionService>();
        }

        [Fact]
        public async Task GetTransactionsAsync_ShouldReturnExpectedResponse()
        {
            // Arrange
            var request = new TransactionsRequestDto
            {
               
                ChainId = MockUtil.MainChainId,
                SkipCount = 0,
                MaxResultCount = 10
            };

            // Act
            var response = await _dynamicTransactionService.GetTransactionsAsync(request);

            // Assert
            response.Transactions.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetTransactionDetailAsync_ShouldReturnExpectedDetail()
        {
            // Arrange
            var request = new TransactionDetailRequestDto
            {
                TransactionId = "TransactionId",
            };

            // Act
            var detailResponse = await _dynamicTransactionService.GetTransactionDetailAsync(request);

            // Assert
            detailResponse.ShouldNotBeNull();
        }
    }
}