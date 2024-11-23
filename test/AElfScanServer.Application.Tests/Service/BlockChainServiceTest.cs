using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Service;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer;

public class BlockChainServiceTest : AElfScanServerApplicationTestBase
{
    private readonly IBlockChainService _blockChainService;

    public BlockChainServiceTest(ITestOutputHelper output) : base(output)
    {
        _blockChainService = GetRequiredService<IBlockChainService>();
    }

    [Fact]
    public async Task GetTransactionsAsync_Test()
    {
        // Arrange
        var requestDto = new TransactionsRequestDto
        {
            // 初始化您的请求参数
        };

        // Act
        var result = await _blockChainService.GetTransactionsAsync(requestDto);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetBlocksAsync_Test()
    {
        // Arrange
        var requestDto = new BlocksRequestDto
        {
            // 初始化您的请求参数
        };

        // Act
        var result = await _blockChainService.GetBlocksAsync(requestDto);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetBlockDetailAsync_Test()
    {
        // Arrange
        var requestDto = new BlockDetailRequestDto
        {
            // 初始化您的请求参数
        };

        // Act
        var result = await _blockChainService.GetBlockDetailAsync(requestDto);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTransactionDetailAsync_Test()
    {
        // Arrange
        var request = new TransactionDetailRequestDto
        {
            // 初始化您的请求参数
        };

        // Act
        var result = await _blockChainService.GetTransactionDetailAsync(request);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetLogEventsAsync_Test()
    {
        // Arrange
        var request = new GetLogEventRequestDto
        {
            // 初始化您的请求参数
        };

        // Act
        var result = await _blockChainService.GetLogEventsAsync(request);

        // Assert
        result.ShouldNotBeNull();
    }
}