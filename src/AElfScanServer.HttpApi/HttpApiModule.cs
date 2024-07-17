using AElf.EntityMapping.Elasticsearch;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.HttpApi.DataStrategy;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Common;
using AElfScanServer.Common.GraphQL;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AElfScanServer.HttpApi;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AElfIndexingElasticsearchModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(AElfEntityMappingElasticsearchModule),
    typeof(AElfScanCommonModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AElfScanCommonModule)
)]
public class HttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<HttpApiModule>(); });

        context.Services.AddSingleton<IHomePageService, HomePageService>();
        context.Services.AddSingleton<IBlockChainService, BlockChainService>();
        context.Services.AddSingleton<IAddressService, AddressService>();
        context.Services.AddSingleton<ISearchService, SearchService>();

        context.Services.AddSingleton<OverviewDataStrategy, OverviewDataStrategy>();
        context.Services.AddSingleton<CurrentBpProduceDataStrategy, CurrentBpProduceDataStrategy>();
        context.Services.AddSingleton<LatestTransactionDataStrategy, LatestTransactionDataStrategy>();
        context.Services.AddSingleton<LatestBlocksDataStrategy, LatestBlocksDataStrategy>();
        context.Services.AddSingleton<IIndexerGenesisProvider, IndexerGenesisProvider>();
        context.Services.AddTransient<IAddressAppService, AddressAppService>();
        context.Services.AddSingleton<IIndexerTokenProvider, IndexerTokenProvider>();
        context.Services.AddTransient<IContractAppService, ContractAppService>();
        context.Services.AddSingleton<IDecompilerProvider, DecompilerProvider>();
        context.Services.AddSingleton<INftInfoProvider, NftInfoProvider>();
        context.Services.AddSingleton<ITokenAssetProvider, TokenAssetProvider>();
        context.Services.AddSingleton<ITokenPriceService, TokenPriceService>();
        context.Services.AddSingleton<ITokenHolderPercentProvider, TokenHolderPercentProvider>();
        context.Services.AddSingleton<INftCollectionHolderProvider, NftCollectionHolderProvider>();
        context.Services.AddTransient<ITokenService, TokenService>();
        context.Services.AddTransient<IChartDataService, ChartDataService>();

        var configuration = context.Services.GetConfiguration();
        Configure<BlockChainOption>(configuration.GetSection("BlockChainServer"));

        Configure<DecompilerOption>(configuration.GetSection("Decompiler"));
        Configure<AELFIndexerOptions>(configuration.GetSection("AELFIndexer"));
        Configure<GlobalOptions>(configuration.GetSection("BlockChain"));
        Configure<ElasticsearchOptions>(configuration.GetSection("Elasticsearch"));

        Configure<WorkerOptions>(configuration.GetSection("Worker"));
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "TokenDataFunctionServer:"; });

        ConfigureGraphQl(context, configuration);


        context.Services.AddSingleton<AELFIndexerProvider, AELFIndexerProvider>();
        context.Services.AddSingleton<HomePageProvider, HomePageProvider>();
        context.Services.AddSingleton<LogEventProvider, LogEventProvider>();
        context.Services.AddSingleton<BlockChainDataProvider, BlockChainDataProvider>();
        context.Services.AddSingleton<ITokenIndexerProvider, TokenIndexerProvider>();
        context.Services.AddSingleton<IBlockChainIndexerProvider, BlockChainIndexerProvider>();
        context.Services.AddSignalR();
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton<IGraphQlFactory, GraphQlFactory>();
        Configure<IndexerOptions>(configuration.GetSection("Indexer"));
    }
}