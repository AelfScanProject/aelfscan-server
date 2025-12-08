using AElf.OpenTelemetry;
using AElfScanServer.Grains;
using AElfScanServer.Common;
// using AElfScanServer.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;

namespace AElfScanServer.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(OpenTelemetryModule),
     typeof(AElfScanCommonModule),
     typeof(AElfScanServerGrainsModule),
    typeof(AbpCachingStackExchangeRedisModule))]
public class AElfScanServerOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHostedService<AElfScanServerHostedService>();
        ConfigureCache(configuration);
        // context.Services.AddTransient<IAppDeployManager, KubernetesAppManager>();
    }

    //Disable TokenCleanupService
    // private void ConfigureTokenCleanupService()
    // {
    //     Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    // }

    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AElfScanServer:"; });
    }
    //Create the ElasticSearch Index & Initialize field cache based on Domain Entity
    // private void ConfigureEsIndexCreation()
    // {
    //     Configure<CollectionCreateOptions>(x => { x.AddModule(typeof(AeFinderDomainModule)); });
    // }
}