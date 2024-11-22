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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Portkey.backend;
using Portkey.Dtos;
using Portkey.Options;
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
    private readonly BlockChainDataProvider _blockChainProvider;
    private IDistributedCache<TransactionDetailResponseDto> _detailCache;
    private IOptionsMonitor<AddressOptions> _addressOptions;

    // private readonly string CaAddress = "238X6iw1j8YKcHvkDYVtYVbuYk2gJnK8UoNpVCtssynSpVC8hb";
    // private readonly string CaAddress = "2UthYi7AHRdfrqc1YCfeQnjdChDLaas65bW4WxESMGMojFiXj9";

    public PortkeyService(ILogger<PortkeyService> logger,
        IObjectMapper objectMapper,
        IPortkeyTransactionProvider portkeyTransactionProvider,
        IBlockChainIndexerProvider blockChainIndexerProvider,
        DynamicTransactionService dynamicTransactionService,
        BlockChainDataProvider blockChainProvider,
        IDistributedCache<TransactionDetailResponseDto> detailCache,
        IOptionsMonitor<AddressOptions> addressOptions
    )
    {
        _logger = logger;
        _portkeyTransactionProvider = portkeyTransactionProvider;
        _objectMapper = objectMapper;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _dynamicTransactionService = dynamicTransactionService;
        _blockChainProvider = blockChainProvider;
        _detailCache = detailCache;
        _addressOptions = addressOptions;
    }

    public async Task<AElfScanServer.HttpApi.Dtos.TransactionsResponseDto> GetTransactionsAsync(
        TransactionsRequestDto requestDto)
    {
        if (requestDto.Address.IsNullOrEmpty())
        {
            return _dynamicTransactionService.GetTransactionsAsync(requestDto).Result;
        }

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

    public async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(TransactionDetailRequestDto request)
    {
        var detailResponseDto = await _detailCache.GetAsync(request.TransactionId + request.ChainId);
        if (detailResponseDto != null)
        {
            return detailResponseDto;
        }


        _logger.LogInformation("Start plugin transaction detail{txnId}", request.TransactionId);
        var transactionDetailAsync = await _dynamicTransactionService.GetTransactionDetailAsync(request);
        if (transactionDetailAsync.List.IsNullOrEmpty())
        {
            _logger.LogInformation("GetTransactionDetailAsync  base transaction not found:{chainId},{transactionId}",
                request.ChainId, request.TransactionId);
            return transactionDetailAsync;
        }

        _logger.LogInformation("Find  transaction detail{txnId}", request.TransactionId);
        var detail = transactionDetailAsync.List.First();

        var transactionDetail =
            await _blockChainProvider.GetTransactionDetailAsync(request.ChainId, request.TransactionId);

        _logger.LogInformation("Find  transaction result from node {txnId},to address:{address},caAddress:{caAddress}",
            request.TransactionId,
            detail.To.Address, _addressOptions.CurrentValue.CaAddress);

        if (detail.To.Address == _addressOptions.CurrentValue.CaAddress)
        {
            switch (detail.Method)
            {
                case "ManagerForwardCall":
                    await ParseManagerForwardCall(request, detail, transactionDetail.Transaction.Params);
                    break;
            }

            var transactions = await _portkeyTransactionProvider.GetActivitiesAsync(new List<CAAddressInfo>(),
                request.ChainId,
                "", null, 0, 1, detail.TransactionId);
            if (transactions == null || transactions.CaHolderTransaction == null ||
                transactions.CaHolderTransaction.Data.IsNullOrEmpty())
            {
                _logger.LogError("GetTransactionDetailAsync activity transaction not found:{chainId},{transactionId}",
                    request.ChainId, request.TransactionId);
                return transactionDetailAsync;
            }

            var activity = transactions.CaHolderTransaction.Data.First();

            var fromAddress = await GetFromAddress(activity);
            if (!fromAddress.IsNullOrEmpty())
            {
                detail.From.Address = fromAddress;
            }


            if (!activity.ToContractAddress.IsNullOrEmpty())
            {
                detail.To.Address = activity.ToContractAddress;
            }

            detail.Method = activity.MethodName;
        }


        await _detailCache.SetAsync(request.TransactionId + request.ChainId, transactionDetailAsync,
            new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });

        return transactionDetailAsync;
    }

    public async Task<string> GetFromAddress(IndexerTransaction transaction)
    {
        if (transaction.TransferInfo != null && !transaction.TransferInfo.FromAddress.IsNullOrEmpty() &&
            transaction.TransferInfo.FromAddress.Length > 10)
        {
            return transaction.TransferInfo.FromAddress;
        }

        if (!transaction.FromAddress.IsNullOrEmpty())
        {
            return transaction.FromAddress;
        }


        return "";
    }

    public async Task ParseManagerForwardCall(TransactionDetailRequestDto request, TransactionDetailDto detail,
        string param)
    {
        try
        {
            var jObject = JObject.Parse(param);
            var parseParam = await _blockChainProvider.GeFormatTransactionParamAsync(request.ChainId,
                jObject["contractAddress"].ToString(),
                jObject["methodName"].ToString(), jObject["args"].ToString());

            detail.TransactionParams = parseParam;

            _logger.LogInformation("Replace  transaction  {txnId},{param}", request.TransactionId, parseParam);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Parse  ManagerForwardCall transaction error");
        }
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

        var defailAddress = "null";
        var transactionIds = new List<string>();
        foreach (var indexerTransaction in transactions.CaHolderTransaction.Data)
        {
            var tokenInfoAddress =
                indexerTransaction.TransferInfo != null ? indexerTransaction.TransferInfo.FromAddress : "null";
            var tokenInfoToAddress =
                indexerTransaction.TransferInfo != null ? indexerTransaction.TransferInfo.ToAddress : "null";
            _logger.LogInformation(
                $"txid:{indexerTransaction.TransactionId},fromAddress:{indexerTransaction.FromAddress},TransferInfo from address:{tokenInfoAddress},TransferInfo to address:{tokenInfoToAddress}"
            );
            transactionIds.Add(indexerTransaction.TransactionId);
            var txn = new TransactionResponseDto()
            {
                TransactionId = indexerTransaction.TransactionId,
                BlockHeight = indexerTransaction.BlockHeight,
                Method = indexerTransaction.MethodName,
                Timestamp = indexerTransaction.Timestamp,
                From = new CommonAddressDto() { Address = await GetFromAddress(indexerTransaction), IsManager = true },
                To = new CommonAddressDto()
                {
                    Address = indexerTransaction.ToContractAddress.IsNullOrEmpty()
                        ? _addressOptions.CurrentValue.CaAddress
                        : indexerTransaction.ToContractAddress,
                    IsManager = true
                },
                ChainIds = new List<string>() { indexerTransaction.ChainId }
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