using System.Collections.Generic;
using System.Linq;

namespace AElfScanServer.Worker.Core.Options;

public class WorkerOptions
{
    public List<ChainOptionDto> Chains { get; set; }
    
    public Dictionary<string, Worker> Workers { get; set; } = new();
    
    public long TransactionStartBlockHeight { get; set; }

    public bool TransactionStartBlockHeightSwitch { get; set; }

    public bool ClearTransactionDataSwitch { get; set; }

    public List<string> EsUrl { get; set; }

    public List<string> PullDataChainIds { get; set; }

    public List<string> GetChainIds()
    {
        return Chains.Select(i => i.ChainId).ToList();
    }
    
    public int GetWorkerPeriodMinutes(string workerName)
    {
        return Workers.TryGetValue(workerName, out var worker) ? worker.Minutes : Worker.DefaultMinutes;
    }
}

public class ChainOptionDto
{
    public string ChainId { get; set; }
    public string BasicInfoUrl { get; set; }
}

public class Worker
{
    public const int DefaultMinutes = 24 * 60;

    public int Minutes { get; set; } = DefaultMinutes;
}