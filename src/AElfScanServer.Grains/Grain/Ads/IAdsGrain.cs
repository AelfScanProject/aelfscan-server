using AElfScanServer.Common.Dtos.Ads;

namespace AElfScanServer.Grains.Grain.Ads;

public interface IAdsGrain : IGrainWithStringKey
{
    Task<AdsDto> UpdateAsync(AdsDto dto);

    Task<AdsDto> GetAsync();
}