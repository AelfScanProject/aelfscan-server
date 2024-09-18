using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.Grains;
using AElfScanServer.Grains.Grain.Contract;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Dtos.Indexer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Org.BouncyCastle.Ocsp;
using Orleans;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.HttpApi.Service;

public interface IContractAppService
{
    Task<GetContractListResultDto> GetContractListAsync(GetContractContracts input);
    Task<ContractFileResultDto> GetContractFileAsync(GetContractFileInput input);
    Task<GetContractHistoryResultDto> GetContractHistoryAsync(GetContractHistoryInput input);
    Task<GetContractEventResp> GetContractEventsAsync(GetContractEventReq input);

    Task SaveContractFileAsync(string chainId);
    Task UpdateContractHeightAsync(SynchronizationDto input);
}

[Ump]
public class ContractAppService : IContractAppService
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ContractAppService> _logger;
    private readonly IDecompilerProvider _decompilerProvider;
    private readonly IIndexerTokenProvider _indexerTokenProvider;
    private readonly IIndexerGenesisProvider _indexerGenesisProvider;
    private readonly IBlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly IEntityMappingRepository<LogEventIndex, string> _logEventIndexRepository;
    private readonly IDistributedCache<GetContractListResultDto> _contractListCache;
    private readonly IClusterClient _clusterClient;

    public ContractAppService(IObjectMapper objectMapper, ILogger<ContractAppService> logger,
        IDecompilerProvider decompilerProvider,
        IIndexerTokenProvider indexerTokenProvider, IIndexerGenesisProvider indexerGenesisProvider,
        IOptionsMonitor<GlobalOptions> globalOptions, IBlockChainIndexerProvider blockChainIndexerProvider,
        IEntityMappingRepository<LogEventIndex, string> logEventIndexRepository,
        AELFIndexerProvider aelfIndexerProvider, IDistributedCache<GetContractListResultDto> contractListCache,
        IClusterClient clusterClient)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _decompilerProvider = decompilerProvider;
        _indexerTokenProvider = indexerTokenProvider;
        _indexerGenesisProvider = indexerGenesisProvider;
        _globalOptions = globalOptions;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _logEventIndexRepository = logEventIndexRepository;
        _aelfIndexerProvider = aelfIndexerProvider;
        _contractListCache = contractListCache;
        _clusterClient = clusterClient;
    }

    public async Task<GetContractListResultDto> GetContractListAsync(GetContractContracts input)
    {
        _logger.LogInformation("GetContractListAsync");
        var result = new GetContractListResultDto { List = new List<ContractDto>() };

        var key = IdGeneratorHelper.GenerateId("GetContractListAsync", input.SkipCount, input.MaxResultCount,
            input.ChainId);
        var contractDtos =
            await _contractListCache.GetAsync(key);
        if (contractDtos != null)
        {
            return contractDtos;
        }

        var getContractListResult =
            await _indexerGenesisProvider.GetContractListAsync(input.ChainId,
                input.SkipCount,
                input.MaxResultCount, input.OrderBy, input.Sort, "");
        result.Total = getContractListResult.ContractList.TotalCount;


        var addressList = getContractListResult.ContractList.Items.Select(c => c.Address).ToList();

        var addressTransactionCountList = new List<IndexerAddressTransactionCountDto>();
        var addressTokenList = new List<AccountTokenDto>();

        var tasks = new List<Task>();

        tasks.Add(_blockChainIndexerProvider.GetAddressTransactionCount(input.ChainId, addressList).ContinueWith(task =>
        {
            addressTransactionCountList = task.Result;
        }));


        tasks.Add(_indexerTokenProvider.GetAddressTokenListAsync(input.ChainId,
            "ELF", addressList, 0, addressList.Count * 2).ContinueWith(task => { addressTokenList = task.Result; }));

        await tasks.WhenAll();

        var accountTokenDic = addressTokenList.ToDictionary(c => c.Address + c.Metadata.ChainId, c => c);

        var txnCountDic = addressTransactionCountList.ToDictionary(c => c.Address + c.ChainId, c => c);
        foreach (var info in getContractListResult.ContractList.Items)
        {
            var blockBlockTime = info.Metadata.Block.BlockTime;
            if (info.Metadata.Block.BlockHeight == 1)
            {
                blockBlockTime = input.ChainId == "AELF"
                    ? CommonConstant.AELFOneBlockTime
                    : CommonConstant.TDVVOneBlockTime;
            }


            var contractInfo = new ContractDto
            {
                Address = info.Address,
                ContractVersion = info.ContractVersion == "" ? info.Version.ToString() : info.ContractVersion,
                LastUpdateTime = blockBlockTime,
                Type = info.ContractType,
                ContractName = GetContractName(info.Metadata.ChainId, info.Address)
            };

            if (txnCountDic.TryGetValue(info.Address + info.Metadata.ChainId, out var txnCountInfo))
            {
                contractInfo.Txns = txnCountInfo.Count;
            }


            if (accountTokenDic.TryGetValue(info.Address + info.Metadata.ChainId, out var v))
            {
                contractInfo.Balance = addressTokenList[0].FormatAmount;
            }

            contractInfo.ChainIds = new List<string>() { info.Metadata.ChainId };

            result.List.Add(contractInfo);
        }

        await _contractListCache.SetAsync(key, result);

        return result;
    }


    public string GetContractName(string chainId, string address)
    {
        _globalOptions.CurrentValue.ContractNames.TryGetValue(chainId, out var contractNames);
        if (contractNames == null)
        {
            return "";
        }

        contractNames.TryGetValue(address, out var contractName);

        return contractName;
    }


    public async Task<ContractFileResultDto> GetContractFileAsync(GetContractFileInput input)
    {
        _logger.LogInformation("GetContractFileAsync");
        var contractFileResultDto = await _clusterClient
            .GetGrain<IContractFileGrain>(GrainIdHelper.GenerateContractFileKey(input.ChainId, input.Address))
            .GetAsync();
        contractFileResultDto.ContractName = GetContractName(input.ChainId, input.Address);
        return contractFileResultDto;
    }

    public async Task<GetContractHistoryResultDto> GetContractHistoryAsync(
        GetContractHistoryInput input)
    {
        var result = new GetContractHistoryResultDto();
        var getContractRecordResult =
            await _indexerGenesisProvider.GetContractRecordAsync(input.ChainId, input.Address);

        var getContractListResult =
            await _indexerGenesisProvider.GetContractListAsync(input.ChainId,
                0,
                1, "", "", input.Address);

        var codeHash = getContractListResult.ContractList.Items.First().CodeHash;
        result.Record = getContractRecordResult.Select(t =>
        {
            var tempContractRecord = _objectMapper.Map<ContractInfoDto, ContractRecordDto>(t.ContractInfo);
            tempContractRecord.BlockTime = t.Metadata.Block.BlockTime;
            tempContractRecord.TransactionId = t.TransactionId;
            tempContractRecord.BlockHeight = t.Metadata.Block.BlockHeight;
            tempContractRecord.CodeHash = codeHash;
            tempContractRecord.Version = t.ContractInfo.ContractVersion;
            tempContractRecord.ContractOperationType = t.OperationType;
            return tempContractRecord;
        }).OrderByDescending(t => t.Version).ToList();

        return result;
    }

    public async Task<GetContractEventResp> GetContractEventsAsync(GetContractEventReq req)
    {
        var result = new GetContractEventResp()
        {
            List = new List<LogEventIndex>()
        };

        if (req.BlockHeight > 0)
        {
            var transactionList =
                await _aelfIndexerProvider.GetTransactionsAsync(req.ChainId, req.BlockHeight, req.BlockHeight, "");

            if (transactionList.IsNullOrEmpty())
            {
                return result;
            }

            for (var i = 0; i < transactionList.Count; i++)
            {
                var txn = transactionList[i];

                if (txn.To != req.ContractAddress)
                {
                    continue;
                }

                for (var i1 = 0; i1 < txn.LogEvents.Count; i1++)
                {
                    var curEvent = txn.LogEvents[i1];
                    curEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
                    curEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);
                    var logEvent = new LogEventIndex()
                    {
                        TransactionId = txn.TransactionId,
                        ChainId = req.ChainId,
                        BlockHeight = txn.BlockHeight,
                        MethodName = txn.MethodName,
                        BlockTime = txn.BlockTime,
                        TimeStamp = txn.BlockTime.ToUtcMilliSeconds(),
                        ToAddress = txn.To,
                        ContractAddress = curEvent.ContractAddress,
                        EventName = curEvent.EventName,
                        NonIndexed = nonIndexed,
                        Indexed = indexed,
                        Index = i1
                    };
                    result.List.Add(logEvent);
                }
            }

            result.List = result.List.Skip(req.SkipCount).Take(req.MaxResultCount).ToList();
            result.Total = result.List.Count;
            return result;
        }

        var queryable = _logEventIndexRepository.GetQueryableAsync().Result.Where(c => c.ChainId == req.ChainId)
            .Where(c => c.ToAddress == req.ContractAddress);

        var count = queryable.Count();
        var logEventIndices = queryable.Skip(req.SkipCount).Take(req.MaxResultCount).OrderByDescending(c => c.BlockTime)
            .ToList();

        result.Total = count > 10000 ? 10000 : count;
        result.List = logEventIndices;


        return result;
    }

    public async Task SaveContractFileAsync(string chainId)
    {
        bool flag = false;
        do
        {
            var bizId = GrainIdHelper.GenerateSynchronizationKey(chainId,
                SynchronizationType.ContractFile.ToString());
            var synchronizationDto = await _clusterClient.GetGrain<ISynchronizationGrain>(bizId).GetAsync();
            var list = await _indexerGenesisProvider.GetContractListAsync(chainId, 0, 20, "BlockTime", "asc", "",
                synchronizationDto.LastBlockHeight);
            if (!list.ContractList.Items.IsNullOrEmpty())
            {
                foreach (var contractRecord in list.ContractList.Items)
                {
                    var contractFileId = GrainIdHelper.GenerateContractFileKey(chainId, contractRecord.Address);
                    var contractFileResultDto =
                        await _clusterClient.GetGrain<IContractFileGrain>(contractFileId).GetAsync();
                    if (contractFileResultDto.LastBlockHeight != 0 &&
                        synchronizationDto.LastBlockHeight == contractFileResultDto.LastBlockHeight)
                    {
                        flag = false;
                        continue;
                    }

                    flag = true;
                    var getContractRegistrationResult =
                        await _indexerGenesisProvider.GetContractRegistrationAsync(chainId,
                            contractRecord.CodeHash);

                    if (getContractRegistrationResult.Count == 0)
                    {
                        continue;
                    }

                    var getFilesResult = await _decompilerProvider.GetFilesAsync(getContractRegistrationResult[0].Code);
                    await _clusterClient.GetGrain<IContractFileGrain>(contractFileId).SaveAndUpdateAsync(
                        new ContractFileResultDto
                        {
                            ChainId = chainId,
                            Address = contractRecord.Address,
                            LastBlockHeight = contractRecord.Metadata.Block.BlockHeight,
                            ContractName = GetContractName(chainId, contractRecord.Address),
                            ContractVersion = contractRecord.ContractVersion == ""
                                ? contractRecord.Version.ToString()
                                : contractRecord.ContractVersion,
                            ContractSourceCode = getFilesResult.Data
                        });
                    _logger.LogInformation("SaveContractFileAsync ChainId:{ChainId}, LastBlockHeight {LastBlockHeight}",
                        chainId, contractRecord.Metadata.Block.BlockHeight);
                }
            }
        } while (flag);
    }

    public async Task UpdateContractHeightAsync(SynchronizationDto input)
    {
        var bizId = GrainIdHelper.GenerateSynchronizationKey(input.ChainId,
            SynchronizationType.ContractFile.ToString());
        await _clusterClient.GetGrain<ISynchronizationGrain>(bizId).SaveAndUpdateAsync(input);
    }
}