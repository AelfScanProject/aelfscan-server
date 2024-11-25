using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Provider;

namespace AElfScanServer.Mocks.Provider;

public class MockIndexerGenesisProvider: IIndexerGenesisProvider
{
    public async Task<IndexerContractListResultDto> GetContractListAsync(string chainId, int skipCount, int maxResultCount, string orderBy, string sort,
        string address, long blockHeight = 0)
    {
        return new IndexerContractListResultDto
        {
            ContractList = new IndexerContractListDto
            {
                TotalCount = 1,
                Items = new List<ContractInfoDto>
                {
                    new ContractInfoDto
                    {
                        Address = address,
                        CodeHash = "CodeHash",
                        Author = "Test",
                        Version = 0,
                        NameHash = null,
                        ContractVersion = "1.6",
                        ContractCategory = 0,
                        ContractType = null,
                        ChainId = "AELF",
                        Metadata = MockUtil.CreateDefaultMetaData()
                    }
                }
            }
        };
    }

    public async Task<Dictionary<string, ContractInfoDto>> GetContractListAsync(string chainId, List<string> addresslist)
    {
        return addresslist.Select(o => new ContractInfoDto
        {
            Address = o,
            CodeHash = "CodeHash",
            Author = "Test",
            Version = 0,
            NameHash = null,
            ContractVersion = "1.6",
            ContractCategory = 0,
            ContractType = null,
            ChainId = chainId,
            Metadata = MockUtil.CreateDefaultMetaData()
        }).ToDictionary(dto => dto.Address, default);
    }

    public async Task<List<ContractRecordDto>> GetContractRecordAsync(string chainId, string address, int skipCount = 0, int maxResultCount = 10)
    {
        return new List<ContractRecordDto>
        {
           new ContractRecordDto
           {
               Id = "Id",
               ChainId = chainId,
               BlockHash = "BlockHash",
               BlockHeight = 100,
               CodeHash = "CodeHash",
               BlockTime = default,
               OperationType = "",
               Operator = "",
               TransactionId = "TransactionId",
               Author = "",
               Address = address,
               Version = "1",
               ContractInfo = new ContractInfoDto(),
               Metadata = MockUtil.CreateDefaultMetaData(),
               ContractType = "",
               ContractOperationType = "",

           }
        };
    }

    public async Task<List<ContractRegistrationDto>> GetContractRegistrationAsync(string chainId, string codeHash, int skipCount = 0, int maxResultCount = 0)
    {
        return new List<ContractRegistrationDto>()
        {
            new ContractRegistrationDto
            {
                Id = "Id",
                ChainId = chainId,
                BlockHash = "BlockHash",
                BlockHeight = 100,
                BlockTime = default,
                ContractCategory = 0,
                Code = "TestCode"
            }
        };
    }
}