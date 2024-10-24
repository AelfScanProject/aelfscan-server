using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.GraphQL;
using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.Contract.Provider;

public interface IGenesisPluginProvider
{
    Task<Dictionary<string, ContractInfoDto>> GetContractListAsync(string chainId, List<string> addressList);


    Task<bool> IsContractAddressAsync(string chainId, string address);
}

public class GenesisPluginProvider : IGenesisPluginProvider, ISingletonDependency
{
    private readonly GraphQlFactory _graphQlFactory;
    private readonly ILogger<GenesisPluginProvider> _logger;
    private const string IndexerType = AElfIndexerConstant.GenesisIndexer;
    private readonly IDistributedCache<string> _contractAddressCache;


    public GenesisPluginProvider(GraphQlFactory graphQlFactory, ILogger<GenesisPluginProvider> logger,
        IDistributedCache<string> contractAddressCache)
    {
        _graphQlFactory = graphQlFactory;
        _logger = logger;
        _contractAddressCache = contractAddressCache;
    }

    public async Task<bool> IsContractAddressAsync(string chainId, string address)
    {
        try
        {
            var addr = await _contractAddressCache.GetAsync(chainId + address);
            if (!addr.IsNullOrEmpty())
            {
                return true;
            }

            var result = await GetContractAddressAsync(chainId, address);

            if (result != null && result.ContractList != null && !result.ContractList.Items.IsNullOrEmpty())
            {
                await _contractAddressCache.SetAsync(chainId + address, "1", null);
                return true;
            }


            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Determine whether it is a contract address failed.address:{address}", address);
            return false;
        }
    }

    public async Task<Dictionary<string, ContractInfoDto>> GetContractListAsync(string chainId,
        List<string> addressList)
    {
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
                        chainId = chainId,
                        skipCount = 0, maxResultCount = addressList.Count * 2, addressList = addressList
                    }
                });
           
                return result.ContractList.Items.ToDictionary(s => s.Address + s.Metadata.ChainId, s => s);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Contract failed.");
            return new Dictionary<string, ContractInfoDto>();
        }
    }

    public async Task<IndexerContractListResultDto> GetContractAddressAsync(string chainId, string address)
    {
        var indexerContractListResultDto = new IndexerContractListResultDto()
        {
            ContractList = new IndexerContractListDto()
            {
                Items = new List<ContractInfoDto>()
            }
        };
        try
        {
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerContractListResultDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!,$orderBy:String,$sort:String,$skipCount:Int!,$maxResultCount:Int!,$address:String){
                            contractList(input: {chainId:$chainId,orderBy:$orderBy,sort:$sort,skipCount:$skipCount,maxResultCount:$maxResultCount,address:$address}){
                               totalCount
                               items {
                                 address
                                contractVersion
                                version
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
                        chainId = chainId, orderBy = "", sort = "", skipCount = 0,
                        maxResultCount = 1, address = address
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
}