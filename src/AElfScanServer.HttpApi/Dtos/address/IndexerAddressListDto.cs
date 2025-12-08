using System.Collections.Generic;

namespace AElfScanServer.HttpApi.Dtos.address;

public class IndexerAddressListDto
{
    public List<AccountInfoDto> AccountInfo { get; set; }
}