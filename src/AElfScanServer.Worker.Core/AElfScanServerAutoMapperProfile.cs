using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.HttpApi.Dtos;
using AutoMapper;

namespace AElfScanServer.Worker.Core;

public class AelfExploreServerAutoMapperProfile : Profile
{
    public AelfExploreServerAutoMapperProfile()
    {
        CreateMap<AddressIndex, CommonAddressDto>()
            .ReverseMap();
        CreateMap<BlockIndex, BlockRespDto>()
            .ReverseMap();
        CreateMap<IndexerTokenInfoDto, TokenInfoIndex>()
            .ForMember(dest => dest.ChainId, opt => opt.MapFrom(src => src.Metadata.ChainId))
            .ReverseMap();
        CreateMap<IndexerTokenHolderInfoDto, AccountTokenIndex>()
            .ForMember(dest => dest.ChainId, opt => opt.MapFrom(src => src.Metadata.ChainId))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Address + src.Token.Symbol))
            .ReverseMap();
        CreateMap<IndexerTokenBaseDto, TokenBase>()
            .ReverseMap();
    }
}