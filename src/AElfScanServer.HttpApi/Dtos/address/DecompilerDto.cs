using System.Collections.Generic;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetContractFilesResponseDto
{
    public int Code { get; set; }
    public string Msg { get; set; }
    public string Version { get; set; }
    public List<DecompilerContractFileDto> Data { get; set; }
}
