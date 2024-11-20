using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace AElfScanServer.Worker.Tests;

[DependsOn(
    typeof(AElfScanServerWorkerModule)

)]
public class AElfScanServerWorkerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddLogging(configure => configure.AddConsole());
        
    }
}