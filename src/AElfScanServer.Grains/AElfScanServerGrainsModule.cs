using AElfScanServer.Common;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfScanServer.Grains;

[DependsOn(typeof(AElfScanCommonModule))]
public class AElfScanServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AElfScanServerGrainsModule>(); });
    }
}