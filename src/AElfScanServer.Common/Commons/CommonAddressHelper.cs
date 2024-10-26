using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;

namespace AElfScanServer.Common.Commons;

public class CommonAddressHelper
{
    public static CommonAddressDto GetCommonAddress(string address, string chainId,
        Dictionary<string, ContractInfoDto> contractInfoDict,
        Dictionary<string, Dictionary<string, string>> contractNameDic)
    {
        var addressDto = new CommonAddressDto()
        {
            Address = address
        };


        if (contractInfoDict.TryGetValue(address + chainId, out var contractInfo))
        {
            addressDto.AddressType = AddressType.ContractAddress;
            if (contractNameDic.TryGetValue(chainId, out var contractNames))
            {
                if (contractNames.TryGetValue(address, out var contractName))
                {
                    addressDto.Name = contractName;
                }
            }
        }


        return addressDto;
    }
}