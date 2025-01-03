using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.Helper;
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
        string sortOrder = "desc", List<string> symbols = null, List<string> specialSymbols = null,
        SymbolType symbolType = SymbolType.Token,
        string CollectionSymbol = "")
    {
        var searchDescriptor = new SearchDescriptor<TokenInfoIndex>()
            .Index("tokeninfoindex")
            .Skip(skip)
            .Size(size)
            .Query(q => q
                .Bool(b =>
                {
                    var mustClauses = new List<Func<QueryContainerDescriptor<TokenInfoIndex>, QueryContainer>>
                    {
                        s => s.Term(t => t.Field(f => f.Type).Value(symbolType))
                    };

                    if (!symbols.IsNullOrEmpty())
                    {
                        mustClauses.Add(s => s.Terms(t => t.Field(f => f.Symbol).Terms(symbols)));
                    }

                    if (!CollectionSymbol.IsNullOrEmpty())
                    {
                        mustClauses.Add(s => s.Terms(t => t.Field(f => f.CollectionSymbol).Terms(CollectionSymbol)));
                    }

                    var shouldClauses = new List<Func<QueryContainerDescriptor<TokenInfoIndex>, QueryContainer>>();
                    if (!specialSymbols.IsNullOrEmpty())
                    {
                        shouldClauses.Add(s => s.Terms(t => t.Field(f => f.Symbol).Terms(specialSymbols)));
                    }

                    return b
                        .Should(
                            s => s.Bool(bq => bq.Must(mustClauses.ToArray())),
                            s => s.Bool(bq =>
                                bq.Should(shouldClauses.ToArray()).MinimumShouldMatch(1))
                        )
                        .MinimumShouldMatch(1);
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


    public static async Task<TokenInfoIndex> SearchTokenDetail(string symbol)
    {
        var searchDescriptor = new SearchDescriptor<TokenInfoIndex>()
            .Index("tokeninfoindex")
            .Skip(0)
            .Size(1)
            .Query(q => q
                .Bool(b =>
                {
                    var mustClauses = new List<Func<QueryContainerDescriptor<TokenInfoIndex>, QueryContainer>>
                    {
                        s => s.Term(t => t.Field(f => f.Symbol).Value(symbol))
                    };

                    return b.Must(mustClauses.ToArray());
                })
            );

        var searchResponse = await esClient.SearchAsync<TokenInfoIndex>(searchDescriptor);
        long totalCount = searchResponse.Total;

        if (!searchResponse.IsValid)
        {
            throw new Exception($"Elasticsearch query failed: {searchResponse.OriginalException.Message}");
        }

        List<TokenInfoIndex> tokenInfoList = searchResponse.Documents.ToList();

        return tokenInfoList.First();
    }


    public static async Task<(List<TokenInfoIndex> list, long totalCount)> SearchTokenInfoList(
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


    public static async Task<(List<AccountTokenIndex> list, long totalCount)> SearchAccountList(
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

        if (!input.Types.IsNullOrEmpty())
        {
            filterQueries.Add(new TermsQuery
            {
                Field = "token.type",
                Terms = input.Types.Cast<object>()
            });
        }

        filterQueries.Add(new NumericRangeQuery
        {
            Field = "formatAmount",
            GreaterThan = 0
        });

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
                : null,
            TrackTotalHits = true
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


    public static async Task<(List<AccountTokenIndex> list, long totalCount)> SearchAccountIndexList(
        TokenHolderInput input, List<string> specialSymbols = null)
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

        if (!input.Types.IsNullOrEmpty() || !specialSymbols.IsNullOrEmpty())
        {
            var typeQueries = new List<QueryContainer>();

            if (!input.Types.IsNullOrEmpty())
            {
                typeQueries.Add(new TermsQuery
                {
                    Field = "token.type",
                    Terms = input.Types.Cast<object>()
                });
            }

            if (!specialSymbols.IsNullOrEmpty())
            {
                typeQueries.Add(new TermsQuery
                {
                    Field = "token.symbol",
                    Terms = specialSymbols.Cast<object>()
                });
            }

            filterQueries.Add(new BoolQuery
            {
                Should = typeQueries,
                MinimumShouldMatch = 1
            });
        }

        filterQueries.Add(new NumericRangeQuery
        {
            Field = "formatAmount",
            GreaterThan = 0
        });

        if (!string.IsNullOrEmpty(input.Address))
        {
            filterQueries.Add(new TermQuery { Field = "address", Value = input.Address });
        }

        if (!string.IsNullOrEmpty(input.ChainId))
        {
            filterQueries.Add(new TermQuery { Field = "chainId", Value = input.ChainId });
        }

        var searchRequest = new SearchRequest("accounttokenindex")
        {
            From = (int)input.SkipCount,
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
            TrackTotalHits = true
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


    public static async Task<long> GetTokenHolders(
        string symbol, string chainId)
    {
        var countResponse = await esClient.CountAsync<AccountTokenIndex>(c => c
            .Index("accounttokenindex")
            .Query(q => q
                .Bool(b => b
                    .Must(must => must
                            .Term(t => t.Field(f => f.Token.Symbol).Value(symbol)),
                        must => !string.IsNullOrEmpty(chainId)
                            ? must.Terms(t => t.Field(f => f.ChainIds).Terms(chainId))
                            : null,
                        must => must.Range(r => r.Field(f => f.FormatAmount).GreaterThan(0))
                    )
                )
            )
        );

        return countResponse.Count;
    }

    public static async Task<long> GetTokenTypeHolders(
        string chainId, List<SymbolType> tokenTypeList)
    {
        var countResponse = await esClient.CountAsync<AccountTokenIndex>(c => c
            .Index("accounttokenindex")
            .Query(q => q
                .Bool(b => b
                    .Must(must => must
                            .Terms(t => t.Field(f => f.Token.Type)
                                .Terms(tokenTypeList)), 
                        must => !string.IsNullOrEmpty(chainId)
                            ? must.Terms(t => t.Field(f => f.ChainIds).Terms(chainId)) 
                            : null,
                        must => must.Range(r => r.Field(f => f.FormatAmount).GreaterThan(0)) // FormatAmount > 0
                    )
                )
            )
        );

        return countResponse.Count;
    }
}