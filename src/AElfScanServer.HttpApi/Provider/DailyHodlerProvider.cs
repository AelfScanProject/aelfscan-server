using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.GraphQL;
using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public interface IDailyHolderProvider
{
    Task<IndexerDailyHolderDto> GetDailyHolderListAsync(string chainId);
}

public class DailyHolderProvider : IDailyHolderProvider, ISingletonDependency
{
    private readonly GraphQlFactory _graphQlFactory;
    private readonly ILogger<DailyHolderProvider> _logger;
    private const string IndexerType = AElfIndexerConstant.DailyHolderIndexer;


    public DailyHolderProvider(GraphQlFactory graphQlFactory, ILogger<DailyHolderProvider> logger)
    {
        _graphQlFactory = graphQlFactory;
        _logger = logger;
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetDailyHolderListAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["chainId"])]
    public virtual async Task<IndexerDailyHolderDto> GetDailyHolderListAsync(string chainId)
    {
        var indexerDailyHolderDto = new IndexerDailyHolderDto();
     
            var result = await _graphQlFactory.GetGraphQlHelper(IndexerType).QueryAsync<IndexerDailyHolderDto>(
                new GraphQLRequest
                {
                    Query =
                        @"query($chainId:String!){
                            dailyHolder(input: {chainId:$chainId}){
                               dateStr
                               count
                            }
                        }",
                    Variables = new
                    {
                        chainId = chainId
                    }
                });
            return result;
    
    }
}