using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;

namespace AElfScanServer.Common.GraphQL;

public interface IGraphQlHelper
{
    Task<T> QueryAsync<T>(GraphQLRequest request);
}

public class GraphQlHelper : IGraphQlHelper
{
    private readonly IGraphQLClient _client;

    public GraphQlHelper(IGraphQLClient client)
    {
        _client = client;
    }

    public async Task<T> QueryAsync<T>(GraphQLRequest request)
    {
        var graphQlResponse = await _client.SendQueryAsync<T>(request);
        return graphQlResponse.Errors is not { Length: > 0 } ? graphQlResponse.Data : default;
    }
}

public class GraphQlResponseException : Exception
{
    public GraphQlResponseException(string message) : base(message)
    {
    }
}