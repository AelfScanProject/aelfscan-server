using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Grains.Grain.Contract;

public interface ISynchronizationContractGrain: IGrainWithStringKey
{
    Task SaveAndUpdateAsync(SynchronizationContractDto synchronizationContractDto);

    Task<SynchronizationContractDto> GetAsync();
}