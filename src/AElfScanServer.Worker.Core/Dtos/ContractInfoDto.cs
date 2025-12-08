using System.Collections.Generic;

namespace AElfScanServer.Worker.Core.Dtos;

public class GetContractsInfoResponseDto
{
    public string Msg { get; set; }
    public int Code { get; set; }
    public GetContractsInfoResponseDataDto Data { get; set; }
}

public class GetContractsInfoResponseDataDto
{
    public int Total { get; set; }
    public List<ContractInfoDto> List { get; set; }
}

public class ContractInfoDto
{
    public string ContractName { get; set; }
    public string Address { get; set; }
    public string Author { get; set; }
    public string Category { get; set; }
    public bool IsSystemContract { get; set; }
    public string Serial { get; set; }
    public string Version { get; set; }
    public string UpdateTime { get; set; }
}