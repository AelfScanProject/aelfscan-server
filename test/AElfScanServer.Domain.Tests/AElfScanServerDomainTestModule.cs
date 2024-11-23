using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Elasticsearch.Options;
using AElf.EntityMapping.Options;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Options;
using AElf.Indexing.Elasticsearch.Services;
using AElfScanServer.Common;
using AElfScanServer.HttpApi;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElfScanServer;

[DependsOn(
    typeof(AElfScanServerTestBaseModule)
)]
public class AElfScanServerDomainTestModule : AbpModule
{
     public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<CollectionCreateOptions>(x =>
        {
            x.AddModule(typeof(AElfScanCommonModule));
            x.AddModule(typeof(HttpApiModule));
        });
        
         /*// Do not modify this!!!
         context.Services.Configure<EsEndpointOption>(options =>
         {
             options.Uris = new List<string> { "http://127.0.0.1:9200"};
         });*/
            
        /*context.Services.Configure<IndexSettingOptions>(options =>
        {
            options.NumberOfReplicas = 1;
            options.NumberOfShards = 1;
            options.Refresh = Refresh.True;
            options.IndexPrefix = "AeFinder";
        });*/
        context.Services.Configure<AElfEntityMappingOptions>(options =>
        {
            options.CollectionPrefix = "test";
            // options.ShardInitSettings = InitShardInitSettingOptions();
        });
        context.Services.Configure<ElasticsearchOptions>(
            options =>
            {
                options.NumberOfShards = 1;
                options.NumberOfReplicas = 1;
                options.Refresh = Refresh.True;
            }
        );
    }
    
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var option = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AElfEntityMappingOptions>>();
        if(option.Value.CollectionPrefix.IsNullOrEmpty())
            return;
        
        var clientProvider = context.ServiceProvider.GetRequiredService<IElasticsearchClientProvider>();
        var client = clientProvider.GetClient();
        var indexPrefix = option.Value.CollectionPrefix.ToLower();
        
        client.Indices.Delete(indexPrefix+"*");
        client.Indices.DeleteTemplate(indexPrefix + "*");
    }

}