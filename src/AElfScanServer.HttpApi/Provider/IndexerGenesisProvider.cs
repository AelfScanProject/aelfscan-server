using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.GraphQL;
using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public interface IIndexerGenesisProvider
{
    Task<IndexerContractListResultDto> GetContractListAsync(string chainId, int skipCount,
        int maxResultCount, string orderBy, string sort, string address, long blockHeight = 0);


    Task<Dictionary<string, ContractInfoDto>> GetContractListAsync(string chainId, List<string> addresslist);

    Task<List<ContractRecordDto>> GetContractRecordAsync(string chainId, string address, int skipCount = 0,
        int maxResultCount = 10);

    Task<List<ContractRegistrationDto>> GetContractRegistrationAsync(string chainId, string codeHash,
        int skipCount = 0, int maxResultCount = 0);
}

public class IndexerGenesisProvider : IIndexerGenesisProvider, ISingletonDependency
{
    private readonly GraphQlFactory _graphQlFactory;
    private readonly ILogger<IndexerGenesisProvider> _logger;
    private const string IndexerType = AElfIndexerConstant.GenesisIndexer;


    public IndexerGenesisProvider(GraphQlFactory graphQlFactory, ILogger<IndexerGenesisProvider> logger)
    {
        _graphQlFactory = graphQlFactory;
        _logger = logger;
    }

    public async Task<Dictionary<string, ContractInfoDto>> GetContractListAsync(string chainId,
        List<string> addressList)
    {
        var indexerContractListResultDto = new IndexerContractListResultDto();
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerContractListResultDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String,$addressList:[String!],$skipCount:Int!,$maxResultCount:Int!){
                            contractList(input: {chainId:$chainId,addressList:$addressList,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                               totalCount
                               items {
                                 address
                                contractVersion
                                version
                                author
                                codeHash
                                contractType
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
                        chainId = chainId, addressList = addressList, skipCount = 0,
                        maxResultCount = 100,
                    }
                });

            return result.ContractList.Items.ToDictionary(c => c.Address + c.Metadata.ChainId, c => c);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query ContractList failed.");
            return new Dictionary<string, ContractInfoDto>();
        }
    }

    public async Task<IndexerContractListResultDto> GetContractListAsync(string chainId,
        int skipCount,
        int maxResultCount, string orderBy = "", string sort = "", string address = "", long blockHeight = 0)
    {
        var indexerContractListResultDto = new IndexerContractListResultDto();
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerContractListResultDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String,$orderBy:String,$sort:String,$skipCount:Int!,$maxResultCount:Int!,$address:String,,$blockHeight:Long){
                            contractList(input: {chainId:$chainId,orderBy:$orderBy,sort:$sort,skipCount:$skipCount,maxResultCount:$maxResultCount,address:$address,blockHeight:$blockHeight}){
                               totalCount
                               items {
                                 address
                                contractVersion
                                version
                                author
                                codeHash
                                contractType
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
                        chainId = chainId, orderBy = orderBy, sort = sort, skipCount = skipCount,
                        maxResultCount = maxResultCount, address = address, blockHeight = blockHeight
                    }
                });
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query ContractList failed.");
            return indexerContractListResultDto;
        }
    }


    public async Task<List<ContractRecordDto>> GetContractRecordAsync(string chainId, string address, int skipCount = 0,
        int maxResultCount = 10)
    {
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerContractRecordListDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$address:String,$skipCount:Int!,$maxResultCount:Int!){
                            contractRecord(input: {chainId:$chainId,address:$address,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            operator
                            operationType
                            transactionId
                            contractInfo {
                              address
                              codeHash
                              author
                              version
                              nameHash
                              contractVersion
                              contractCategory
                              contractType
                            }
                            metadata {
                              chainId
                              block {
                                blockHash
                                blockTime
                                blockHeight
                              }
                            }
                                                        
                            }
                        }",
                    Variables = new
                    {
                        chainId = chainId, address = address, skipCount = skipCount, maxResultCount = maxResultCount
                    }
                });
            return result.ContractRecord;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Contract failed.");
            return new List<ContractRecordDto>();
        }
    }

    public async Task<List<ContractRegistrationDto>> GetContractRegistrationAsync(string chainId, string codeHash,
        int skipCount, int maxResultCount)
    {
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType)
                .QueryAsync<IndexerContractRegistrationListDto>(new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$codeHash:String!){
                            contractRegistration(input: {chainId:$chainId,codeHash:$codeHash}){
                                codeHash,
                                code  
                            }
                        }",
                    Variables = new
                    {
                        chainId = chainId, codeHash = codeHash, skipCount = skipCount, maxResultCount = maxResultCount
                    }
                });
            return result.ContractRegistration;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Contract Registration failed.");
            return new List<ContractRegistrationDto>();
        }
    }
}