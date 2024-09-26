using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.GraphQL;
using AElfScanServer.HttpApi.Dtos;
using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.HttpApi.Provider;

public interface IBlockChainIndexerProvider
{
    public Task<IndexerTransactionListResultDto>
        GetTransactionsAsync(TransactionsRequestDto req);

    public Task<long> GetTransactionCount(string chainId);

    public Task<IndexerTransactionListResultDto> GetTransactionsByHashsAsync(TransactionsByHashRequestDto input);

    public Task<List<IndexerAddressTransactionCountDto>> GetAddressTransactionCount(string chainId,
        List<string> addressList);
}

public class BlockChainIndexerProvider : IBlockChainIndexerProvider, ISingletonDependency
{
    private readonly IGraphQlFactory _graphQlFactory;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<BlockChainIndexerProvider> _logger;

    public BlockChainIndexerProvider(IGraphQlFactory graphQlFactory, IObjectMapper objectMapper,
        ILogger<BlockChainIndexerProvider> logger)
    {
        _graphQlFactory = graphQlFactory;
        _objectMapper = objectMapper;
        _logger = logger;
    }


    public async Task<List<IndexerAddressTransactionCountDto>> GetAddressTransactionCount(string chainId,
        List<string> addressList)
    {
        var graphQlHelper = _graphQlFactory.GetGraphQlHelper(AElfIndexerConstant.BlockChainIndexer);

        var indexerResult = await graphQlHelper.QueryAsync<IndexerAddressTransactionCountResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$addressList:[String!]){
                    addressTransactionCount(input: {chainId:$chainId,addressList:$addressList})
                {
         
                    items {
                          count
                          chainId
                          address
                         
                    }
                }
            }",
            Variables = new
            {
                chainId = chainId, addressList = addressList
            }
        });
        return indexerResult.AddressTransactionCount.Items;
    }

    public async Task<IndexerTransactionListResultDto> GetTransactionsAsync(TransactionsRequestDto input)
    {
        var graphQlHelper = _graphQlFactory.GetGraphQlHelper(AElfIndexerConstant.BlockChainIndexer);

        var indexerResult = await graphQlHelper.QueryAsync<IndexerTransactionResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$skipCount:Int!,$maxResultCount:Int!,$startTime:Long!,$endTime:Long!,$address:String!,$searchAfter:[String],$orderInfos:[OrderInfo]){
                    transactionInfos(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,startTime:$startTime,endTime:$endTime,address:$address,searchAfter:$searchAfter,orderInfos:$orderInfos})
                {
                  totalCount
                    items {
                       transactionId
                          blockHeight
                          chainId
                          methodName
                          status
                          from
                          to
                          transactionValue
                          fee
                          metadata {
                            chainId
                            block {
                              blockHash
                              blockHeight
                              blockTime
                            }
                          }
                    }
                }
            }",
            Variables = new
            {
                chainId = input.ChainId, skipCount = input.SkipCount, maxResultCount = input.MaxResultCount,
                startTime = input.StartTime,
                endTime = input.EndTime, address = input.Address,
                orderInfos = input.OrderInfos, searchAfter = input.SearchAfter
            }
        });
        return indexerResult?.TransactionInfos;
    }

    public async Task<IndexerTransactionListResultDto> GetTransactionsByHashsAsync(TransactionsByHashRequestDto input)
    {
        var graphQlHelper = _graphQlFactory.GetGraphQlHelper(AElfIndexerConstant.BlockChainIndexer);

        var indexerResult = await graphQlHelper.QueryAsync<IndexerTransactionByHashResultDto>(new GraphQLRequest
        {
            Query =
                @"query($hashs:[String!],$skipCount:Int!,$maxResultCount:Int!){
                    transactionByHash(input: {hashs:$hashs,skipCount:$skipCount,maxResultCount:$maxResultCount})
                {
                    totalCount
                    items {
                       transactionId
                          blockHeight
                          chainId
                          methodName
                          status
                          from
                          to
                          transactionValue
                          fee
                          metadata {
                            chainId
                            block {
                              blockHash
                              blockHeight
                              blockTime
                            }
                          }
                    }
                }
            }",
            Variables = new
            {
                skipCount = input.SkipCount, maxResultCount = input.MaxResultCount,
                hashs = input.Hashs
            }
        });
        return indexerResult?.TransactionByHash;
    }


    public async Task<long> GetTransactionCount(string chainId)
    {
        try
        {
            var graphQlHelper = _graphQlFactory.GetGraphQlHelper(AElfIndexerConstant.BlockChainIndexer);

            var indexerResult = await graphQlHelper.QueryAsync<IndexerTransactionCountResultDto>(new GraphQLRequest
            {
                Query =
                    @"query($chainId:String){
                    transactionCount(input: {chainId:$chainId})
                {
                   count
                }
            }",
                Variables = new
                {
                    chainId = chainId,
                }
            });
            return indexerResult.TransactionCount.Count;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get transaction count error from blockchain app plugin:{chainId}", chainId);
        }

        return 0;
    }
}