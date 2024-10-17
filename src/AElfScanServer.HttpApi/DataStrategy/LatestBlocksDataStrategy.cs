using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElf.ExceptionHandler;
using AElf.ExceptionHandler.ABP;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.ChartData;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.DataStrategy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.HttpApi.DataStrategy;

public class LatestBlocksDataStrategy : DataStrategyBase<string, BlocksResponseDto>
{
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly IEntityMappingRepository<BlockIndex, string> _blockResponseDtoRepository;
    private readonly IObjectMapper _objectMapper;


    public LatestBlocksDataStrategy(IOptions<RedisCacheOptions> optionsAccessor,
        ILogger<DataStrategyBase<string, BlocksResponseDto>> logger,
        IOptionsMonitor<GlobalOptions> globalOptions,
        AELFIndexerProvider aelfIndexerProvider, IDistributedCache<string> cache,
        ITokenIndexerProvider tokenIndexerProvider,
        IEntityMappingRepository<BlockIndex, string> blockResponseDtoRepository, IObjectMapper objectMapper) : base(
        optionsAccessor, logger, cache)
    {
        _globalOptions = globalOptions;
        _aelfIndexerProvider = aelfIndexerProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _blockResponseDtoRepository = blockResponseDtoRepository;
        _objectMapper = objectMapper;
    }

    public override async Task<BlocksResponseDto> QueryData(string chainId)
    {
        var result = new BlocksResponseDto()
        {
            Blocks = new List<BlockRespDto>()
        };
        var summariesList = await _aelfIndexerProvider.GetLatestSummariesAsync(chainId);
        var blockHeightAsync = summariesList.First().LatestBlockHeight;


        var blockList = await _aelfIndexerProvider.GetLatestBlocksAsync(chainId,
            blockHeightAsync - 10,
            blockHeightAsync);

        Dictionary<long, long> blockBurntFee = new Dictionary<long, long>();

        
        blockBurntFee = await ParseBlockBurntAsync(chainId,
            blockHeightAsync - 10,
            blockHeightAsync);
        for (var i = blockList.Count - 1; i >= 0; i--)
        {
            var indexerBlockDto = blockList[i];
            var latestBlockDto = new BlockRespDto();

            latestBlockDto.BlockHeight = indexerBlockDto.BlockHeight;
            latestBlockDto.Timestamp = DateTimeHelper.GetTotalSeconds(indexerBlockDto.BlockTime);
            latestBlockDto.TransactionCount = indexerBlockDto.TransactionIds.Count;
            latestBlockDto.ProducerAddress = indexerBlockDto.Miner;
            if (_globalOptions.CurrentValue.BPNames.TryGetValue(chainId, out var bpNames))
            {
                if (bpNames.TryGetValue(indexerBlockDto.Miner, out var name))
                {
                    latestBlockDto.ProducerName = name;
                }
            }

            latestBlockDto.BurntFees = blockBurntFee.TryGetValue(indexerBlockDto.BlockHeight, out var value)
                ? value.ToString()
                : "0";
            if (i == 0)
            {
                latestBlockDto.TimeSpan = result.Blocks.Last().TimeSpan;
            }
            else
            {
                latestBlockDto.TimeSpan = (Convert.ToDouble(0 < blockList.Count
                    ? DateTimeHelper.GetTotalMilliseconds(indexerBlockDto.BlockTime) -
                      DateTimeHelper.GetTotalMilliseconds(blockList[i - 1].BlockTime)
                    : 0) / 1000).ToString("0.0");
            }


            result.Blocks.Add(latestBlockDto);
            if (chainId == "AELF")
            {
                latestBlockDto.Reward = _globalOptions.CurrentValue.BlockRewardAmountStr;
            }
            else
            {
                latestBlockDto.Reward = "0";
            }

            latestBlockDto.ChainId = indexerBlockDto.ChainId;
            latestBlockDto.ChainIds.Add(indexerBlockDto.ChainId);
        }

        if (result.Blocks.Count > 6)
        {
            result.Blocks = result.Blocks.GetRange(result.Blocks.Count - 6, 6);
        }


        ;
        await _blockResponseDtoRepository.AddOrUpdateManyAsync(
            _objectMapper.Map<List<BlockRespDto>, List<BlockIndex>>(result.Blocks));
        DataStrategyLogger.LogInformation("Merge block insert:{count},{chainId}", result.Blocks, chainId);
        return result;
    }


    [ExceptionHandler(typeof(IOException),typeof(TimeoutException),typeof(Exception), Message = "ParseBlockBurntAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException),ReturnDefault = ReturnDefault.New, LogTargets = ["chainId","startBlockHeight","endBlockHeight"])]
    public virtual async Task<Dictionary<long, long>> ParseBlockBurntAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {

        List<BlockBurnFeeDto> blockBurntFeeListAsync;
        var result = new Dictionary<long, long>();

        blockBurntFeeListAsync =
            await _tokenIndexerProvider.GetBlockBurntFeeListAsync(chainId, startBlockHeight, endBlockHeight);


        foreach (var blockBurnFeeDto in blockBurntFeeListAsync)
        {
            result.Add(blockBurnFeeDto.BlockHeight, blockBurnFeeDto.Amount);
        }


        return result;
    }
    
    
    public async Task Finally1(CommonAddressDto input)
    {
       DataStrategyLogger.LogInformation($"test 1   {input.Address}");
    }
   
    public override string DisplayKey(string chainId)
    {
        return RedisKeyHelper.LatestBlocks(chainId);
    }
}