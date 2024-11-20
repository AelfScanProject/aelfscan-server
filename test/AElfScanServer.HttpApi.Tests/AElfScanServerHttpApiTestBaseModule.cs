using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace AElfScanServer.HttpApi.Test;


[DependsOn(
    typeof(HttpApiModule)

)]
public class AElfScanServerHttpApiTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddLogging(configure => configure.AddConsole());
        
    }
}
