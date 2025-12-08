using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AElfScanServer.HttpApi.Dtos;

public class GetLogEventListResultDto
{
}

public class GetLogEventListRequestInput : IValidatableObject
{
    public string ChainId { get; set; }
    public string Address { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            yield return new ValidationResult("Invalid address input");
        }
    }
}