using AElfScanServer.Common.Dtos;
using AElfScanServer.Grains.State.Contract;
using AutoMapper;

namespace AElfScanServer.Grains;

public class GrainsAutoMapperProfile : Profile
{
    public GrainsAutoMapperProfile()
    {
        CreateMap<ContractFileCodeState, ContractFileResultDto>().ReverseMap();
        CreateMap<SynchronizationState, SynchronizationDto>().ReverseMap();
    }
}