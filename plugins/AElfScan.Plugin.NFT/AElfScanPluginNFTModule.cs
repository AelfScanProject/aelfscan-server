using AElfScanServer.Common;
using AElfScanServer.Common.GraphQL;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using Microsoft.Extensions.DependencyInjection;
using NFT.backend;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace NFT;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AElfScanCommonModule)
)]
public class AElfScanPluginNFTModule : AElfScanPluginBaseModule<AElfScanPluginNFTModule>

{
    protected override string Name { get; }
    protected override string Version { get; }

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
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "BlockChainDataFunctionServer:"; });
        context.Services.AddSingleton<ITokenPriceService, TokenPriceService>();
        context.Services.AddSingleton<IGraphQlFactory, GraphQlFactory>();
        context.Services.AddSingleton<NftController, NftController>();

        context.Services.AddSingleton<INftService, NftService>();
        context.Services.AddTransient<ITokenIndexerProvider, TokenIndexerProvider>();
    }
}