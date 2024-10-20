using AElf.Client.Service;
using AElf.EntityMapping.Options;
using AElf.OpenTelemetry;
using AElfScanServer.Common.Address.Provider;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.GraphQL;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.NodeProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.ThirdPart.Exchange;
using AElfScanServer.Common.Token.Provider;
using Aetherlink.PriceServer;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Modularity;

namespace AElfScanServer.Common;

[DependsOn(
    typeof(OpenTelemetryModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AetherlinkPriceServerModule)
)]
public class AElfScanCommonModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();


        Configure<ChainOptions>(configuration.GetSection("ChainOptions"));
        Configure<ApiClientOption>(configuration.GetSection("ApiClient"));
        Configure<IndexerOptions>(configuration.GetSection("Indexers"));
        Configure<ExchangeOptions>(configuration.GetSection("Exchange"));
        Configure<CoinGeckoOptions>(configuration.GetSection("CoinGecko"));
        Configure<TokenInfoOptions>(configuration.GetSection("TokenInfoOptions"));
        Configure<AssetsInfoOptions>(configuration.GetSection("AssetsInfoOptions"));
        Configure<CollectionCreateOptions>(x => { x.AddModule(typeof(AElfScanCommonModule)); });
        context.Services.AddSingleton<IHttpProvider, HttpProvider>();
        context.Services.AddSingleton<IGraphQlFactory, GraphQlFactory>();
        context.Services.AddTransient<IExchangeProvider, OkxProvider>();
        context.Services.AddTransient<IExchangeProvider, BinanceProvider>();
        context.Services.AddTransient<IExchangeProvider, CoinGeckoProvider>();
        context.Services.AddTransient<ITokenExchangeProvider, TokenExchangeProvider>();
        context.Services.AddTransient<ITokenInfoProvider, TokenInfoProvider>();
        context.Services.AddTransient<IAddressInfoProvider, AddressInfoProvider>();
        context.Services.AddTransient<IGenesisPluginProvider, GenesisPluginProvider>();
        context.Services.AddTransient<ITokenIndexerProvider, TokenIndexerProvider>();
        context.Services.AddTransient<INftCollectionHolderProvider, NftCollectionHolderProvider>();
        context.Services.AddTransient<INftInfoProvider, NftInfoProvider>();
        context.Services.AddTransient<ITokenInfoProvider, TokenInfoProvider>();
        context.Services.AddTransient<CoinMarketCapProvider, CoinMarketCapProvider>();
        context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
        context.Services.AddHttpClient();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
    }
}