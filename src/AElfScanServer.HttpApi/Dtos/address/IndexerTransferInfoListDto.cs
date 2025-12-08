using System;
using System.Collections.Generic;

namespace AElfScanServer.HttpApi.Dtos.address;

public class IndexerTransferInfoListDto
{
    public List<TransferInfoDto> TransferInfo { get; set; }
}