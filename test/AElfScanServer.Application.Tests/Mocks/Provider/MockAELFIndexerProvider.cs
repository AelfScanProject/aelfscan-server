using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Enums;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Provider;

namespace AElfScanServer.Mocks.Provider;

public class MockAELFIndexerProvider : IAELFIndexerProvider
{
    public Task<List<IndexerBlockDto>> GetLatestBlocksAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var mockBlockDto = new List<IndexerBlockDto>
        {
            new IndexerBlockDto
            {
                ChainId = "MockChainId",
                BlockHash = "MockBlockHash",
                BlockHeight = 100,  // example block height
                BlockTime = DateTime.UtcNow,  // current time as an example
                Miner = "MockMinerAddress",
                Confirmed = true,  // assuming the block is confirmed
                TransactionIds = new List<string>
                {
                    "MockTransactionId1",
                    "MockTransactionId2",
                    "MockTransactionId3"
                }
            }
        };

        return Task.FromResult(mockBlockDto);
    }

    public Task<long> GetLatestBlockHeightAsync(string chainId)
    {
        return Task.FromResult(123456L); // Mocked block height
    }

    public Task<List<IndexSummaries>> GetLatestSummariesAsync(string chainId)
    {
        var mockSummaries = new List<IndexSummaries>
        {
            new IndexSummaries
            {
                // Populate fields with mock data
            }
        };

        return Task.FromResult(mockSummaries);
    }

    public Task<List<TransactionIndex>> GetTransactionsAsync(
        string chainId,
        long startBlockHeight,
        long endBlockHeight,
        string transactionId
    )
    {
        var mockTransactions = new List<TransactionIndex>
        {
            new  TransactionIndex
            {
            Id = Guid.NewGuid().ToString(), 
            TransactionId = Guid.NewGuid().ToString(),
            ChainId = "AELF", 
            From = "address_from",
            To = "address_to", 
            BlockHeight = 123456, 
            Signature = "mock_signature", 
            Confirmed = true, 
            BlockTime = DateTime.UtcNow, 
            Status = TransactionStatus.Mined, 
            MethodName = "Transfer", 
            DateStr = DateTime.UtcNow.ToString("yyyyMMdd"), 
            ExtraProperties = new Dictionary<string, string> 
            {
                { "Property1", "Value1" },
                { "Property2", "Value2" }
            },
            LogEvents = new List<IndexerLogEventDto>
            {
                 new IndexerLogEventDto
                 {
                     ChainId = "AELF",
                     BlockHeight = 100,
                     TransactionId = "TransactionId",
                     BlockTime = default,
                     ContractAddress = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
                     EventName = "Transferred",
                     Index = 0,
                     ExtraProperties = new Dictionary<string, string>()
                     {
                     }
                 }
            }
            } 
        };

        return Task.FromResult(mockTransactions);
    }

    public Task<List<TransactionData>> GetTransactionsDataAsync(
        string chainId,
        long startBlockHeight,
        long endBlockHeight,
        string transactionId
    )
    {
        var mockTransactionData = new List<TransactionData>
        {
            new TransactionData
            {
                // Populate fields with mock data
            }
        };

        return Task.FromResult(mockTransactionData);
    }

    
}