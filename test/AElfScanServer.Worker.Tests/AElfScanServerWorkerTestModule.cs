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
        context.Services.AddSingleton<ChainTestHelper>();
        context.Services.AddSingleton<TradePairTestHelper>();
        context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IAElfClientProvider, MockAelfClientProvider>();
        context.Services.AddSingleton<ISyncStateProvider, MockSyncStateProvider>();
        context.Services.Configure<PortfolioOptions>(o => { o.DataVersion = "v1"; });
    }
}