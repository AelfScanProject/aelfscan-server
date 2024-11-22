using AElfScanServer;
using AElfScanServer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace ETransferServer.Grain.Test;

[DependsOn(
    typeof(AElfScanServerGrainsModule),
    typeof(AElfScanServerDomainTestModule),
    typeof(AElfScanServerDomainModule)
)]
public class AElfScanServerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        // context.Services.AddHttpClient();
        // context.Services.Configure<CoinGeckoOptions>(o => { o.CoinIdMapping["ELF"] = "aelf"; });
        // context.Services.Configure<CAAccountOption>(o =>
        // {
        //     o.CAAccountRequestInfoMaxLength = 100;
        //     o.CAAccountRequestInfoExpirationTime = 1;
        // });
    }
}