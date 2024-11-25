using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Mocks.Provider;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer
{
    public class ContractAppServiceTest : AElfScanServerApplicationTestBase
    {
        private readonly IContractAppService _contractAppService;

        public ContractAppServiceTest(ITestOutputHelper output) : base(output)
        {
            _contractAppService = GetRequiredService<IContractAppService>();
        }

        [Fact]
        public async Task GetContractListAsync_Test()
        {
            // Arrange
            var input = new GetContractContracts
            {
               
            };

            // Act
            var result = await _contractAppService.GetContractListAsync(input);

            // Assert
            result.ShouldNotBeNull();
           
        }

        [Fact]
        public async Task GetContractFileAsync_Test()
        {
            // Arrange
            var input = new GetContractFileInput
            {
                // 初始化输入参数
            };

            // Act
            var result = await _contractAppService.GetContractFileAsync(input);

            // Assert
            result.ShouldNotBeNull();
            // 添加更多断言以验证文件内容
        }

        [Fact]
        public async Task GetContractHistoryAsync_Test()
        {
            // Arrange
            var input = new GetContractHistoryInput
            {
                // 初始化输入参数
            };

            // Act
            var result = await _contractAppService.GetContractHistoryAsync(input);

            // Assert
            result.ShouldNotBeNull();
            // 根据历史数据添加具体的断言
        }

        [Fact]
        public async Task GetContractEventsAsync_Test()
        {
            // Arrange
            var input = new GetContractEventReq
            {
                // 初始化输入参数
            };

            // Act
            var result = await _contractAppService.GetContractEventsAsync(input);

            // Assert
            result.ShouldNotBeNull();
            // 对返回的事件数据进行断言
        }

        [Fact]
        public async Task SaveContractFileAsync_Test()
        {
            // Arrange
            var chainId = "sampleChainId";

            // Act
            await _contractAppService.SaveContractFileAsync(chainId);

            // Assert
            
        }

        [Fact]
        public async Task UpdateContractHeightAsync_Test()
        {
            // Arrange
            var input = new SynchronizationContractDto
            {
                ChainId = MockUtil.MainChainId,
                BizType = "File",
                LastBlockHeight = 10
            };

            // Act
            await _contractAppService.UpdateContractHeightAsync(input);

            // Assert
           
        }
    }
}