using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
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

                    if (!symbols.IsNullOrEmpty())
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


    public static async Task<(List<BlockIndex> list, long totalCount)> SearchMergeBlockList(
        int skip, int size)
    {
        var searchResponse = esClient.Search<BlockIndex>(s => s
            .Index("blockindex")
            .Sort(sort => sort.Descending(p => p.Timestamp))
            .From(skip)
            .Size(size)
            .TrackTotalHits(true)
        );

        long totalCount = searchResponse.Total;
        if (!searchResponse.IsValid)
        {
            throw new Exception($"Elasticsearch query failed: {searchResponse.OriginalException.Message}");
        }

        List<BlockIndex> tokenInfoList = searchResponse.Documents.ToList();

        return (tokenInfoList, totalCount);
    }


    // public static async Task<(List<AccountTokenIndex> list, long totalCount)> SearchMergeAccountList(
    //     TokenHolderInput input)
    // {
    //     var sortOrder = SortOrder.Descending;
    //
    //     if (input.OrderInfos != null && !input.OrderInfos.IsNullOrEmpty())
    //     {
    //         if (input.OrderInfos.First().Sort == "Asc")
    //         {
    //             sortOrder = SortOrder.Ascending;
    //         }
    //     }
    //
    //     var searchRequest = new SearchRequest("accounttokenindex")
    //     {
    //         Size = (int)input.MaxResultCount,
    //         Sort = new List<ISort>
    //         {
    //             new FieldSort { Field = "formatAmount", Order = sortOrder },
    //             new FieldSort { Field = "address", Order = sortOrder }
    //         },
    //         Query = new BoolQuery
    //         {
    //             Filter = new List<QueryContainer>
    //             {
    //                 new TermQuery { Field = "token.symbol", Value = input.Symbol }
    //             }
    //         },
    //         SearchAfter = input.SearchAfter != null && !input.SearchAfter.IsNullOrEmpty()
    //             ? new List<object> { input.SearchAfter[0], input.SearchAfter[1] }
    //             : null
    //     };
    //
    //     var response = esClient.Search<AccountTokenIndex>(searchRequest);
    //
    //     if (!response.IsValid)
    //     {
    //         throw new Exception($"Error occurred: {response.OriginalException.Message}");
    //     }
    //
    //     var results = response.Documents;
    //     var total = response.Total;
    //
    //     return (new List<AccountTokenIndex>(results), total);
    // }
    
    public static async Task<(List<AccountTokenIndex> list, long totalCount)> SearchMergeAccountList(
        TokenHolderInput input)
    {
        var sortOrder = SortOrder.Descending;

        if (input.OrderInfos != null && !input.OrderInfos.IsNullOrEmpty())
        {
            if (input.OrderInfos.First().Sort == "Asc")
            {
                sortOrder = SortOrder.Ascending;
            }
        }

        var filterQueries = new List<QueryContainer>();

        if (!string.IsNullOrEmpty(input.Symbol))
        {
            filterQueries.Add(new TermQuery { Field = "token.symbol", Value = input.Symbol });
        }

        if (!string.IsNullOrEmpty(input.CollectionSymbol))
        {
            filterQueries.Add(new TermQuery { Field = "token.collectionSymbol", Value = input.CollectionSymbol });
        }

        var searchRequest = new SearchRequest("accounttokenindex")
        {
            Size = (int)input.MaxResultCount,
            Sort = new List<ISort>
            {
                new FieldSort { Field = "formatAmount", Order = sortOrder },
                new FieldSort { Field = "address", Order = sortOrder }
            },
            Query = new BoolQuery
            {
                Filter = filterQueries
            },
            SearchAfter = input.SearchAfter != null && !input.SearchAfter.IsNullOrEmpty()
                ? new List<object> { input.SearchAfter[0], input.SearchAfter[1] }
                : null
        };

        var response = esClient.Search<AccountTokenIndex>(searchRequest);

        if (!response.IsValid)
        {
            throw new Exception($"Error occurred: {response.OriginalException.Message}");
        }

        var results = response.Documents;
        var total = response.Total;

        return (new List<AccountTokenIndex>(results), total);
    }

}