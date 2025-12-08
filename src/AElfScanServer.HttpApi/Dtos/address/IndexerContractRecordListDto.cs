using System.Collections.Generic;

namespace AElfScanServer.HttpApi.Dtos.address;

public class IndexerContractRecordListDto
{
    public List<ContractRecordDto> ContractRecord { get; set; }
}