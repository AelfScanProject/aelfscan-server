using System.Collections.Generic;
using Orleans;

namespace AElfScanServer.Common.Dtos;

[GenerateSerializer]
public class ContractFileResultDto
{
    [Id(0)] public string ChainId { get; set; }

    [Id(1)] public string Address { get; set; }
    [Id(2)] public string ContractName { get; set; }
    [Id(3)] public string ContractVersion { get; set; }

    [Id(4)] public long LastBlockHeight { get; set; }
    [Id(5)] public List<DecompilerContractFileDto> ContractSourceCode { get; set; }
}

[GenerateSerializer]
public class DecompilerContractFileDto
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Content { get; set; }
    [Id(2)] public string FileType { get; set; }
    [Id(3)] public List<DecompilerContractFileDto> Files { get; set; }
}

// [GenerateSerializer]
// public class DecompilerContractFileDto
// {
//     [Id(0)] public string Name { get; set; }
//     [Id(1)] public string Content { get; set; }
//     [Id(2)] public string FileType { get; set; }
//     [Id(3)] public List<DecompilerContractFileDto> Files { get; set; }
// }