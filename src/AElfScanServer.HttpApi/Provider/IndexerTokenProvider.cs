using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.ExceptionHandling;
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

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetAddressListAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId","skipCount","maxResultCount"])]
    public virtual async Task<List<AccountInfoDto>> GetAddressListAsync(string chainId, int skipCount, int maxResultCount)
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

    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetAddressTokenListAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId","skipCount","maxResultCount","symbol","addressList"])]
    public virtual async Task<List<AccountTokenDto>> GetAddressTokenListAsync(string chainId, string symbol,
        List<string> addressList, int skipCount,
        int maxResultCount)
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

    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetAddressTokenListAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId","skipCount","maxResultCount","symbol","address"])]
    public virtual async Task<List<AccountTokenDto>> GetAddressTokenListAsync(string chainId, string address, string symbol,
        int skipCount, int maxResultCount)
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

    
        
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetAddressElfBalanceAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId","address"])]
    public virtual async Task<long> GetAddressElfBalanceAsync(string chainId, string address)
    {
      
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerAccountTokenDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$address:String!,$symbol:String!,$skipCount:Int!,$maxResultCount:Int!){
                            accountToken(input: {chainId:$chainId,address:$address,symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            amount
                            }
                        }",
                    Variables = new
                    {
                        chainId = chainId, address = address, symbol = "ELF", skipCount = 0, maxResultCount = 1
                    }
                });
            return result.AccountToken.Items.Count > 0 ? result.AccountToken.Items[0].Amount : 0;
       
    }

    
    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetTransferInfoListAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId","address"])]
    public virtual async Task<List<TransferInfoDto>> GetTransferInfoListAsync(string chainId, string address, int skipCount,
        int maxResultCount)
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
}