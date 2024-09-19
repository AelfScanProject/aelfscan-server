using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.DataStrategy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.Caching;

namespace AElfScanServer.HttpApi.DataStrategy;

public class LatestBlocksDataStrategy : DataStrategyBase<string, BlocksResponseDto>
{
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly AELFIndexerProvider _aelfIndexerProvider;


    public LatestBlocksDataStrategy(IOptions<RedisCacheOptions> optionsAccessor,
        ILogger<DataStrategyBase<string, BlocksResponseDto>> logger,
        IOptionsMonitor<GlobalOptions> globalOptions,
        AELFIndexerProvider aelfIndexerProvider,IDistributedCache<string> cache) : base(
        optionsAccessor, logger,cache)
    {
        _globalOptions = globalOptions;
        _aelfIndexerProvider = aelfIndexerProvider;
    }

    public override async Task<BlocksResponseDto> QueryData(string chainId)
    {
        var result = new BlocksResponseDto()
        {
            Blocks = new List<BlockResponseDto>()
        };
        var summariesList = await _aelfIndexerProvider.GetLatestSummariesAsync(chainId);
        var blockHeightAsync = summariesList.First().LatestBlockHeight;


        var blockList = await _aelfIndexerProvider.GetLatestBlocksAsync(chainId,
            blockHeightAsync - 10,
            blockHeightAsync);
        
        for (var i = blockList.Count - 1; i >= 0; i--)
        {
            var indexerBlockDto = blockList[i];
            var latestBlockDto = new BlockResponseDto();

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
        }

        if (result.Blocks.Count > 6)
        {
            result.Blocks = result.Blocks.GetRange(result.Blocks.Count - 6, 6);
        }

        return result;
    }

    public override string DisplayKey(string chainId)
    {
        return RedisKeyHelper.LatestBlocks(chainId);
    }
}