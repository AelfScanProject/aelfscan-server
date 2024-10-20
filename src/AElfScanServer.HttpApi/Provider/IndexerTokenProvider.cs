using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.GraphQL;

using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public interface IIndexerTokenProvider
{
    Task<List<AccountInfoDto>> GetAddressListAsync(string chainId, int skipCount, int maxResultCount);

    Task<List<AccountTokenDto>> GetAddressTokenListAsync(string chainId, string symbol, List<string> addressList,
        int skipCount = 0,
        int maxResultCount = 10);

    Task<List<AccountTokenDto>> GetAddressTokenListAsync(string chainId, string address,
        string symbol,
        int skipCount = 0, int maxResultCount = 10);

    Task<long> GetAddressElfBalanceAsync(string chainId, string address);


    Task<List<TransferInfoDto>> GetTransferInfoListAsync(string chainId, string address, int skipCount = 0,
        int maxResultCount = 10);
}

public class IndexerTokenProvider : IIndexerTokenProvider, ISingletonDependency
{
    private readonly GraphQlFactory _graphQlFactory;
    private readonly ILogger<IndexerTokenProvider> _logger;
    private const string IndexerType = AElfIndexerConstant.TokenIndexer;

    public IndexerTokenProvider(GraphQlFactory graphQlFactory, ILogger<IndexerTokenProvider> logger)
    {
        _logger = logger;
        _graphQlFactory = graphQlFactory;
    }

    public async Task<List<AccountInfoDto>> GetAddressListAsync(string chainId, int skipCount, int maxResultCount)
    {
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerAddressListDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!){
                        accountInfo(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,
                            chainId,
                            blockHash,
                            blockHeight,
                            blockTime,
                            address,
                            tokenHoldingCount,
                            transferCount
                        }
                    }",
                    Variables = new
                    {
                        chainId = "AELF", skipCount = skipCount, maxResultCount = maxResultCount
                    }
                });
            return result.AccountInfo;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query AccountInfo failed.");
            return new List<AccountInfoDto>();
        }
    }

    public async Task<List<AccountTokenDto>> GetAddressTokenListAsync(string chainId, string symbol,
        List<string> addressList, int skipCount,
        int maxResultCount)
    {
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerAccountTokenDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$symbol:String!,$addressList:[String!],$skipCount:Int!,$maxResultCount:Int!){
                            accountToken(input: {chainId:$chainId,symbol:$symbol,skipCount:$skipCount,addressList:$addressList,maxResultCount:$maxResultCount})
                           {
                                totalCount
                                items {
                                  address
                                  token {
                                    symbol
                                    collectionSymbol
                                    type
                                    decimals
                                  }
                                  amount
                                  formatAmount
                                  transferCount
                                  firstNftTransactionId
                                  firstNftTime
                                  metadata {
                                    chainId
                                    block {
                                      blockHash
                                      blockTime
                                      blockHeight
                                    }
                                  }
                                }
                            }
                        }",
                    Variables = new
                    {
                        chainId = chainId, symbol = symbol, skipCount = skipCount, maxResultCount = maxResultCount,
                        addressList = addressList
                    }
                });
            return result.AccountToken != null ? result.AccountToken.Items : new List<AccountTokenDto>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query AccountToken failed.");
            return new List<AccountTokenDto>();
        }
    }

    public async Task<List<AccountTokenDto>> GetAddressTokenListAsync(string chainId, string address, string symbol,
        int skipCount, int maxResultCount)
    {
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerAccountTokenDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$symbol:String!,$address:String!,$skipCount:Int!,$maxResultCount:Int!){
                            accountToken(input: {chainId:$chainId,symbol:$symbol,address:$address,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            totalCount
                            items {
                              address
                              token {
                                symbol
                                collectionSymbol
                                type
                                decimals
                              }
                              amount
                              formatAmount
                              transferCount
                              firstNftTransactionId
                              firstNftTime
                            }
                            }
                        }",
                    Variables = new
                    {
                        chainId = chainId, address = address, symbol = symbol, skipCount = skipCount,
                        maxResultCount = maxResultCount
                    }
                });
            return result.AccountToken != null ? result.AccountToken.Items : new List<AccountTokenDto>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query AccountToken failed.");
            return new List<AccountTokenDto>();
        }
    }

    public async Task<long> GetAddressElfBalanceAsync(string chainId, string address)
    {
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerAccountTokenDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$address:String!,$symbol:String!,$skipCount:Int!,$maxResultCount:Int!){
                            accountToken(input: {chainId:$chainId,address:$address,symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items {
                              amount
                            }
                            }
                        }",
                    Variables = new
                    {
                        chainId = chainId, address = address, symbol = "ELF", skipCount = 0, maxResultCount = 1
                    }
                });
            return result.AccountToken.Items.Count > 0 ? result.AccountToken.Items[0].Amount : 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Account ELF Balance failed.");
            return 0;
        }
    }

    public async Task<List<TransferInfoDto>> GetTransferInfoListAsync(string chainId, string address, int skipCount,
        int maxResultCount)
    {
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerTransferInfoListDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$address:String!,$skipCount:Int!,$maxResultCount:Int!){
                            transferInfo(input: {chainId:$chainId,address:$address,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                                id,
                                chainId,
                                blockHash,
                                blockHeight,
                                blockTime,
                                transactionId,
                                from,
                                to,
                                method,
                                amount,
                                formatAmount,
                                token{
                                   symbol,
                                   collectionSymbol,
                                   type,
                                   decimals
                                },
                                memo,
                                toChainId,
                                fromChainId,
                                issueChainId,
                                parentChainHeight,
                                transferTransactionId
                        }
                    }",
                    Variables = new
                    {
                        chainId = chainId, address = address, skipCount = skipCount, maxResultCount = maxResultCount
                    }
                });
            return result.TransferInfo;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query TransferInfo failed.");
            return new List<TransferInfoDto>();
        }
    }
}