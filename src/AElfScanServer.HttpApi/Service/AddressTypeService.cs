using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Common.IndexerPluginProvider;
using Microsoft.IdentityModel.Tokens;

namespace AElfScanServer.HttpApi.Service;

public interface IAddressTypeService
{
    Task<List<string>> GetAddressTypeList(string chainId, string address);
}

public class AddressTypeService : IAddressTypeService
{
    private readonly IAddressTypeProvider _addressTypeProvider;
    private readonly IEnumerable<IAddressTypeProvider> _implementations;

    public AddressTypeService(IAddressTypeProvider addressTypeProvider,
        IEnumerable<IAddressTypeProvider> implementations)
    {
        _addressTypeProvider = addressTypeProvider;
        _implementations = implementations;
    }

    public async Task<List<string>> GetAddressTypeList(string chainId, string address)
    {
        var list = new List<string>();
        foreach (var implementation in _implementations)
        {
            var t = await implementation.GetAddressType(chainId, address);
            if (!t.IsNullOrEmpty())
            {
                list.Add(t);
            }
        }

        return list;
    }
}