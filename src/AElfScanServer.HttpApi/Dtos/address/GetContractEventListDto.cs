using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetContractEventContractsInput : GetListInputBasicDto
{
    [Required] public string Address { get; set; }
}

public class GetContractEventListResultDto
{
    public long Total { get; set; }
    public List<LogEventIndex> LogEvents { get; set; }
}