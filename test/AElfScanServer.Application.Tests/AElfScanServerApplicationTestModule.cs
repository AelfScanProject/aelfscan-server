using System.Collections.Generic;
using AElfScanServer.Common;
using AElfScanServer.Common.Address.Provider;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.ThirdPart.Exchange;
using AElfScanServer.Common.Token;
using AElfScanServer.HttpApi;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Mocks;
using AElfScanServer.Mocks.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElfScanServer;

[DependsOn(
    typeof(HttpApiModule),
    typeof(AElfScanServerApplicationContractsModule),
    typeof(AElfScanServerOrleansTestBaseModule),
    typeof(AElfScanServerDomainTestModule)
)]
public class AElfScanServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<ElasticsearchOptions>(o => o.Url = new List<string>()
        {
            "http://127.0.0.1:9200"
        });
        context.Services.AddSingleton<ITokenIndexerProvider, MockTokenIndexerProvider>();
        context.Services.AddSingleton<IAddressInfoProvider, MockAddressInfoProvider>();
        context.Services.AddSingleton<IBlockChainIndexerProvider, MockBlockChainIndexerProvider>();
        context.Services.AddSingleton<ICacheProvider, LocalCacheProvider>();
        context.Services.AddSingleton<IBlockChainDataProvider, MockBlockChainDataProvider>();
        context.Services.AddSingleton<IAELFIndexerProvider, MockAELFIndexerProvider>();
        context.Services.AddSingleton<IIndexerGenesisProvider, MockIndexerGenesisProvider>();
        context.Services.AddSingleton<IDecompilerProvider, MockDecompilerProvider>();
        context.Services.AddSingleton<ITokenPriceService, MockTokenPriceService>();
        context.Services.AddSingleton<IGenesisPluginProvider, MockGenesisPluginProvider>();
        context.Services.AddSingleton<ICoinMarketCapProvider, MockCoinMarketCapProvider>();
        Configure<GlobalOptions>(options =>
        {
            options.OrganizationAddress = "OrganizationAddress";
            options.ContractAddressConsensus = new Dictionary<string, string>()
            {
                { "AELF", "ContractAddressConsensusAddress" }
            };
            options.BPNames = new Dictionary<string, Dictionary<string, string>>();
            options.ContractNames = new Dictionary<string, Dictionary<string, string>>()
            {
                {
                    "AELF", new Dictionary<string, string>()
                    {
                        { "TokenContractAddress", "TokenContractAddress" }
                    }
                }
            };

            options.SideChainId = "tDVW";
            options.FilterTypes = new Dictionary<string, int>()
            {
                { "All Filter", 0 },
                { "Tokens", 1 },
                { "Accounts", 2 },
                { "Contracts", 3 },
                { "Nfts", 4 }
            };
        });
        Configure<ChainOptions>(options => options.ChainInfos = new Dictionary<string, ChainOptions.ChainInfo>()
        {
            {
                MockUtil.MainChainId, new ChainOptions.ChainInfo
                {
                    TokenContractAddress = "TokenContractAddress"
                }
            }
        });
        Configure<TokenInfoOptions>(options =>
        {
            options.NonResourceSymbols = new HashSet<string>()
            {
                "SGR-1"
            };
        });
    }
}