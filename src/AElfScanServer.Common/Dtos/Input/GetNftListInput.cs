namespace AElfScanServer.Common.Dtos.Input;

public class GetNftListInput : BaseInput
{
    public string Sorting { get; set; }
    public string Address { get; set; }
    public string Keyword { get; set; }
}