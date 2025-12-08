using System.Collections.Generic;

namespace AElfScanServer.Worker.Core.Dtos;

public class BlockChainProducerInfoDto
{
    public string PublicKey { get; set; }
    public string Address { get; set; }
    public string Name { get; set; }
    public string IsActive { get; set; }
}

public class GetBlockChainProducersInfoResponseDto
{
    public string Msg { get; set; }
    public int Code { get; set; }
    public List<BlockChainProducerInfoDto> Data { get; set; }
}