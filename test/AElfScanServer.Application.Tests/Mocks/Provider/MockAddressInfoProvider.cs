using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Address.Provider;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Token.Provider;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace AElfScanServer.Mocks.Provider;

public class MockAddressInfoProvider : IAddressInfoProvider
{
    private const string AddressAssetCacheKeyPrefix = "AddressAsset";
    private const string AddressInfoCacheKeyPrefix = "AddressInfo";
    private readonly IMemoryCache _memoryCache;
    public MockAddressInfoProvider(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task CreateAddressAssetAsync(AddressAssetType type, string chainId, AddressAssetDto addressAsset,
        List<SymbolType> symbolTypes = null)
    {
        var key = GetKey(AddressAssetCacheKeyPrefix + type, chainId, addressAsset.Address);
        var serializedValue = JsonConvert.SerializeObject(addressAsset);
        _memoryCache.Set(key, serializedValue);
        
    }

    public async Task<AddressAssetDto> GetAddressAssetAsync(AddressAssetType type, string chainId, string address, List<SymbolType> symbolTypes = null)
    {
        var key = GetKey(AddressAssetCacheKeyPrefix + type, chainId, address);
        var re = _memoryCache.Get(key);
        if (re != null) 
        {
            return JsonConvert.DeserializeObject<AddressAssetDto>(re.ToString());
        }

        return new AddressAssetDto();
    }

    public async Task<Dictionary<string, CommonAddressDto>> GetAddressInfo(string chainId, List<string> addressList)
    {
        return addressList.ToDictionary(o => o, o => new CommonAddressDto
        {
            Name = "Name",
            Address = o,
            AddressType = AddressType.EoaAddress,
            IsManager = false,
            IsProducer = false
        });
    }
    
    private string GetKey(string prefix, string chainId, string address)
    {
        return $"{prefix}-{chainId}-{address}";
    }
}