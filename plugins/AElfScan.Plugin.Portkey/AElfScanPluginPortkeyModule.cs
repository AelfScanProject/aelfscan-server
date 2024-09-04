using AElfScanServer.Common;
using AElfScanServer.Common.GraphQL;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.HttpApi.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portkey.backend;
using Portkey.Options;
using Portkey.Provider;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace Portkey;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreMvcModule)
)]
public class AElfScanPluginPortkeyModule : AElfScanPluginBaseModule<AElfScanPluginPortkeyModule>

{
    protected override string Name { get; }
    protected override string Version { get; }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<PluginUrlOptions>(configuration.GetSection("PluginUrl"));

        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "BlockChainDataFunctionServer:"; });
        context.Services.AddSingleton<IGraphQLHelper, GraphQLHelper>();
        context.Services.AddSingleton<IPortkeyTransactionProvider, PortkeyTransactionProvider>();
        context.Services.AddSingleton<IAddressTypeProvider, PortkeyTransactionProvider>();
        context.Services.AddSingleton<PortkeyController, PortkeyController>();
        context.Services.AddSingleton<IPortkeyService, PortkeyService>();
        context.Services.AddSingleton<IBlockChainIndexerProvider, BlockChainIndexerProvider>();
        context.Services.AddSingleton<DynamicTransactionService, DynamicTransactionService>();
        context.Services.Replace(new ServiceDescriptor(typeof(IDynamicTransactionService), typeof(PortkeyService),
            ServiceLifetime.Singleton));
    }
}