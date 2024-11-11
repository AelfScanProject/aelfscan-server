using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.GraphQL;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.Domain.Common.Entities;
using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.IndexerPluginProvider;

public interface IAwakenIndexerProvider
{
    public Task<TotalValueLockedResultDto> GetAwakenTvl(string chainId, long timeStamp);
}

public class AwakenIndexerProvider : IAwakenIndexerProvider, ISingletonDependency
{
    private readonly IGraphQlFactory _graphQlFactory;

    private ILogger<TokenIndexerProvider> _logger;

    public AwakenIndexerProvider(IGraphQlFactory graphQlFactory, ILogger<TokenIndexerProvider> logger)
    {
        _graphQlFactory = graphQlFactory;
        _logger = logger;
    }

    [ExceptionHandler(typeof(IOException), typeof(TimeoutException), typeof(Exception),
        Message = "GetAwakenTvl err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), LogTargets = ["chainId","timeStamp"])]
    public virtual async Task<TotalValueLockedResultDto> GetAwakenTvl(string chainId, long timeStamp)
    {
        var graphQlHelper = _graphQlFactory.GetGraphQlHelper(AElfIndexerConstant.AwakenIndexer);
        var result = await graphQlHelper.QueryAsync<TotalValueLockedResult>(
            new GraphQLRequest
            {
                Query =
                    @"query($chainId:String!,$timestamp:Long!){
                        totalValueLocked(dto: {chainId:$chainId,timestamp:$timestamp}){
                          value
                        }
                    }",
                Variables = new
                {
                    chainId = chainId, timestamp = timeStamp
                }
            });

        return result.TotalValueLocked;
    }
}