using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Ads;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Dtos.MergeData;
using AElfScanServer.Common.Helper;
using AElfScanServer.Grains.State.Ads;
using AElfScanServer.HttpApi.Dtos.AdsData;
using AElfScanServer.HttpApi.Dtos.ChartData;
using AutoMapper;
using AddressIndex = AElfScanServer.HttpApi.Dtos.AddressIndex;

namespace AElfScanServer.HttpApi;

public class BlockChainAutoMapperProfile : Profile
{
    public BlockChainAutoMapperProfile()
    {
        CreateMap<AdsBannerIndex, AdsBannerResp>().ReverseMap();
        CreateMap<UpdateAdsBannerReq, AdsBannerIndex>().ReverseMap();
        CreateMap<AdsInfoDto, AdsResp>().ReverseMap();
        CreateMap<AdsIndex, AdsResp>().ReverseMap();
        CreateMap<UpdateAdsReq, AdsIndex>().ReverseMap();
        CreateMap<AdsInfoDto, AdsIndex>().ReverseMap();
        CreateMap<DailyHolderDto, DailyHolder>()
            .ReverseMap();


        CreateMap<DailyStakedIndex, DailyStaked>()
            .ReverseMap();
        CreateMap<DailySupplyGrowthIndex, DailySupplyGrowth>()
            .ReverseMap();

        CreateMap<DailyTotalContractCallIndex, DailyTotalContractCall>()
            .ReverseMap();

        CreateMap<DailyContractCallIndex, DailyTotalContractCall>()
            .ReverseMap();


        CreateMap<DailyAvgTransactionFeeIndex, DailyTransactionFee>()
            .ReverseMap();


        CreateMap<DailyActiveAddressCountIndex, DailyTransactionFee>()
            .ReverseMap();

        CreateMap<DailyActiveAddressCountIndex, DailyTransactionFee>()
            .ReverseMap();

        CreateMap<DailyActiveAddressCountIndex, DailyActiveAddressCount>()
            .ReverseMap();

        CreateMap<DailyTransactionCountIndex, DailyTransactionCount>()
            .ReverseMap();


        CreateMap<DailyUniqueAddressCountIndex, DailyUniqueAddressCount>()
            .ReverseMap();

        CreateMap<DailyAvgBlockSizeIndex, DailyAvgBlockSize>()
            .ReverseMap();


        CreateMap<DailyTransactionCountIndex, DailyTransactionCount>()
            .ReverseMap();

        CreateMap<DailyUniqueAddressCountIndex, DailyUniqueAddressCount>()
            .ReverseMap();

        CreateMap<DailyActiveAddressCountIndex, DailyActiveAddressCount>()
            .ReverseMap();


        CreateMap<DailyAvgTransactionFeeIndex, DailyAvgTransactionFee>()
            .ReverseMap();

        CreateMap<DailyBlockRewardIndex, DailyBlockReward>()
            .ReverseMap();

        CreateMap<DailyTotalBurntIndex, DailyTotalBurnt>()
            .ReverseMap();

        CreateMap<DailyDeployContractIndex, DailyDeployContract>()
            .ReverseMap();


        CreateMap<ElfPriceIndex, ElfPrice>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Close))
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.OpenTime))
            .ReverseMap();

        CreateMap<DailyBlockProduceCountIndex, DailyBlockProduceCount>()
            .ReverseMap();

        CreateMap<DailyBlockProduceDurationIndex, DailyBlockProduceDuration>()
            .ReverseMap();

        CreateMap<DailyCycleCountIndex, DailyCycleCount>()
            .ReverseMap();

        CreateMap<AddressIndex, CommonAddressDto>()
            .ReverseMap();

        CreateMap<TransactionIndex, TransactionDetailDto>();
        ;

        CreateMap<GetAddressTokenListInput, GetTokenListInput>();
        CreateMap<GetAddressTokenListInput, GetNftListInput>();
        CreateMap<GetTransferListInput, TokenTransferInput>();
        CreateMap<AccountTokenDto, GetAddressInfoResultDto>().ForMember(t => t.Balance, m => m.MapFrom(f => f.Amount));
        CreateMap<ContractInfoDto, GetAddressDetailResultDto>();
        CreateMap<ContractInfoDto, ContractRecordDto>();
        CreateMap<TransactionsResponseDto, GetTransactionListResultDto>();
        CreateMap<LogEventResponseDto, GetContractEventListResultDto>();

        CreateMap<IndexerTokenHolderInfoDto, GetAddressInfoResultDto>()
            .ForPath(t => t.Balance, m => m.MapFrom(u => u.FormatAmount))
            .ForPath(t => t.TransactionCount, m => m.MapFrom(u => u.TransferCount))
            ;
        CreateMap<GetAddressTokenListInput, TokenHolderInput>();
        CreateMap<GetAddressTokenListInput, TokenListInput>();
        CreateMap<GetTransferListInput, TokenTransferInput>();

        CreateMap<IndexerTokenHolderInfoDto, TokenInfoDto>()
            .ForPath(t => t.Token.Name, m => m.MapFrom(u => u.Token.CollectionSymbol))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(u => u.Token.Decimals))
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Token.Symbol))
            .ForPath(t => t.Quantity, m => m.MapFrom(u => u.FormatAmount))
            ;
        CreateMap<IndexerTokenHolderInfoDto, AddressNftInfoDto>()
            .ForMember(t => t.Token, m => m.Ignore())
            .ForPath(t => t.Quantity, m => m.MapFrom(u => u.FormatAmount))
            ;
        CreateMap<IndexerTransferInfoDto, TokenTransferInfoDto>()
            .ForMember(t => t.ChainId, m => m.MapFrom(u => BaseConverter.OfChainId(u.Metadata)))
            .ForMember(t => t.TransactionId, m => m.MapFrom(u => u.TransactionId))
            .ForMember(t => t.Method, m => m.MapFrom(u => u.Method))
            .ForMember(t => t.BlockHeight, m => m.MapFrom(u => BaseConverter.OfBlockHeight(u.Metadata)))
            .ForMember(t => t.BlockTime, m => m.MapFrom(u => BaseConverter.OfBlockTime(u.Metadata)))
            .ForMember(t => t.Quantity, m => m.MapFrom(u => u.FormatAmount))
            .ForMember(t => t.Status, m => m.MapFrom(u => TokenInfoHelper.OfTransactionStatus(u.Status)))
            .ForMember(t => t.From, m => m.Ignore())
            .ForMember(t => t.To, m => m.Ignore())
            .ForPath(t => t.From.Address, m => m.MapFrom(u => u.From))
            .ForPath(t => t.To.Address, m => m.MapFrom(u => u.To))
            .ReverseMap()
            ;


        CreateMap<IndexerTokenInfoDto, TokenCommonDto>()
            .ForPath(t => t.Token.Name, m => m.MapFrom(u => u.TokenName))
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Symbol))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(u => u.Decimals))
            .ForMember(t => t.CirculatingSupply, m => m.MapFrom(u => u.Supply))
            .ForPath(t => t.Holders, m => m.MapFrom(u => u.HolderCount))
            .ReverseMap()
            ;


        CreateMap<TokenInfoIndex, TokenCommonDto>()
            .ForPath(t => t.Token.Name, m => m.MapFrom(u => u.TokenName))
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Symbol))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(u => u.Decimals))
            .ForMember(t => t.CirculatingSupply, m => m.MapFrom(u => u.Supply))
            .ForPath(t => t.Holders, m => m.MapFrom(u => u.HolderCount))
            .ReverseMap()
            ;
        CreateMap<IndexerTokenHolderInfoDto, TokenHolderInfoDto>()
            .ForPath(t => t.Quantity, m => m.MapFrom(u => u.FormatAmount))
            .ForMember(t => t.Address, m => m.Ignore())
            .ForPath(t => t.Address.Address, m => m.MapFrom(u => u.Address))
            ;
        CreateMap<IndexerTokenInfoDto, NftInfoDto>()
            .ForPath(t => t.NftCollection.Name, m => m.MapFrom(u => u.TokenName))
            .ForPath(t => t.NftCollection.Symbol, m => m.MapFrom(u => u.Symbol))
            .ForMember(t => t.Holders, m => m.MapFrom(u => u.HolderCount))
            .ReverseMap()
            ;
        CreateMap<IndexerTokenInfoDto, NftDetailDto>()
            .ForPath(t => t.NftCollection.Name, m => m.MapFrom(u => u.TokenName))
            .ForPath(t => t.NftCollection.Symbol, m => m.MapFrom(u => u.Symbol))
            .ForMember(t => t.Holders, m => m.MapFrom(u => u.HolderCount))
            .ReverseMap()
            ;
        CreateMap<IndexerTransferInfoDto, NftTransferInfoDto>()
            .ForPath(t => t.BlockTime, m => m.MapFrom(u => BaseConverter.OfBlockTime(u.Metadata)))
            .ForPath(t => t.BlockHeight, m => m.MapFrom(u => BaseConverter.OfBlockHeight(u.Metadata)))
            .ForPath(t => t.Item.Name, m => m.MapFrom(u => u.Token.CollectionSymbol))
            .ForPath(t => t.Item.Symbol, m => m.MapFrom(u => u.Token.Symbol))
            .ForMember(t => t.TransactionId, m => m.MapFrom(u => u.TransactionId))
            .ForMember(t => t.Value, m => m.MapFrom(u => u.FormatAmount))
            .ForMember(t => t.Status, m => m.MapFrom(u => TokenInfoHelper.OfTransactionStatus(u.Status)))
            .ForMember(t => t.From, m => m.Ignore())
            .ForMember(t => t.To, m => m.Ignore())
            .ReverseMap()
            ;
        CreateMap<IndexerTokenInfoDto, NftInventoryDto>()
            .ForPath(t => t.Item.Name, m => m.MapFrom(u => u.TokenName))
            .ForPath(t => t.Item.Decimals, m => m.MapFrom(u => u.Decimals))
            .ForPath(t => t.Item.Symbol, m => m.MapFrom(u => u.Symbol))
            ;
        CreateMap<GetTransferInfoListInput, TokenTransferInput>()
            .ForPath(t => t.Types,
                m => m.MapFrom(u =>
                    u.IsNft
                        ? new List<SymbolType>() { SymbolType.Nft_Collection, SymbolType.Nft }
                        : new List<SymbolType>() { SymbolType.Token }))
            .ReverseMap()
            ;
        CreateMap<IndexerTransferInfoDto, AddressTokenTransferInfoDto>()
            .ForMember(t => t.Amount, m => m.MapFrom(u => u.FormatAmount))
            .ForPath(t => t.Type, m => m.MapFrom(u => u.Token.Type))
            .ForMember(t => t.TransactionHash, m => m.MapFrom(u => u.TransactionId))
            .ForPath(t => t.Timestamp, m => m.MapFrom(u => BaseConverter.OfBlockTime(u.Metadata)))
            .ForMember(t => t.From, m => m.Ignore())
            .ForMember(t => t.To, m => m.Ignore())
            .ForPath(t => t.From.Address, m => m.MapFrom(u => u.From))
            .ForPath(t => t.To.Address, m => m.MapFrom(u => u.To))
            .ReverseMap()
            ;
        CreateMap<GetTokenListInput, TokenHolderInput>()
            .ForMember(t => t.PartialSymbol, m => m.MapFrom(u => u.Keyword))
            .ForMember(t => t.Types, m => m.MapFrom(u => new List<SymbolType> { SymbolType.Token }))
            .ReverseMap()
            ;
        CreateMap<GetNftListInput, TokenHolderInput>()
            .ForMember(t => t.PartialSymbol, m => m.MapFrom(u => u.Keyword))
            .ForMember(t => t.Types, m => m.MapFrom(u => new List<SymbolType> { SymbolType.Nft }))
            .ReverseMap()
            ;
        CreateMap<IndexerTokenHolderInfoDto, TokenInfoDto>()
            .ForMember(t => t.Quantity, m => m.MapFrom(u => u.FormatAmount))
            .ForMember(t => t.Token, m => m.Ignore())
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Token.Symbol))
            .ReverseMap()
            ;
        CreateMap<IndexerTransferInfoDto, TokenTransferInfoDto>()
            .ForMember(t => t.ChainId, m => m.MapFrom(u => BaseConverter.OfChainId(u.Metadata)))
            .ForMember(t => t.TransactionId, m => m.MapFrom(u => u.TransactionId))
            .ForMember(t => t.Method, m => m.MapFrom(u => u.Method))
            .ForMember(t => t.BlockHeight, m => m.MapFrom(u => BaseConverter.OfBlockHeight(u.Metadata)))
            .ForMember(t => t.BlockTime, m => m.MapFrom(u => BaseConverter.OfBlockTime(u.Metadata)))
            .ForMember(t => t.Quantity, m => m.MapFrom(u => u.FormatAmount))
            .ForMember(t => t.Status, m => m.MapFrom(u => TokenInfoHelper.OfTransactionStatus(u.Status)))
            .ForMember(t => t.From, m => m.Ignore())
            .ForMember(t => t.To, m => m.Ignore())
            .ForPath(t => t.From.Address, m => m.MapFrom(u => u.From))
            .ForPath(t => t.To.Address, m => m.MapFrom(u => u.To))
            .ReverseMap()
            ;
        CreateMap<IndexerTokenInfoDto, NftItemDetailDto>()
            .ForPath(t => t.Item.Name, m => m.MapFrom(u => u.TokenName))
            .ForPath(t => t.Item.Symbol, m => m.MapFrom(u => u.Symbol))
            .ForPath(t => t.Item.Decimals, m => m.MapFrom(u => u.Decimals))
            .ForMember(t => t.Holders, m => m.MapFrom(u => u.HolderCount))
            .ForMember(t => t.Owner,
                m => m.MapFrom(u =>
                    string.IsNullOrEmpty(u.Owner) ? new List<string>() : new List<string> { u.Owner }))
            .ForMember(t => t.Issuer,
                m => m.MapFrom(u =>
                    string.IsNullOrEmpty(u.Owner) ? new List<string>() : new List<string> { u.Issuer }))
            .ForMember(t => t.TokenSymbol, m => m.MapFrom(u => u.Symbol))
            .ForPath(t => t.SymbolToCreate,
                m => m.MapFrom(u => BaseConverter.OfExternalInfoKeyValue(u.ExternalInfo, "__seed_owned_symbol")))
            .ForPath(t => t.ExpireTime,
                m => m.MapFrom(u => BaseConverter.OfExternalInfoKeyValue(u.ExternalInfo, "__seed_exp_time")))
            .ForPath(t => t.Description,
                m => m.MapFrom(u => BaseConverter.OfExternalInfoKeyValue(u.ExternalInfo, "Description")))
            .ReverseMap()
            ;
        CreateMap<NftItemHolderInfoInput, TokenHolderInput>();
        CreateMap<TokenCommonDto, TokenDetailDto>();
        CreateMap<NftItemActivityInput, GetActivitiesInput>();
        CreateMap<NftActivityItem, NftItemActivityDto>()
            .ForMember(t => t.Action, m => m.MapFrom(u => u.Type.ToString()))
            .ForMember(t => t.Quantity, m => m.MapFrom(u => u.Amount))
            .ForPath(t => t.PriceSymbol, m => m.MapFrom(u => BaseConverter.OfSymbol(u.PriceTokenInfo)))
            .ForPath(t => t.BlockTime, m => m.MapFrom(u => u.Timestamp))
            .ForPath(t => t.TransactionId, m => m.MapFrom(u => u.TransactionHash))
            .ForMember(t => t.From, m => m.Ignore())
            .ForMember(t => t.To, m => m.Ignore())
            ;
        CreateMap<NftInventoryInput, TokenListInput>();
        CreateMap<NftInventoryInput, TokenHolderInput>();
        CreateMap<TokenTransferInfoDto, NftTransferInfoDto>()
            .ForMember(t => t.Value, m => m.MapFrom(u => u.Quantity))
            .ForPath(t => t.Item.Name, m => m.MapFrom(u => u.SymbolName))
            .ForPath(t => t.Item.Symbol, m => m.MapFrom(u => u.Symbol))
            .ForPath(t => t.Item.ImageUrl, m => m.MapFrom(u => u.SymbolImageUrl))
            ;
    }
}