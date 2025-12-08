using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.IndexerPluginProvider;

public interface IAddressTypeProvider
{
    public Task<string> GetAddressType(string chainId, string address);
}

public class AddressTypeProvider : IAddressTypeProvider, ISingletonDependency
{
    public AddressTypeProvider()
    {
    }

    public async Task<string> GetAddressType(string chainId, string address)
    {
        return "";
    }
}