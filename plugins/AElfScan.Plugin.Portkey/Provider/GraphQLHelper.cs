using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.GraphQL;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Options;
using Volo.Abp.DependencyInjection;

namespace Portkey.Provider;

public interface IGraphQLHelper
{
    Task<T> QueryAsync<T>(GraphQLRequest request);
}

public class GraphQLHelper : IGraphQLHelper, ISingletonDependency
{
    private readonly IGraphQLClient _client;
    private readonly ILogger<GraphQLHelper> _logger;
    private readonly IOptions<PluginUrlOptions> _options;

    public GraphQLHelper(IGraphQLClient client, ILogger<GraphQLHelper> logger, IOptions<PluginUrlOptions> _options)
    {
        _logger = logger;
        _client =
            new GraphQLHttpClient(
                _options.Value.Url,
                new NewtonsoftJsonSerializer());
    }

    public async Task<T> QueryAsync<T>(GraphQLRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await SendQueryAsync<T>(request);
        stopwatch.Stop();

        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = $"{nameof(QueryAsync)}:{typeof(T).Name}";
        return response;
    }

    private async Task<T> SendQueryAsync<T>(GraphQLRequest request)
    {
        var graphQlResponse = await _client.SendQueryAsync<T>(request);
        if (graphQlResponse.Errors is not { Length: > 0 })
        {
            return graphQlResponse.Data;
        }

        _logger.LogError("query graphQL err, errors = {Errors}",
            string.Join(",", graphQlResponse.Errors.Select(e => e.Message).ToList()));
        return default;
    }
}

public class GraphQLResponseException : Exception
{
    public GraphQLResponseException(string message) : base(message)
    {
    }
}