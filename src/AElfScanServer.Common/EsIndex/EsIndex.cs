using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.MergeData;
using Nest;
using Volo.Abp.Caching;

namespace AElfScanServer.Common.EsIndex;

public class EsIndex
{
    public static IElasticClient esClient;
    public static IDistributedCache<string> _cache;

    public static void SetElasticClient(IElasticClient client)
    {
        esClient = client;
    }

    public static async Task<(List<TokenInfoIndex> list, long totalCount)> SearchMergeTokenList(
        int skip, int size,
        string sortOrder = "desc", List<string> symbols = null, SymbolType symbolType = SymbolType.Token)
    {
        var searchDescriptor = new SearchDescriptor<TokenInfoIndex>()
            .Index("tokeninfoindex").Skip(skip)
            .Size(size)
            .Query(q => q
                .Bool(b =>
                {
                    var shouldClauses = new List<Func<QueryContainerDescriptor<TokenInfoIndex>, QueryContainer>>
                    {
                        s => s.Term(t => t.Field(f => f.Type).Value(symbolType))
                    };

                    if (symbols != null && symbols.Any())
                    {
                        shouldClauses.Add(s => s.Terms(t => t.Field(f => f.Symbol).Terms(symbols)));
                    }

                    return b.Should(shouldClauses.ToArray()).MinimumShouldMatch(1);
                })
            )
            .Sort(so =>
            {
                if (sortOrder == "asc")
                {
                    return so.Ascending("holderCount");
                }

                return so.Descending("holderCount");
            });

        var searchResponse = await esClient.SearchAsync<TokenInfoIndex>(searchDescriptor);
        long totalCount = searchResponse.Total;
        if (!searchResponse.IsValid)
        {
            throw new Exception($"Elasticsearch query failed: {searchResponse.OriginalException.Message}");
        }

        List<TokenInfoIndex> tokenInfoList = searchResponse.Documents.ToList();

        return (tokenInfoList, totalCount);
    }
}