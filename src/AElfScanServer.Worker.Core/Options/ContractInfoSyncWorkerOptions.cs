namespace AElfScanServer.Worker.Core.Options;

public class ContractInfoSyncWorkerOptions
{
    public int ExecuteInterval { get; set; } = 3;
    public string ContractInfosUri { get; set; } = "api/viewer/list";
}