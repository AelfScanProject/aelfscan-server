using System.Collections.Generic;

namespace AElfScanServer.HttpApi.Dtos.address;

public class IndexerContractRegistrationListDto
{
    public List<ContractRegistrationDto> ContractRegistration { get; set; }
}