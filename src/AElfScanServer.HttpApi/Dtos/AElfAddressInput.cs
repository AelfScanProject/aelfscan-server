using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.HttpApi.Dtos;

public class AElfAddressInput
{
    public string Name { get; set; }
    public List<string> Addresses { get; set; }
    public AddressType AddressType { get; set; }
    public bool IsManager { get; set; }
    public bool IsProducer { get; set; }

    [Required] public string ChainId { get; set; }
}