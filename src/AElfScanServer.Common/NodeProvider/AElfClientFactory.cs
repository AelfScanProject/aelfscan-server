using System.Collections.Concurrent;
using AElf.Client.Service;
using AElfScanServer.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.NodeProvider;

public interface IBlockchainClientFactory<T> 
    where T : class
{
    T GetClient(string chainName);
}

public class AElfClientFactory : IBlockchainClientFactory<AElfClient>,ISingletonDependency
{
        private readonly GlobalOptions _globalOptions;
        private readonly ConcurrentDictionary<string, AElfClient> _clientDic;
        private readonly ILogger<AElfClientFactory> _logger;

        public AElfClientFactory(IOptionsMonitor<GlobalOptions> blockChainOptions,ILogger<AElfClientFactory> logger)
        {
            _globalOptions = blockChainOptions.CurrentValue;
            _clientDic = new ConcurrentDictionary<string, AElfClient>();
            _logger = logger;
        }

        public AElfClient GetClient(string chainName)
        {
            var chainUrl = _globalOptions.ChainNodeHosts[chainName];
            _logger.LogInformation("chainUrl {ChainUrl}", chainUrl);
            if (_clientDic.TryGetValue(chainName, out var client))
            {
                return client;
            }

            client = new AElfClient(chainUrl);
            _clientDic[chainName] = client;
            return client;
        }
}