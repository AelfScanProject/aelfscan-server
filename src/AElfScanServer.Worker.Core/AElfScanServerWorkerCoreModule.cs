﻿using AElf.EntityMapping.Elasticsearch;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Worker.Core.Provider;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfScanServer.Worker.Core;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AElfEntityMappingElasticsearchModule),
    typeof(AElfIndexingElasticsearchModule)
)]
public class AElfScanServerWorkerCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AElfScanServerWorkerCoreModule>(); });
        context.Services.AddTransient<IStorageProvider, StorageProvider>();
    }
}