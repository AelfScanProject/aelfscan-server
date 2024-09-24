using System;
using System.Collections.Generic;
using System.Linq;
using AElf.EntityMapping.Elasticsearch;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Common;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.NodeProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Worker.Core;
using AElfScanServer.Worker.Core.Options;
using AElfScanServer.Worker.Core.Service;
using AElfScanServer.Worker.Core.Worker;
using AElfScanServer.Worker.Core.Worker.MergeDataWorker;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Volo.Abp;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using WorkerOptions = AElfScanServer.Worker.Core.Options.WorkerOptions;

namespace AElfScanServer.Worker;

[DependsOn(
    typeof(AElfScanServerWorkerCoreModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AbpAutofacModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(AElfEntityMappingElasticsearchModule),
    typeof(AElfIndexingElasticsearchModule)
)]
public class AElfScanServerWorkerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureEsIndex(configuration);
        ConfigureContractNameIndex(configuration);
        context.Services.AddSingleton<ITransactionService, TransactionService>();
        context.Services.AddSingleton<ITokenIndexerProvider, TokenIndexerProvider>();
        context.Services.AddSingleton<INftInfoProvider, NftInfoProvider>();
        context.Services.AddSingleton<ITokenAssetProvider, TokenAssetProvider>();
        context.Services.AddSingleton<ITokenPriceService, TokenPriceService>();
        context.Services.AddSingleton<ITokenAssetProvider, TokenAssetProvider>();
        context.Services.AddSingleton<NodeProvider, NodeProvider>();
        context.Services.AddSingleton<IAwakenIndexerProvider, AwakenIndexerProvider>();

        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AElfScanServer:"; });
        Configure<BlockChainProducerInfoSyncWorkerOptions>(configuration.GetSection("BlockChainProducer"));
        Configure<ContractInfoSyncWorkerOptions>(configuration.GetSection("Contract"));
        Configure<SecretOptions>(configuration.GetSection("Secret"));
        Configure<WorkerOptions>(configuration.GetSection("Worker"));

        context.Services.AddHostedService<AElfScanServerHostedService>();
        context.Services.AddHttpClient();
    }


    public void ConfigureContractNameIndex(IConfiguration configuration)
    {
        var blockChainOptions = configuration.GetSection("BlockChain").Get<GlobalOptions>();
        var indexerOptions = configuration.GetSection("AELFIndexer").Get<AELFIndexerOptions>();
        var elasticsearchOptions = configuration.GetSection("Elasticsearch").Get<ElasticsearchOptions>();
        var uris = elasticsearchOptions.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool);
        var elasticClient = new ElasticClient(settings);


        foreach (var indexerOptionsChainId in indexerOptions.ChainIds)
        {
            if (blockChainOptions.ContractNames.TryGetValue(indexerOptionsChainId, out var value))
            {
                var updateAddress = new List<AddressIndex>();
                var searchResponse = elasticClient.Search<AddressIndex>(s => s
                    .Index(BlockChainIndexNameHelper.GenerateAddressIndexName(indexerOptionsChainId))
                    .Query(q => q
                        .Terms(t => t
                            .Field(f => f.Address)
                            .Terms(value.Keys.ToList())
                        )
                    )
                );

                if (searchResponse.IsValid)
                {
                    foreach (var addressIndex in searchResponse.Documents)
                    {
                        if (value.TryGetValue(addressIndex.Address, out var contractName))
                        {
                            addressIndex.Name = contractName;
                            addressIndex.LowerName = contractName.ToLower();
                            updateAddress.Add(addressIndex);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Failed to index object: {searchResponse.DebugInformation}");
                }


                if (!updateAddress.IsNullOrEmpty())
                {
                    var updateResponse = elasticClient.Bulk(b => b
                        .Index(BlockChainIndexNameHelper.GenerateAddressIndexName(indexerOptionsChainId))
                        .UpdateMany<AddressIndex>(updateAddress, (descriptor, addressIndex) => descriptor
                            .Doc(addressIndex)
                            .DocAsUpsert()
                        )
                    );

                    if (!updateResponse.IsValid)
                    {
                        throw new Exception($"Failed to index object: {updateResponse.DebugInformation}");
                    }
                }
            }
        }
    }

    public void ConfigureEsIndex(IConfiguration configuration)
    {
        var indexerOptions = configuration.GetSection("AELFIndexer").Get<AELFIndexerOptions>();
        var elasticsearchOptions = configuration.GetSection("Elasticsearch").Get<ElasticsearchOptions>();
        var uris = elasticsearchOptions.Url.ConvertAll(x => new Uri(x));
        var connectionPool = new StaticConnectionPool(uris);
        var settings = new ConnectionSettings(connectionPool);
        var elasticClient = new ElasticClient(settings);
        if (!elasticClient.Indices.Exists("transactionindex")
                .Exists)
        {
            var indexResponse = elasticClient.Indices.Create(
                "transactionindex", c => c
                    .Settings(s => s
                        .Setting("max_result_window", 200000)
                    )
                    .Map<TransactionIndex>(m => m.AutoMap()));

            if (!indexResponse.IsValid)
            {
                throw new Exception($"Failed to index object: {indexResponse.DebugInformation}");
            }
        }


        foreach (var indexerOptionsChainId in indexerOptions.ChainIds)
        {
            if (!elasticClient.Indices.Exists(BlockChainIndexNameHelper.GenerateAddressIndexName(indexerOptionsChainId))
                    .Exists)
            {
                var indexResponse = elasticClient.Indices.Create(
                    BlockChainIndexNameHelper.GenerateAddressIndexName(indexerOptionsChainId),
                    c => c.Map<AddressIndex>(m => m.AutoMap()));
                if (!indexResponse.IsValid)
                {
                    throw new Exception($"Failed to index object: {indexResponse.DebugInformation}");
                }
            }

            if (!elasticClient.Indices
                    .Exists(BlockChainIndexNameHelper.GenerateTransactionIndexName(indexerOptionsChainId))
                    .Exists)
            {
                var blockChainOptions = configuration.GetSection("BlockChain").Get<GlobalOptions>();
                var indexResponse = elasticClient.Indices.Create(
                    BlockChainIndexNameHelper.GenerateTransactionIndexName(indexerOptionsChainId), c => c
                        .Settings(s => s
                            .Setting("max_result_window", blockChainOptions.TransactionListMaxCount)
                        )
                        .Map<TransactionIndex>(m => m.AutoMap()));
                if (!indexResponse.IsValid)
                {
                    throw new Exception($"Failed to index object: {indexResponse.DebugInformation}");
                }
            }


            if (!elasticClient.Indices
                    .Exists(BlockChainIndexNameHelper.GenerateLogEventIndexName(indexerOptionsChainId))
                    .Exists)
            {
                var indexResponse = elasticClient.Indices.Create(
                    BlockChainIndexNameHelper.GenerateLogEventIndexName(indexerOptionsChainId),
                    c => c.Map<LogEventIndex>(m => m.AutoMap()));
                if (!indexResponse.IsValid)
                {
                    throw new Exception($"Failed to index object: {indexResponse.DebugInformation}");
                }
            }
        }
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // context.AddBackgroundWorkerAsync<MonthlyActiveAddressWorker>();
        // context.AddBackgroundWorkerAsync<TransactionIndexWorker>();
        // context.AddBackgroundWorkerAsync<LogEventWorker>();
        // context.AddBackgroundWorkerAsync<LogEventDelWorker>();
        // context.AddBackgroundWorkerAsync<RoundWorker>();
        // context.AddBackgroundWorkerAsync<TransactionRatePerMinuteWorker>();
        // context.AddBackgroundWorkerAsync<AddressAssetCalcWorker>();
        context.AddBackgroundWorkerAsync<HomePageOverviewWorker>();
        // context.AddBackgroundWorkerAsync<LatestTransactionsWorker>();
        // context.AddBackgroundWorkerAsync<LatestBlocksWorker>();
        // context.AddBackgroundWorkerAsync<BnElfUsdtPriceWorker>();
        // context.AddBackgroundWorkerAsync<DailyNetworkStatisticWorker>();
        // context.AddBackgroundWorkerAsync<BlockSizeWorker>();
        // context.AddBackgroundWorkerAsync<CurrentBpProduceWorker>();
        // context.AddBackgroundWorkerAsync<FixDailyTransactionWorker>();
        // context.AddBackgroundWorkerAsync<ContractFileWorker>();
        // context.AddBackgroundWorkerAsync<TokenHolderPercentWorker>();
        // context.AddBackgroundWorkerAsync<TokenInfoWorker>();
    }
}