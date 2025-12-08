using AElfScanServer.Common.Dtos.Ads;
using AElfScanServer.Grains.State.Ads;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.Grains.Grain.Ads;

public class AdsGain : Grain<AdsState>, IAdsGrain
{
    private readonly IObjectMapper _objectMapper;

    public AdsGain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<AdsDto> UpdateAsync(AdsDto dto)
    {
        State.Records = dto.Records;
        State.CurAds = dto.CurAds;
        await WriteStateAsync();
        return dto;
    }


    public async Task<AdsDto> GetAsync()
    {
        return new AdsDto()
        {
            CurAds = State.CurAds,
            Records = State.Records
        };
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
}