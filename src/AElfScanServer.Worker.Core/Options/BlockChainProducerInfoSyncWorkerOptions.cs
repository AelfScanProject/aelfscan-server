namespace AElfScanServer.Worker.Core.Options;

public class BlockChainProducerInfoSyncWorkerOptions
{
    public int ExecuteInterval { get; set; } = 5;
    public string BaseUrl { get; set; }
    public string BlockChainProducersUri { get; set; } = "api/vote/getAllTeamDesc";
}