using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.HttpApi.Service;

public interface IAddressService
{
    Task<Dictionary<string, CommonAddressDto>> GetAddressDictionaryAsync(AElfAddressInput input);
}

[Ump]
public class AddressService : IAddressService, ISingletonDependency
{
    private readonly INESTRepository<AddressIndex, string> _repository;
    private readonly IObjectMapper _objectMapper;

    public AddressService(INESTRepository<AddressIndex, string> repository, IObjectMapper objectMapper)
    {
        _repository = repository;
        _objectMapper = objectMapper;
    }

    public async Task<Dictionary<string, CommonAddressDto>> GetAddressDictionaryAsync(AElfAddressInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AddressIndex>, QueryContainer>>();


        if (!input.Addresses.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Address).Terms(input.Addresses)));
        }

        if (input.IsManager)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsManager).Value(input.IsManager)));
        }

        if (input.IsProducer)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsProducer).Value(input.IsProducer)));
        }

        if (!input.Name.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Name).Value(input.Name)));
        }

        QueryContainer Filter(QueryContainerDescriptor<AddressIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await _repository.GetListAsync(Filter, skip: 0, limit: 2000,
            index: BlockChainIndexNameHelper.GenerateAddressIndexName(input.ChainId));
        return result.Item2.ToDictionary(k => k.Address, ConvertDto);
    }

    private CommonAddressDto ConvertDto(AddressIndex addressIndex) =>
        _objectMapper.Map<AddressIndex, CommonAddressDto>(addressIndex);
}