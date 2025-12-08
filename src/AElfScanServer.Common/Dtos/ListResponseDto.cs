using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos;

public class ListResponseDto<T>
{
    public long Total { get; set; }
    public List<T> List { get; set; } = new();
}