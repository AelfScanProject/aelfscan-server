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

    public static async Task<(List<TokenInfoIndex> list, long totalCount)> SearchMergeTokenList(List<string> symbols,
        int skip, int size,
        string sortOrder = "desc")
    {
        var searchDescriptor = new SearchDescriptor<TokenInfoIndex>()
            .Index("tokeninfoindex").Skip(skip)
            .Size(size)
            .Query(q => q
                .Bool(b => b
                    .Should(
                        s => s.Term(t => t.Field(f => f.Type).Value(SymbolType.Token)),
                        s => s.Terms(t => t.Field(f => f.Symbol).Terms(symbols))
                    )
                    .MinimumShouldMatch(1)
                )
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

    public static async Task<List<TransactionIndex>> GetTransactionIndexList(string chainId, long startTime,
        long endTime)
    {
        var searchResponse = esClient.Search<TransactionIndex>(s => s // 替换为你的索引名称
            .Index("transactionindex") // 替换为你的索引名称
            .Size(20000) // 限制结果数量为20000
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        .Range(r => r
                            .Field(t => t.BlockTime)
                            .GreaterThan(startTime)
                            .LessThan(endTime)
                        )
                    )
                    .Must(m => m
                        .Match(mm => mm
                            .Field(t => t.ChainId)
                            .Query(chainId)
                        )
                    )
                )
            )
        );


        var transactionIndices = searchResponse.Documents.ToList();

        return transactionIndices;
    }


    public static async Task<List<TransactionIndex>> GetTransactionIndexList(string chainId, string dateStr)
    {
        var searchResponse = await esClient.SearchAsync<TransactionIndex>(s => s
            .Index("transactionindex")
            .Size(200000)
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .Term(mm => mm
                            .Field(t => t.ChainId)
                            .Value(chainId)
                        )
                    )
                    .Must(m => m
                        .Term(terms => terms
                            .Field(t => t.DateStr)
                            .Value(dateStr)
                        )
                    )
                )
            )
        );


        var transactionIndices = searchResponse.Documents.ToList();

        return transactionIndices;
    }
}