using System.Globalization;
using AElfScanServer.Common;
using AElfScanServer.Common.Constant;
using AElfScanServer.Common.Contract.Provider;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.HttpApi.Service;
using CAServer.CAActivity.Provider;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.backend;
using Portkey.Dtos;
using Portkey.Provider;
using Portkey.UserAssets;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Portkey.backend;

public interface IPortkeyService
{
    public Task<TransactionsResponseDto> GetTransactionsAsync(GetTransactionsReq input);
}

public class PortkeyService : IPortkeyService, ISingletonDependency, IDynamicTransactionService

{
    private readonly ILogger<PortkeyService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IPortkeyTransactionProvider _portkeyTransactionProvider;
    private readonly IBlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly DynamicTransactionService _dynamicTransactionService;
    private readonly string CaAddress = "28PcLvP41ouUd6UNGsNRvKpkFTe6am34nPy4YPsWUJnZNwUvzM";


    public PortkeyService(ILogger<PortkeyService> logger,
        IObjectMapper objectMapper,
        IPortkeyTransactionProvider portkeyTransactionProvider,
        IBlockChainIndexerProvider blockChainIndexerProvider,
        DynamicTransactionService dynamicTransactionService
    )
    {
        _logger = logger;
        _portkeyTransactionProvider = portkeyTransactionProvider;
        _objectMapper = objectMapper;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _dynamicTransactionService = dynamicTransactionService;
    }

    public async Task<AElfScanServer.HttpApi.Dtos.TransactionsResponseDto> GetTransactionsAsync(
        TransactionsRequestDto requestDto)
    {
        var caHolderManagerInfoAsync =
            await _portkeyTransactionProvider.GetCaHolderManagerInfoAsync(new List<string>() { requestDto.Address });

        if (caHolderManagerInfoAsync == null || caHolderManagerInfoAsync.CaHolderManagerInfo.IsNullOrEmpty())
        {
            return _dynamicTransactionService.GetTransactionsAsync(requestDto).Result;
        }

        var result = await GetTransactionsAsync(new GetTransactionsReq()
        {
            CaAddress = new List<string>() { requestDto.Address },
            ChainId = requestDto.ChainId,
            SkipCount = (int)requestDto.SkipCount,
            MaxResultCount = (int)requestDto.MaxResultCount
        });
        return result;
    }


    public async Task<TransactionsResponseDto> GetTransactionsAsync(GetTransactionsReq input)
    {
        var caAddressInfos = new List<CAAddressInfo>();
        foreach (var s in input.CaAddress)
        {
            caAddressInfos.Add(new CAAddressInfo()
            {
                CaAddress = s,
                ChainId = input.ChainId
            });
        }

        var transactions = await _portkeyTransactionProvider.GetActivitiesAsync(caAddressInfos, input.ChainId,
            "", null, input.SkipCount, input.MaxResultCount);

        var transactionResponseDtos = new List<TransactionResponseDto>();

        var transactionIds = new List<string>();
        foreach (var indexerTransaction in transactions.CaHolderTransaction.Data)
        {
            transactionIds.Add(indexerTransaction.TransactionId);
            var txn = new TransactionResponseDto()
            {
                TransactionId = indexerTransaction.TransactionId,
                BlockHeight = indexerTransaction.BlockHeight,
                Method = indexerTransaction.MethodName,
                Timestamp = indexerTransaction.Timestamp,
                From = new CommonAddressDto() { Address = indexerTransaction.FromAddress, IsManager = true },
                To = new CommonAddressDto()
                {
                    Address = indexerTransaction.ToContractAddress.IsNullOrEmpty()
                        ? CaAddress
                        : indexerTransaction.ToContractAddress,
                    IsManager = true
                }
            };
            var fee = indexerTransaction?.TransactionFees
                .Where(c => c.Symbol == "ELF")
                .FirstOrDefault();

            txn.TransactionFee = fee != null ? fee.Amount.ToString() : "0";

            transactionResponseDtos.Add(txn);
        }

        var result = await _blockChainIndexerProvider.GetTransactionsByHashsAsync(new TransactionsByHashRequestDto
        {
            Hashs = transactionIds,
            SkipCount = 0,
            MaxResultCount = transactionIds.Count
        });


        if (result != null && !result.Items.IsNullOrEmpty())
        {
            var dic = result.Items.ToDictionary(c => c.TransactionId, c => c);


            foreach (var transactionResponseDto in transactionResponseDtos)
            {
                if (dic.TryGetValue(transactionResponseDto.TransactionId, out var v))
                {
                    transactionResponseDto.TransactionValue = v.TransactionValue.ToString();
                }
            }
        }

        return new TransactionsResponseDto()
        {
            Transactions = transactionResponseDtos,
            Total = transactions.CaHolderTransaction.TotalRecordCount
        };
    }
}