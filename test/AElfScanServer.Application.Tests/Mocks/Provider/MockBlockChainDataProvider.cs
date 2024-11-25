using System.Threading.Tasks;
using AElf.Types;
using AElfScanServer.Common.Dtos;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Provider;
using Header = AElfScanServer.HttpApi.Dtos.Header;

namespace AElfScanServer.Mocks.Provider;

public class MockBlockChainDataProvider : IBlockChainDataProvider
{
    public Task<string> GetBlockRewardAsync(long blockHeight, string chainId)
    {
        return Task.FromResult("0.3125");
    }

    public async Task<string> GetContractAddressAsync(string chainId, string contractName)
    {
        return "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
    }

    public Task<string> TransformTokenToUsdValueAsync(string symbol, long amount, string chainId)
    {
        return Task.FromResult("0.3");
    }

    public Task<string> GetDecimalAmountAsync(string symbol, long amount, string chainId)
    {
        return Task.FromResult("100");
    }

    public Task<string> GetTokenUsdPriceAsync(string symbol)
    {
        return Task.FromResult("1");
    }

    public Task<BinancePriceDto> GetTokenUsd24ChangeAsync(string symbol)
    {
        var mockPriceDto = new BinancePriceDto
        {
            Symbol = "ELF",
            PriceChangePercent = 85,
            LastPrice = 1
        };
        return Task.FromResult(mockPriceDto);
    }

    public Task<int> GetTokenDecimals(string symbol, string chainId)
    {
        return Task.FromResult(8); // Mocked decimal value
    }

    public Task<BlockDetailDto> GetBlockDetailAsync(string chainId, long blockHeight)
    {
        var mockBlockDetail = new BlockDetailDto
        {
            Header = new Header
            {
                PreviousBlockHash = "PreviousBlockHash",
                MerkleTreeRootOfTransactions = "MerkleTreeRootOfTransactions",
                MerkleTreeRootOfWorldState = "MerkleTreeRootOfWorldState",
                MerkleTreeRootOfTransactionState = "MerkleTreeRootOfTransactionState"
            },
            BlockSize = 1000
        };
        return Task.FromResult(mockBlockDetail);
    }

    public Task<NodeTransactionDto> GetTransactionDetailAsync(string chainId, string transactionId)
    {
        var mockTransactionDetail = new NodeTransactionDto
        {
            // Fill with mocked data
        };
        return Task.FromResult(mockTransactionDetail);
    }

    public Task<string> GeFormatTransactionParamAsync(string chainId, string contractAddress, string methodName, string param)
    {
        return Task.FromResult("Mocked Formatted Transaction Param");
    }
}