using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Helper;
using AElfScanServer.HttpApi.Options;
using AElfScanServer.HttpApi.Provider;
using Elasticsearch.Net;
using AElfScanServer.Common.Helper;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.ExceptionHandler;
using AElf.OpenTelemetry;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.ExceptionHandling;
using Castle.Components.DictionaryAdapter.Xml;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.DataStrategy;
using AElfScanServer.HttpApi.DataStrategy;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using StackExchange.Redis;
using Volo.Abp.Caching.StackExchangeRedis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Volo.Abp;
using Volo.Abp.Caching;

namespace AElfScanServer.HttpApi.Service;

public interface IDynamicTransactionService
{
    public Task<TransactionsResponseDto> GetTransactionsAsync(TransactionsRequestDto requestD);

    public Task<TransactionDetailResponseDto> GetTransactionDetailAsync(TransactionDetailRequestDto request);
}

[AggregateExecutionTime]
public class DynamicTransactionService : IDynamicTransactionService
{
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly AELFIndexerProvider _aelfIndexerProvider;
    private readonly IBlockChainIndexerProvider _blockChainIndexerProvider;
    private readonly BlockChainDataProvider _blockChainProvider;
    private readonly LogEventProvider _logEventProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptionsMonitor;
    private readonly DataStrategyContext<string, HomeOverviewResponseDto> _overviewDataStrategy;
    private IDistributedCache<TransactionDetailResponseDto> _transactionDetailCache;
    private readonly ILogger<HomePageService> _logger;


    public DynamicTransactionService(
        ILogger<HomePageService> logger, IOptionsMonitor<GlobalOptions> blockChainOptions,
        AELFIndexerProvider aelfIndexerProvider,
        LogEventProvider logEventProvider,
        BlockChainDataProvider blockChainProvider, IBlockChainIndexerProvider blockChainIndexerProvider,
        ITokenIndexerProvider tokenIndexerProvider, IOptionsMonitor<TokenInfoOptions> tokenInfoOptions,
        OverviewDataStrategy overviewDataStrategy,
        IDistributedCache<TransactionDetailResponseDto> transactionDetailCache, ITokenInfoProvider tokenInfoProvider,ITokenPriceService tokenPriceService)
    {
        _logger = logger;
        _globalOptions = blockChainOptions;
        _aelfIndexerProvider = aelfIndexerProvider;
        _logEventProvider = logEventProvider;
        _blockChainProvider = blockChainProvider;
        _blockChainIndexerProvider = blockChainIndexerProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
        _tokenInfoOptionsMonitor = tokenInfoOptions;
        _overviewDataStrategy = new DataStrategyContext<string, HomeOverviewResponseDto>(overviewDataStrategy);
        _transactionDetailCache = transactionDetailCache;
        _tokenInfoProvider = tokenInfoProvider;
        _tokenPriceService = tokenPriceService;
    }


    
    [ExceptionHandler(typeof(Exception),
        Message = "GetTransactionDetailAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["request"])]
    public virtual async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(TransactionDetailRequestDto request)
    {
        var transactionDetailResponseDto = new TransactionDetailResponseDto();
        if (!_globalOptions.CurrentValue.ChainIds.Exists(s => s == request.ChainId))
        {
            return transactionDetailResponseDto;
        }


        var detailResponseDto = await _transactionDetailCache.GetAsync(request.TransactionId);
        if (detailResponseDto != null)
        {
            return detailResponseDto;
        }

            var blockHeight = 0l;
            NodeTransactionDto transactionDto = new NodeTransactionDto();
            var tasks = new List<Task>();
            tasks.Add(_overviewDataStrategy.DisplayData(request.ChainId).ContinueWith(task =>
            {
                if (task.Result != null)
                {
                    blockHeight = task.Result.BlockHeight;

                }
            }));


            tasks.Add(_blockChainProvider.GetTransactionDetailAsync(request.ChainId,
                request.TransactionId).ContinueWith(task => { transactionDto = task.Result; }));

            await tasks.WhenAll();
            var transactionIndex = _aelfIndexerProvider.GetTransactionsAsync(request.ChainId,
                transactionDto.BlockNumber,
                transactionDto.BlockNumber, request.TransactionId).Result.First();


            var detailDto = new TransactionDetailDto();
            detailDto.TransactionId = transactionIndex.TransactionId;
            detailDto.Status = transactionIndex.Status;
            detailDto.BlockConfirmations = detailDto.Status == TransactionStatus.Mined ? blockHeight : 0;
            detailDto.BlockHeight = transactionIndex.BlockHeight;
            detailDto.Timestamp = DateTimeHelper.GetTotalSeconds(transactionIndex.BlockTime);
            detailDto.Method = transactionIndex.MethodName;
            detailDto.TransactionParams = transactionDto.Transaction.Params;
            detailDto.TransactionSignature = transactionIndex.Signature;
            detailDto.Confirmed = transactionIndex.Confirmed;
            detailDto.From = ConvertAddress(transactionIndex.From, transactionIndex.ChainId);
            detailDto.To = ConvertAddress(transactionIndex.To, transactionIndex.ChainId);


            await AnalysisExtraPropertiesAsync(detailDto, transactionIndex);
            await AnalysisTransferredAsync(detailDto, transactionIndex);
            await AnalysisLogEventAsync(detailDto, transactionIndex);
            
            var result = new TransactionDetailResponseDto()
            {
                List = new List<TransactionDetailDto>() { detailDto }
            };
            await _transactionDetailCache.SetAsync(request.TransactionId, result, new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });
            return result;
            
    }


    public async Task AnalysisLogEventAsync(TransactionDetailDto detailDto, TransactionIndex transactionIndex)
    {
        foreach (var transactionDtoLogEvent in transactionIndex.LogEvents)
        {
            transactionDtoLogEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
            transactionDtoLogEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);
            var logEventInfoDto = new LogEventInfoDto()
            {
                Indexed = indexed,
                NonIndexed = nonIndexed,
                EventName = transactionDtoLogEvent.EventName,
                ContractInfo = ConvertAddress(transactionDtoLogEvent.ContractAddress, transactionIndex.ChainId)
            };
            detailDto.LogEvents.Add(logEventInfoDto);
            //add parse log event logic
            if (!indexed.IsNullOrEmpty() &&
                (_globalOptions.CurrentValue.ParseLogEvent(detailDto.From.Address, detailDto.Method)
                 || _globalOptions.CurrentValue.ParseLogEvent(detailDto.To.Address, detailDto.Method)))
            {
                var message = ParseMessage(transactionDtoLogEvent.EventName, ByteString.FromBase64(indexed));
                detailDto.AddParseLogEvents(message);
            }
        }
    }

    public async Task AnalysisExtraPropertiesAsync(TransactionDetailDto detailDto, TransactionIndex transactionIndex)
    {
        if (!transactionIndex.ExtraProperties.IsNullOrEmpty())
        {
            if (transactionIndex.ExtraProperties.TryGetValue("Version", out var version))
            {
                detailDto.Version = version;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("RefBlockNumber", out var refBlockNumber))
            {
                detailDto.TransactionRefBlockNumber = refBlockNumber;
            }

            if (transactionIndex.ExtraProperties.TryGetValue("RefBlockPrefix", out var refBlockPrefix))
            {
                detailDto.TransactionRefBlockPrefix = refBlockPrefix;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("Bloom", out var bloom))
            {
                detailDto.Bloom = bloom;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("ReturnValue", out var returnValue))
            {
                detailDto.ReturnValue = returnValue;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("Error", out var error))
            {
                detailDto.Error = error;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("TransactionSize", out var transactionSize))
            {
                detailDto.TransactionSize = transactionSize;
            }


            if (transactionIndex.ExtraProperties.TryGetValue("ResourceFee", out var resourceFee))
            {
                detailDto.ResourceFee = resourceFee;
            }
        }
    }


    public async Task AnalysisTransferredAsync(TransactionDetailDto detailDto,
        TransactionIndex transactionIndex)
    {
        var transactionValues = new Dictionary<string, ValueInfoDto>();

        var transactionFees = new Dictionary<string, ValueInfoDto>();

        var burntFees = new Dictionary<string, ValueInfoDto>();

        foreach (var txnLogEvent in transactionIndex.LogEvents)
        {
            txnLogEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
            txnLogEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);

            var indexedList = indexed != null
                ? JsonConvert.DeserializeObject<List<string>>(indexed)
                : new List<string>();
            var logEvent = new LogEvent
            {
                Indexed = { indexedList?.Select(ByteString.FromBase64) },
            };

            if (nonIndexed != null)
            {
                logEvent.NonIndexed = ByteString.FromBase64(nonIndexed);
            }


            switch (txnLogEvent.EventName)
            {
                case nameof(Transferred):
                    var transferred = new Transferred();
                    transferred.MergeFrom(logEvent);
                    await SetValueInfoAsync(transactionValues, transferred.Symbol, transferred.Amount);

                    if (TokenSymbolHelper.GetSymbolType(transferred.Symbol) == SymbolType.Token)
                    {
                        _globalOptions.CurrentValue.TokenImageUrls.TryGetValue(transferred.Symbol, out var imageUrl);
                        var token = new TokenTransferredDto()
                        {
                            Symbol = transferred.Symbol,
                            Name = transferred.Symbol,
                            Amount = transferred.Amount,
                            AmountString =
                                await _blockChainProvider.GetDecimalAmountAsync(transferred.Symbol, transferred.Amount,
                                    transactionIndex.ChainId),
                            From = ConvertAddress(transferred.From.ToBase58(), transactionIndex.ChainId),
                            To = ConvertAddress(transferred.To.ToBase58(), transactionIndex.ChainId),
                            ImageUrl = await _tokenIndexerProvider.GetTokenImageAsync(transferred.Symbol,
                                txnLogEvent.ChainId),
                            NowPrice = await _blockChainProvider.TransformTokenToUsdValueAsync(transferred.Symbol,
                                transferred.Amount,txnLogEvent.ChainId) 
                            
                        };


                        detailDto.TokenTransferreds.Add(token);
                    }
                    else
                    {
                        var nft = new NftsTransferredDto()
                        {
                            Symbol = transferred.Symbol,
                            Amount = transferred.Amount,
                            AmountString = await _blockChainProvider.GetDecimalAmountAsync(transferred.Symbol,
                                transferred.Amount, transactionIndex.ChainId),
                            Name = transferred.Symbol,
                            From = ConvertAddress(transferred.From.ToBase58(), transactionIndex.ChainId),
                            To = ConvertAddress(transferred.To.ToBase58(), transactionIndex.ChainId),
                            IsCollection = TokenSymbolHelper.IsCollection(transferred.Symbol),
                            ImageUrl = await _tokenIndexerProvider.GetTokenImageAsync(transferred.Symbol,
                                txnLogEvent.ChainId),
                        };


                        detailDto.NftsTransferreds.Add(nft);
                    }

                    break;
                case nameof(TransactionFeeCharged):
                    var transactionFeeCharged = new TransactionFeeCharged();
                    transactionFeeCharged.MergeFrom(logEvent);
                    await SetValueInfoAsync(transactionFees, transactionFeeCharged.Symbol,
                        transactionFeeCharged.Amount);
                    await SetValueInfoAsync(transactionValues, transactionFeeCharged.Symbol,
                        transactionFeeCharged.Amount);

                    var address = "";
                    if (transactionFeeCharged.ChargingAddress == null ||
                        transactionFeeCharged.ChargingAddress.ToBase58().IsNullOrEmpty())
                    {
                        address = transactionIndex.From;
                    }
                    else
                    {
                        address = transactionFeeCharged.ChargingAddress.ToBase58();
                    }

                    await HandleTransferData(transactionFeeCharged.Symbol, address,
                        "", transactionFeeCharged.Amount,
                        transactionIndex.ChainId, detailDto);

                    break;

                case nameof(CrossChainReceived):
                    var crossChainReceived = new CrossChainReceived();
                    crossChainReceived.MergeFrom(logEvent);
                    await HandleTransferData(crossChainReceived.Symbol, crossChainReceived.From.ToBase58(),
                        crossChainReceived.To.ToBase58(), crossChainReceived.Amount,
                        transactionIndex.ChainId, detailDto);
                    await SetValueInfoAsync(transactionValues, crossChainReceived.Symbol, crossChainReceived.Amount);

                    break;
                case nameof(CrossChainTransferred):
                    var crossChainTransferred = new CrossChainTransferred();
                    crossChainTransferred.MergeFrom(logEvent);

                    await HandleTransferData(crossChainTransferred.Symbol, crossChainTransferred.From.ToBase58(),
                        crossChainTransferred.To.ToBase58(), crossChainTransferred.Amount,
                        transactionIndex.ChainId, detailDto);
                    await SetValueInfoAsync(transactionValues, crossChainTransferred.Symbol,
                        crossChainTransferred.Amount);

                    break;
                case nameof(Issued):
                    var issued = new Issued();
                    issued.MergeFrom(logEvent);
                    await HandleTransferData(issued.Symbol, "",
                        issued.To.ToBase58(), issued.Amount,
                        transactionIndex.ChainId, detailDto);
                    await SetValueInfoAsync(transactionValues, issued.Symbol,
                        issued.Amount);

                    break;
                case nameof(Burned):
                    var burned = new Burned();
                    burned.MergeFrom(logEvent);
                    await SetValueInfoAsync(burntFees, burned.Symbol, burned.Amount);
                    await SetValueInfoAsync(transactionValues, burned.Symbol, burned.Amount);
                    await HandleTransferData(burned.Symbol, burned.Burner.ToBase58(), ""
                        , burned.Amount,
                        transactionIndex.ChainId, detailDto);
                    
                  

                    break;
            }
        }


        foreach (var valueInfoDto in transactionValues)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount,transactionIndex.ChainId);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }

        foreach (var valueInfoDto in transactionFees)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount,transactionIndex.ChainId);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }


        foreach (var valueInfoDto in burntFees)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount,transactionIndex.ChainId);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }


        detailDto.TransactionFees = transactionFees.Values.OrderByDescending(x => x.Amount).ToList();

        detailDto.TransactionValues = transactionValues.Values.OrderByDescending(x => x.Amount).ToList();
        detailDto.BurntFees = burntFees.Values.OrderByDescending(x => x.Amount).ToList();
    }


    public async Task HandleTransferData(string symbol, string from, string to, long amount, string chainId,
        TransactionDetailDto detailDto)
    {
        if (TokenSymbolHelper.GetSymbolType(symbol) == SymbolType.Token)
        {
            _globalOptions.CurrentValue.TokenImageUrls.TryGetValue(symbol, out var imageUrl);
            var token = new TokenTransferredDto()
            {
                Symbol = symbol,
                Name = symbol,
                Amount = amount,
                AmountString =
                    await _blockChainProvider.GetDecimalAmountAsync(symbol, amount,
                        chainId),
                From = ConvertAddress(from, chainId),
                To = ConvertAddress(to, chainId),
                ImageUrl = await _tokenIndexerProvider.GetTokenImageAsync(symbol,
                    chainId),
                NowPrice = await _blockChainProvider.TransformTokenToUsdValueAsync(symbol,
                    amount,chainId)
            };


            detailDto.TokenTransferreds.Add(token);
        }
        else
        {
            var nft = new NftsTransferredDto()
            {
                Symbol = symbol,
                Amount = amount,
                AmountString = await _blockChainProvider.GetDecimalAmountAsync(symbol,
                    amount, chainId),
                Name = symbol,
                From = ConvertAddress(from, chainId),
                To = ConvertAddress(to, chainId),
                IsCollection = TokenSymbolHelper.IsCollection(symbol),
                ImageUrl = await _tokenIndexerProvider.GetTokenImageAsync(symbol,
                    chainId),
            };


            detailDto.NftsTransferreds.Add(nft);
        }
    }

    public async Task SetValueInfoAsync(Dictionary<string, ValueInfoDto> dic, string symbol, long amount)
    {
        if (dic.TryGetValue(symbol, out var value))
        {
            value.Amount += amount;
        }
        else
        {
            dic.Add(symbol, new ValueInfoDto()
            {
                Amount = amount,
                Symbol = symbol
            });
        }
    }

    
    [ExceptionHandler(typeof(Exception),
        Message = "GetTransactionsAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException), ReturnDefault = ReturnDefault.New,LogTargets = ["requestDto"])]
    public virtual async Task<TransactionsResponseDto> GetTransactionsAsync(TransactionsRequestDto requestDto)
    {
        var result = new TransactionsResponseDto();
        result.Transactions = new List<TransactionResponseDto>();

      
            requestDto.SetDefaultSort();
            var indexerTransactionList = await _blockChainIndexerProvider.GetTransactionsAsync(requestDto);


            foreach (var transactionIndex in indexerTransactionList.Items)
            {
                var transactionRespDto = new TransactionResponseDto()
                {
                    TransactionId = transactionIndex.TransactionId,
                    Timestamp = DateTimeHelper.GetTotalSeconds(transactionIndex.Metadata.Block.BlockTime),
                    TransactionValue = transactionIndex.TransactionValue.ToString(),
                    BlockHeight = transactionIndex.BlockHeight,
                    Method = transactionIndex.MethodName,
                    Status = transactionIndex.Status,
                    TransactionFee = transactionIndex.Fee.ToString(),
                    BlockTime = transactionIndex.Metadata.Block.BlockTime,
                    ChainIds = new List<string>() { transactionIndex.Metadata.ChainId }
                };


                transactionRespDto.From = ConvertAddress(transactionIndex.From, transactionIndex.Metadata.ChainId);

                transactionRespDto.To = ConvertAddress(transactionIndex.To, transactionIndex.Metadata.ChainId);
                result.Transactions.Add(transactionRespDto);
            }


            if (!requestDto.OrderInfos.IsNullOrEmpty() && requestDto.OrderInfos.Count > 1)
            {

                var primarySortOrder = requestDto.OrderInfos[0].Sort.ToLower() == "desc"
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
                var secondarySortOrder = requestDto.OrderInfos[1].Sort.ToLower() == "desc"
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
                result.Transactions = Sort(result.Transactions, requestDto.OrderInfos[0].OrderBy, primarySortOrder,
                    requestDto.OrderInfos[1].OrderBy, secondarySortOrder);

            }

            // result.Transactions = result.Transactions.OrderByDescending(item => item.BlockTime)
            //     .ThenByDescending(item => item.TransactionId)
            //     .ToList();

            result.Total = indexerTransactionList.TotalCount;

        return result;
    }
    
    public List<TransactionResponseDto> Sort(List<TransactionResponseDto> transactions, string primarySortBy, SortOrder primarySortOrder, string secondarySortBy, SortOrder secondarySortOrder)
    {
        var sortedTransactions = SortBy(transactions, primarySortBy, primarySortOrder);
        sortedTransactions = SortBy(sortedTransactions, secondarySortBy, secondarySortOrder, true);
        return sortedTransactions.ToList();
    }
    
    private IOrderedEnumerable<TransactionResponseDto> SortBy(IEnumerable<TransactionResponseDto> transactions, string sortBy, SortOrder sortOrder, bool isSecondary = false)
    {
        PropertyInfo propInfo = typeof(TransactionResponseDto).GetProperty(sortBy);
        if (propInfo == null)
            throw new ArgumentException($"Invalid sort field: {sortBy}");
        Func<TransactionResponseDto, object> keySelector = item => propInfo.GetValue(item);
        if (!isSecondary)
        {
            return sortOrder == SortOrder.Ascending
                ? transactions.OrderBy(keySelector)
                : transactions.OrderByDescending(keySelector);
        }
        else
        {
            var orderedTransactions = transactions as IOrderedEnumerable<TransactionResponseDto>;
            return sortOrder == SortOrder.Ascending
                ? orderedTransactions.ThenBy(keySelector)
                : orderedTransactions.ThenByDescending(keySelector);
        }
    }

    public IMessage? ParseMessage(string eventName, ByteString byteString)
    {
        IMessage? message = eventName switch
        {
            "Transferred" => Transferred.Parser.ParseFrom(byteString),
            "CrossChainTransferred" => CrossChainTransferred.Parser.ParseFrom(byteString),
            "CrossChainReceived" => CrossChainReceived.Parser.ParseFrom(byteString),
            _ => null
        };
        return message;
    }

    private CommonAddressDto ConvertAddress(string address, string chainId)
    {
        var commonAddressDto = new CommonAddressDto()
        {
            AddressType = AddressType.EoaAddress,
            Address = address
        };
        if (_globalOptions.CurrentValue.ContractNames.TryGetValue(chainId, out var contractNames))
        {
            if (contractNames.TryGetValue(address, out var contractName))
            {
                commonAddressDto.Name = contractName;
                commonAddressDto.AddressType = AddressType.ContractAddress;
            }
        }


        return commonAddressDto;
    }
}