using AElfScanServer.Common.Dtos;
using AElfScanServer.Grains.State.Contract;
using Orleans.Providers;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.Grains.Grain.Contract;
[StorageProvider(ProviderName= "Default")]
public class SynchronizationContractGrain : Grain<SynchronizationContractState>, ISynchronizationContractGrain
{
    private readonly IObjectMapper _objectMapper;

    public SynchronizationContractGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    public async Task SaveAndUpdateAsync(SynchronizationContractDto synchronizationContractDtot)
    {
        State = _objectMapper.Map<SynchronizationContractDto,SynchronizationContractState>(synchronizationContractDtot);

        await WriteStateAsync();
    }

    public  async Task<SynchronizationContractDto> GetAsync()
    {
        if (State == null)
        {
            return new SynchronizationContractDto();
        }

        return _objectMapper.Map<SynchronizationContractState,SynchronizationContractDto>(State);
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
}