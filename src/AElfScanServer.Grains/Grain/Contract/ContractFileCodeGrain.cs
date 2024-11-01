using AElfScanServer.Common.Dtos;
using AElfScanServer.Grains.State.Contract;
using Orleans.Providers;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.Grains.Grain.Contract;
[StorageProvider(ProviderName= "Default")]
public class ContractFileCodeGrain : Grain<ContractFileCodeState>, IContractFileCodeGrain
{
    private readonly IObjectMapper _objectMapper;

    public ContractFileCodeGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    public async Task SaveAndUpdateAsync(ContractFileResultDto contractFileResultDto)
    {
        State = _objectMapper.Map<ContractFileResultDto,ContractFileCodeState>(contractFileResultDto);
        await WriteStateAsync();
        var bizId = GrainIdHelper.GenerateSynchronizationKey(contractFileResultDto.ChainId,
            SynchronizationType.ContractFile.ToString());
        await GrainFactory
            .GetGrain<ISynchronizationGrain>(bizId).SaveAndUpdateAsync(
                new SynchronizationDto()
                {
                  ChainId  = contractFileResultDto.ChainId,
                  BizType = SynchronizationType.ContractFile.ToString(),
                  LastBlockHeight = contractFileResultDto.LastBlockHeight
                });
    }

    public async Task<ContractFileResultDto> GetAsync()
    {
        await ReadStateAsync();
        if (State == null)
        {
            return new ContractFileResultDto();
        }

        return _objectMapper.Map<ContractFileCodeState ,ContractFileResultDto>(State);

    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
}