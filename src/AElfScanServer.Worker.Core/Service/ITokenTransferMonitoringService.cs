using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Dtos;

namespace AElfScanServer.Worker.Core.Service;

public interface ITokenTransferMonitoringService
{
    /// <summary>
    /// Incrementally get transfer events based on time scanning
    /// </summary>
    Task<List<TransferEventDto>> GetTransfersAsync(string chainId);

    /// <summary>
    /// Process single transfer event and send metrics
    /// </summary>
    void ProcessTransfer(TransferEventDto transfer);

    /// <summary>
    /// Process multiple transfer events and send metrics
    /// </summary>
    void ProcessTransfers(List<TransferEventDto> transfers);

    /// <summary>
    /// Send transfer metrics to monitoring system
    /// </summary>
    void SendTransferMetrics(TransferEventDto transfer);
} 