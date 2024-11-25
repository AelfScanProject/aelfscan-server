using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Dtos.Indexer;

namespace AElfScanServer.Mocks.Provider;

public class MockGenesisPluginProvider : IGenesisPluginProvider
{
    public async Task<Dictionary<string, ContractInfoDto>> GetContractListAsync(string chainId, List<string> addressList)
    {
        return addressList.Select(o => new ContractInfoDto
        {
            Address = o,
            CodeHash = "CodeHash",
            Author = "Author",
            Version = 0,
            NameHash = "NameHash",
            ContractVersion = "1",
            ContractCategory = 0,
            ContractType = "ContractType",
            ChainId = chainId,
            Metadata = MockUtil.CreateDefaultMetaData()
        }).ToDictionary(o => o.Address,o =>o);
         
    }

    public async Task<bool> IsContractAddressAsync(string chainId, string address)
    {
        return false;
    }
}