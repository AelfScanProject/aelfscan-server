using System.Collections.Generic;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElfScanServer;

[DependsOn(
    typeof(AbpEventBusModule),
    typeof(HttpApiModule),
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
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<HttpApiModule>(); });
        Configure<ElasticsearchOptions>(o => o.Url = new List<string>()
        {
            "http://127.0.0.1:9200"
        });
        
    }
}