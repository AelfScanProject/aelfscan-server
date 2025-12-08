using System;

namespace AElfScanServer.HttpApi.Dtos;

public class OrganizationUnitDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreationTime { get; set; }
}