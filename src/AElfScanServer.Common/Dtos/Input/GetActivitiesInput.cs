using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AElfScanServer.Common.Dtos.Input;

public class GetActivitiesInput : BaseInput
{
    [Required]public string NftInfoId { get; set; }
    public List<int> Types { get; set; }
    public long TimestampMin { get; set; }
    public long TimestampMax { get; set; }
}