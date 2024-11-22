using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElfScanServer;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule)
)]
public class AElfScanServerTestBaseModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddConfiguration(context.Services.GetConfiguration())  
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "OpenTelemetry:ServiceName", "test" },
                { "OpenTelemetry:ServiceVersion", "test" }
            });

        var newConfiguration = configurationBuilder.Build();

        context.Services.Replace(ServiceDescriptor.Singleton<IConfiguration>(newConfiguration));

    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = false;
        });

        context.Services.AddAlwaysAllowAuthorization();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
    }

}