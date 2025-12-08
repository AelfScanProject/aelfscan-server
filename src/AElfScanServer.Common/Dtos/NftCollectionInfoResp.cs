using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos;

public class NftCollectionInfoResp
{
    public string Code { get; set; }
    public string Message { get; set; }
    public NftCollectionInfoData Data { get; set; }
}


public class NftCollectionInfoData
{
    public int TotalCount { get; set; }

    public List<NftCollectionInfoDto> Items { get; set; } = new();
}