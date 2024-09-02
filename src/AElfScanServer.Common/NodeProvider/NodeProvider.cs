using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.EntityMapping.Elasticsearch.Options;
using AElf.EntityMapping.Repositories;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.Common.Options;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.NodeProvider;

public class NodeProvider : AbpRedisCache, ISingletonDependency
{
    private readonly GlobalOptions _globalOptions;
    private readonly IHttpProvider _httpProvider;


    private readonly ILogger<NodeProvider> _logger;
    private readonly IEntityMappingRepository<BlockSizeErrInfoIndex, string> _blockSizeErrInfoIndexRepository;

    public NodeProvider(
        ILogger<NodeProvider> logger, IOptionsMonitor<GlobalOptions> blockChainOptions,
        IOptions<RedisCacheOptions> optionsAccessor,
        IHttpProvider httpProvider,
        IEntityMappingRepository<BlockSizeErrInfoIndex, string> blockSizeErrInfoIndexRepository
    ) : base(optionsAccessor)
    {
        _logger = logger;
        _globalOptions = blockChainOptions.CurrentValue;
        _httpProvider = httpProvider;
        _blockSizeErrInfoIndexRepository = blockSizeErrInfoIndexRepository;
    }


    public async Task<BlockSizeDto> GetBlockSize(string chainId, long blockHeight)
    {
        try
        {
            var apiPath = string.Format("/api/blockChain/blockByHeight?blockHeight={0}&includeTransactions=true",
                blockHeight);

            var response =
                await _httpProvider.InvokeAsync<BlockSizeDto>(_globalOptions.ChainNodeHosts[chainId],
                    new ApiInfo(HttpMethod.Get, apiPath));

            return response;
        }
        catch (Exception e)
        {
            var blockSizeErrInfoIndex = new BlockSizeErrInfoIndex()
            {
                ChainId = chainId,
                BlockHeight = blockHeight,
                // ErrMsg = e,
                Date = DateTime.UtcNow
            };
            await _blockSizeErrInfoIndexRepository.AddOrUpdateAsync(blockSizeErrInfoIndex);
            var blockSizeDto = new BlockSizeDto();
            blockSizeDto.PullFalse = true;
            _logger.LogError(e,"GetBlockSize {chainId},blockHeight:{blockHeight}", chainId, blockHeight);
            return null;
        }
    }
}