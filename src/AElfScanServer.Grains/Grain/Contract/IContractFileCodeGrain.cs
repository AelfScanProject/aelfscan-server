using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Grains.Grain.Contract;

public interface IContractFileCodeGrain: IGrainWithStringKey
{
    Task SaveAndUpdateAsync(ContractFileResultDto contractFileResultDto);

    Task<ContractFileResultDto> GetAsync();
}