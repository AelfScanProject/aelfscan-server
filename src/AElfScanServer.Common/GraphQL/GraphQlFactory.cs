using System;
using System.Collections.Concurrent;
using AElfScanServer.Common.Options;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.GraphQL;

public interface IGraphQlFactory
{
    public IGraphQlHelper GetGraphQlHelper(string indexerName);
}

public class GraphQlFactory : IGraphQlFactory, ISingletonDependency
{
    private readonly IndexerOptions _options;
    private readonly ConcurrentDictionary<string, GraphQlHelper> _graphQlHelperDic = new();

    public GraphQlFactory(IOptionsSnapshot<IndexerOptions> options)
    {
        _options = options.Value;
    }


    public IGraphQlHelper GetGraphQlHelper(string indexerName)
    {
        if (_graphQlHelperDic.TryGetValue(indexerName, out var graphQlHelper))
        {
            return graphQlHelper;
        }

        if (!_options.IndexerInfos.TryGetValue(indexerName, out var indexerInfo))
        {
            throw new ArgumentException("Indexer {indexerName} not exists", indexerName);
        }


        var graphQlHttpClient = new GraphQLHttpClient(indexerInfo.BaseUrl, new NewtonsoftJsonSerializer());
        graphQlHelper = new GraphQlHelper(graphQlHttpClient);
        _graphQlHelperDic[indexerName] = graphQlHelper;
        return graphQlHelper;
    }
}