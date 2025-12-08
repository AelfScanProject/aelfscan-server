using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Domain.Common.Entities;
using Microsoft.Extensions.Logging;
using Nest;

namespace AElfScanServer.Common.IndexerPluginProvider;

public interface INftCollectionHolderProvider
{
    public Task UpdateNftCollectionHolder(List<NftCollectionHolderInfoIndex> list);

    public Task<List<NftCollectionHolderInfoIndex>> GetNftCollectionHolderInfoAsync(string collectionSymbol,
        string chainId);
}

public class NftCollectionHolderProvider : INftCollectionHolderProvider
{
    private readonly INESTRepository<NftCollectionHolderInfoIndex, string> _repository;
    private ILogger<NftCollectionHolderProvider> _logger;

    public NftCollectionHolderProvider(ILogger<NftCollectionHolderProvider> logger,
        INESTRepository<NftCollectionHolderInfoIndex, string> repository)
    {
        _logger = logger;
        _repository = repository;
    }


    public async Task UpdateNftCollectionHolder(List<NftCollectionHolderInfoIndex> list)
    {
        //todo add cache
        await _repository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<NftCollectionHolderInfoIndex>> GetNftCollectionHolderInfoAsync(string collectionSymbol,
        string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NftCollectionHolderInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionSymbol).Value(collectionSymbol)));


        QueryContainer Filter(QueryContainerDescriptor<NftCollectionHolderInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _repository.GetListAsync(Filter);
        return result.Item2;
    }
}