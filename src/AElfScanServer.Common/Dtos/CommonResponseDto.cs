namespace AElfScanServer.Common.Dtos;

public class CommonResponseDto<T>
{
    public string Code { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
}