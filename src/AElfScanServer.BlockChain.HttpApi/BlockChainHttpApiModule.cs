using AElf.EntityMapping.Elasticsearch;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.BlockChain.HttpApi.DataStrategy;
using AElfScanServer.BlockChain.HttpApi.Service;
using AElfScanServer.Token;
using AElfScanServer.TokenDataFunction.Provider;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AElfScanServer.BlockChain.HttpApi;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AElfIndexingElasticsearchModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(AElfScanServerBlockChainModule),
    typeof(AElfEntityMappingElasticsearchModule)
)]
public class BlockChainHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<BlockChainHttpApiModule>(); });
        context.Services.AddSingleton<ITokenIndexerProvider, TokenIndexerProvider>();
        context.Services.AddSingleton<IHomePageService, HomePageService>();
        context.Services.AddSingleton<IBlockChainService, BlockChainService>();
        context.Services.AddSingleton<IAddressService, AddressService>();
        context.Services.AddSingleton<INftInfoProvider, NftInfoProvider>();
        context.Services.AddSingleton<ITokenPriceService, TokenPriceService>();
        context.Services.AddSingleton<ISearchService, SearchService>();
        context.Services.AddSingleton<OverviewDataStrategy, OverviewDataStrategy>();
        context.Services.AddSingleton<LatestTransactionDataStrategy, LatestTransactionDataStrategy>();
        context.Services.AddSingleton<LatestBlocksDataStrategy, LatestBlocksDataStrategy>();
        context.Services.AddSignalR();
    }
}