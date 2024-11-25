using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using AElfScanServer.HttpApi.Service; 
using Xunit.Abstractions;

namespace AElfScanServer.Service
{
    public class TokenServiceTest : AElfScanServerApplicationTestBase
    {
        private readonly ITokenService _tokenService;

        public TokenServiceTest(ITestOutputHelper output) : base(output)
        {
            _tokenService = GetRequiredService<ITokenService>();
        }

        [Fact]
        public async Task GetTokenListAsync_ShouldReturnTokenList()
        {
            // Arrange
            var input = new TokenListInput
            {
                MaxResultCount = 10,
                SkipCount = 0
            };

            // Act
            var response = await _tokenService.GetTokenListAsync(input);
            // Assert
            response.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetTokenDetailAsync_ShouldReturnTokenDetail()
        {
            // Arrange
            var symbol = "TOKEN";
            var chainId = "AELF";

            // Act
            var response = await _tokenService.GetTokenDetailAsync(symbol, chainId);

            // Assert
            response.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetMergeTokenDetailAsync_ShouldReturnMergeTokenDetail()
        {
            // Arrange
            var symbol = "MERGETOKEN";
            var chainId = "AELF";

            // Act
            var response = await _tokenService.GetMergeTokenDetailAsync(symbol, chainId);

            // Assert
            response.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetTokenTransferInfosAsync_ShouldReturnTokenTransferInfo()
        {
            // Arrange
            var input = new TokenTransferInput
            {
            };

            // Act
            var response = await _tokenService.GetTokenTransferInfosAsync(input);

            // Assert
            response.ShouldNotBeNull();
            // 验证返回的转账信息
        }

        [Fact]
        public async Task GetTokenHolderInfosAsync_ShouldReturnTokenHolderInfos()
        {
            // Arrange
            var input = new TokenHolderInput
            {
            };

            // Act
            var response = await _tokenService.GetTokenHolderInfosAsync(input);

            // Assert
            response.ShouldNotBeNull();
            response.List.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task GetTokenPriceInfoAsync_ShouldReturnTokenPriceInfo()
        {
            // Arrange
            var input = new CurrencyDto
            {
            };

            // Act
            var response = await _tokenService.GetTokenPriceInfoAsync(input);

            // Assert
            response.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetTokenBaseInfoAsync_ShouldReturnTokenBaseInfo()
        {
            // Arrange
            var symbol = "BASEINFO";
            var chainId = "AELF";

            // Act
            var response = await _tokenService.GetTokenBaseInfoAsync(symbol, chainId);

            // Assert
            response.ShouldNotBeNull();
            // 验证代币的基本信息
        }
    }
}