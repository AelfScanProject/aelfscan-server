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

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.AddBackgroundWorkerAsync<MonthlyActiveAddressWorker>();
        context.AddBackgroundWorkerAsync<TransactionIndexWorker>();
        context.AddBackgroundWorkerAsync<LogEventWorker>();
        context.AddBackgroundWorkerAsync<LogEventDelWorker>();
        context.AddBackgroundWorkerAsync<RoundWorker>();
        context.AddBackgroundWorkerAsync<TransactionRatePerMinuteWorker>();
        context.AddBackgroundWorkerAsync<AddressAssetCalcWorker>();
        context.AddBackgroundWorkerAsync<HomePageOverviewWorker>();
        context.AddBackgroundWorkerAsync<LatestTransactionsWorker>();
        context.AddBackgroundWorkerAsync<LatestBlocksWorker>();
        context.AddBackgroundWorkerAsync<BnElfUsdtPriceWorker>();
        context.AddBackgroundWorkerAsync<DailyNetworkStatisticWorker>();
        context.AddBackgroundWorkerAsync<BlockSizeWorker>();
        context.AddBackgroundWorkerAsync<CurrentBpProduceWorker>();
        context.AddBackgroundWorkerAsync<FixDailyTransactionWorker>();
        context.AddBackgroundWorkerAsync<ContractFileWorker>();
        context.AddBackgroundWorkerAsync<TokenHolderPercentWorker>();
        context.AddBackgroundWorkerAsync<TokenInfoWorker>();
        context.AddBackgroundWorkerAsync<DeleteMergeBlocksWorker>();
        context.AddBackgroundWorkerAsync<MergeAddressWorker>();
        context.AddBackgroundWorkerAsync<FixTokenHolderInfoWorker>();
        context.AddBackgroundWorkerAsync<TwitterSyncWorker>();
    }
}