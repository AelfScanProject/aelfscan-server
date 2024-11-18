using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.Address.Provider;

public interface IAddressInfoProvider
{
    Task CreateAddressAssetAsync(AddressAssetType type, string chainId, AddressAssetDto addressAsset);

    Task<AddressAssetDto> GetAddressAssetAsync(AddressAssetType type, string chainId, string address,
        List<SymbolType> symbolTypes = null);

    Task<Dictionary<string, CommonAddressDto>> GetAddressInfo(string chainId, List<string> addressList);
}

public class AddressInfoProvider : RedisCacheExtension, IAddressInfoProvider, ISingletonDependency
{
    private const string AddressAssetCacheKeyPrefix = "AddressAsset";
    private const string AddressInfoCacheKeyPrefix = "AddressInfo";

    private readonly IOptionsMonitor<AddressAssetOptions> _addressAssetOptions;

    public AddressInfoProvider(IOptions<RedisCacheOptions> optionsAccessor,
        IOptionsMonitor<AddressAssetOptions> addressAssetOptions) : base(optionsAccessor)
    {
        _addressAssetOptions = addressAssetOptions;
    }

    public async Task CreateAddressAssetAsync(AddressAssetType type, string chainId, AddressAssetDto addressAsset)
    {
        await ConnectAsync();

        var key = GetKey(AddressAssetCacheKeyPrefix + type, chainId, addressAsset.Address);

        await SetObjectAsync(key, addressAsset,
            TimeSpan.FromSeconds(_addressAssetOptions.CurrentValue.GetExpireSeconds(type)));
    }

    public async Task<AddressAssetDto> GetAddressAssetAsync(AddressAssetType type, string chainId, string address,
        List<SymbolType> symbolTypes)
    {
        await ConnectAsync();

        var key = "";
        if (symbolTypes.IsNullOrEmpty())
        {
            key = GetKey(AddressAssetCacheKeyPrefix + type, chainId, address);
        }
        else
        {
            string symbols = string.Join(",", symbolTypes);
            key = GetKey(AddressAssetCacheKeyPrefix + type, chainId, address, symbols);
        }


        return await GetObjectAsync<AddressAssetDto>(key);
    }

    public async Task<Dictionary<string, CommonAddressDto>> GetAddressInfo(string chainId, List<string> addressList)
    {
        await ConnectAsync();
        var batch = RedisDatabase.CreateBatch();
        var tasks = addressList
            .Select(address => batch.StringGetAsync(GetKey(AddressInfoCacheKeyPrefix, chainId, address))).ToArray();
        batch.Execute();
        await Task.WhenAll(tasks);

        var addressInfos = new List<CommonAddressDto>();

        foreach (var task in tasks)
        {
            var json = task.Result;
            if (!json.IsNullOrEmpty)
            {
                var addressInfo = JsonConvert.DeserializeObject<CommonAddressDto>(json);
                addressInfos.Add(addressInfo);
            }
        }

        return addressInfos.ToDictionary(i => i.Address, i => i);
    }

    private string GetKey(params string[] parts)
    {
        return string.Join("-", parts);
    }
}