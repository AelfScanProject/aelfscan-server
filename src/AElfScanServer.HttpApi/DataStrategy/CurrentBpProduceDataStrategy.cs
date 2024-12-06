using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.Consensus.AEDPoS;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.DataStrategy;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Volo.Abp.Caching;

namespace AElfScanServer.HttpApi.DataStrategy;

public class CurrentBpProduceDataStrategy : DataStrategyBase<string, BlockProduceInfoDto>
{
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly IOptionsMonitor<SecretOptions> _secretOptions;


    public CurrentBpProduceDataStrategy(IOptions<RedisCacheOptions> optionsAccessor,
        IOptionsMonitor<GlobalOptions> globalOptions, IOptionsMonitor<SecretOptions> secretOptions,
        ILogger<DataStrategyBase<string, BlockProduceInfoDto>> logger, IDistributedCache<string> cache
    ) : base(
        optionsAccessor, logger,cache)
    {
        _globalOptions = globalOptions;
        _secretOptions = secretOptions;
    }

    public override async Task<BlockProduceInfoDto> QueryData(string chainId)
    {
        var client = new AElfClient(_globalOptions.CurrentValue.ChainNodeHosts[chainId]);

        var param = new Empty()
        {
        };

        var transaction = await client.GenerateTransactionAsync(
            client.GetAddressFromPrivateKey(_secretOptions.CurrentValue.ContractPrivateKey),
            _globalOptions.CurrentValue.ContractAddressConsensus[chainId],
            "GetCurrentRoundInformation", param);


        var blockProduceInfoDto = new BlockProduceInfoDto();
        var signTransaction = client.SignTransaction(_secretOptions.CurrentValue.ContractPrivateKey, transaction);

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto()
        {
            RawTransaction = HexByteConvertorExtensions.ToHex(signTransaction.ToByteArray())
        });

        var round = Round.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));

        var currentProduceInfoDtos = new List<BlockProduceDto>();

        foreach (var minerInRound in round.RealTimeMinersInformation)
        {
            var address = client.GetAddressFromPubKey(minerInRound.Key);
            currentProduceInfoDtos.Add(new BlockProduceDto()
            {
                ProducerAddress = address,
                Order = minerInRound.Value.Order,
                BlockCount = minerInRound.Value.ActualMiningTimes.Count,
                ProducerName = await GetBpName(chainId, address)
            });
        }

        var produceInfoDtos = currentProduceInfoDtos.OrderBy(c => c.Order).ToList();
        for (var i = 0; i < produceInfoDtos.Count; i++)
        {
            if (i == 0 && currentProduceInfoDtos[i].BlockCount == 0)
            {
                currentProduceInfoDtos[i].IsMinning = true;
                break;
            }

            if (currentProduceInfoDtos[i].BlockCount == 0)
            {
                currentProduceInfoDtos[i - 1].IsMinning = true;
                break;
            }

            if (i == produceInfoDtos.Count - 1)
            {
                currentProduceInfoDtos[i].IsMinning = true;
                break;
            }
        }

        blockProduceInfoDto.List = currentProduceInfoDtos;

        return blockProduceInfoDto;
    }

    public override string DisplayKey(string chainId)
    {
        return RedisKeyHelper.CurrentBpProduce(chainId);
    }

    public async Task<string> GetBpName(string chainId, string address)
    {
        if (_globalOptions.CurrentValue.BPNames == null)
        {
            return "";
        }

        _globalOptions.CurrentValue.BPNames.TryGetValue(chainId, out var contractNames);
        if (contractNames == null)
        {
            return "";
        }

        contractNames.TryGetValue(address, out var contractName);

        return contractName;
    }
}