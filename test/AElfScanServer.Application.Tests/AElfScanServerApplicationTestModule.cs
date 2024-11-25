using System.Collections.Generic;
using AElfScanServer.Common;
using AElfScanServer.Common.Address.Provider;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Mocks;
using AElfScanServer.Mocks.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElfScanServer;

[DependsOn(
    typeof(AbpEventBusModule),
    typeof(HttpApiModule),
    typeof(AElfScanCommonModule),
    typeof(AElfScanServerOrleansTestBaseModule),
    typeof(AElfScanServerApplicationContractsModule),
    typeof(AElfScanServerOrleansTestBaseModule),
    typeof(AElfScanServerDomainTestModule)
)]
public class AElfScanServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<ElasticsearchOptions>(o => o.Url = new List<string>()
        {
            "http://127.0.0.1:9200"
        });
        Configure<GlobalOptions>(o => o.SideChainId = "tDVW");
        context.Services.AddSingleton<ITokenIndexerProvider, MockTokenIndexerProvider>();
        context.Services.AddSingleton<IAddressInfoProvider, MockAddressInfoProvider>();
        context.Services.AddSingleton<IBlockChainIndexerProvider, MockBlockChainIndexerProvider>();
        context.Services.AddSingleton<ICacheProvider, LocalCacheProvider>();
    }
}