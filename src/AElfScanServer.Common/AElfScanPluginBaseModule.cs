using AElf.EntityMapping.Options;
using AElfScanServer.Common.Dtos.ChartData;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfScanServer.Common;

public abstract class AElfScanPluginBaseModule<TModule> : AbpModule
    where TModule : AElfScanPluginBaseModule<TModule>
{
    protected abstract string Name { get; }
    protected abstract string Version { get; }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TModule>(); });
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(TModule).Assembly);
        });



        ConfigureServices(context.Services);
    }

    protected virtual void ConfigureServices(IServiceCollection serviceCollection)
    {
    }
}