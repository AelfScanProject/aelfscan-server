using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetContractEventReq : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string ContractAddress { get; set; }
    public long BlockHeight { get; set; }
}

public class GetContractEventResp
{
    public List<Common.Dtos.LogEventIndex> List { get; set; }
    public int Total { get; set; }
}