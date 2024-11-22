using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Common.Enums;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.HttpApi.Provider;

namespace AElfScanServer.Mocks.Provider;

public class MockBlockChainIndexerProvider :IBlockChainIndexerProvider
{
    public async Task<IndexerTransactionListResultDto> GetTransactionsAsync(TransactionsRequestDto req)
    {
        return new IndexerTransactionListResultDto
        {
            TotalCount = 1,
            Items =
            {
                new IndexerTransactionInfoDto
                {
                    TransactionId = "TransactionId",
                    BlockHeight = 100,
                    MethodName = "ss",
                    Status = TransactionStatus.NotExisted,
                    From = "From",
                    To = "To",
                    TransactionValue = 0,
                    Fee = 0,
                    Metadata = new MetadataDto
                    {
                        ChainId = "ChainId",
                        Block = new BlockMetadataDto
                        {
                            BlockHash = "BlockHash",
                            BlockHeight = 100,
                            BlockTime = default
                        }
                    }
                }
            }
        };
    }

    public Task<long> GetTransactionCount(string chainId)
    {
        throw new System.NotImplementedException();
    }

    public Task<IndexerTransactionListResultDto> GetTransactionsByHashsAsync(TransactionsByHashRequestDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<IndexerAddressTransactionCountDto>> GetAddressTransactionCount(string chainId, List<string> addressList)
    {
        throw new System.NotImplementedException();
    }
}