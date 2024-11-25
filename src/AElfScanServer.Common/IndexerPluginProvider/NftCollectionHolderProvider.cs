using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
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
    private readonly IEntityMappingRepository<NftCollectionHolderInfoIndex, string> _holderInfoIndexRepository;

    private ILogger<NftCollectionHolderProvider> _logger;

    public NftCollectionHolderProvider(ILogger<NftCollectionHolderProvider> logger,
        IEntityMappingRepository<NftCollectionHolderInfoIndex, string> repository)
    {
        _logger = logger;
        _holderInfoIndexRepository = repository;
    }


    public async Task UpdateNftCollectionHolder(List<NftCollectionHolderInfoIndex> list)
    {
        //todo add cache
        await _holderInfoIndexRepository.AddOrUpdateManyAsync(list);
    }

    public async Task<List<NftCollectionHolderInfoIndex>> GetNftCollectionHolderInfoAsync(string collectionSymbol,
        string chainId)
    {
        var queryableAsync = await _holderInfoIndexRepository.GetQueryableAsync();
        var nftCollectionHolderInfoIndices = queryableAsync.Where(c => c.ChainId == chainId)
            .Where(c => c.CollectionSymbol == collectionSymbol).ToList();

        return nftCollectionHolderInfoIndices;
    }
}