using System.ComponentModel.DataAnnotations;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetContractDetailInput : GetDetailBasicDto
{
    [Required] public string Address { get; set; }
}

public class GetContractDetailResultDto
{
}