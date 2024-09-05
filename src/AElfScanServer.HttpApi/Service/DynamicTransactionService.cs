using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using AElf.OpenTelemetry;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos.Indexer;
using AElfScanServer.Common.Core;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Enums;
using Castle.Components.DictionaryAdapter.Xml;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.DataStrategy;
using AElfScanServer.HttpApi.DataStrategy;
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
        IDistributedCache<TransactionDetailResponseDto> transactionDetailCache, ITokenInfoProvider tokenInfoProvider)
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
    }


    public async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(TransactionDetailRequestDto request)
    {
        var transactionDetailResponseDto = new TransactionDetailResponseDto();
        if (!_globalOptions.CurrentValue.ChainIds.Exists(s => s == request.ChainId))
        {
            return transactionDetailResponseDto;
        }

        try
        {
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
                blockHeight = task.Result.BlockHeight;
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
            await _transactionDetailCache.SetAsync(request.TransactionId, result);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTransactionDetailAsync error ");
        }


        return transactionDetailResponseDto;
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
                                transferred.Amount)
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
                        };
                        if (_tokenInfoOptionsMonitor.CurrentValue.TokenInfos.TryGetValue(
                                TokenSymbolHelper.GetCollectionSymbol(transferred.Symbol), out var info))
                        {
                            nft.ImageUrl = info.ImageUrl;
                        }

                        detailDto.NftsTransferreds.Add(nft);
                    }

                    break;
                case nameof(TransactionFeeCharged):
                    var transactionFeeCharged = new TransactionFeeCharged();
                    transactionFeeCharged.MergeFrom(logEvent);
                    await SetValueInfoAsync(transactionFees, transactionFeeCharged.Symbol,
                        transactionFeeCharged.Amount);

                    break;
                case nameof(Burned):
                    var burned = new Burned();
                    burned.MergeFrom(logEvent);
                    if (TokenSymbolHelper.GetSymbolType(burned.Symbol) == SymbolType.Nft &&
                        "SEED-0".Equals(TokenSymbolHelper.GetCollectionSymbol(burned.Symbol)))
                    {
                        break;
                    }

                    if (_globalOptions.CurrentValue.BurntFeeContractAddresses.TryGetValue(transactionIndex.ChainId,
                            out var addressList)
                        && addressList.Contains(burned.Burner.ToBase58()))
                    {
                        await SetValueInfoAsync(burntFees, burned.Symbol, burned.Amount);
                    }

                    break;
            }
        }


        foreach (var valueInfoDto in transactionValues)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }

        foreach (var valueInfoDto in transactionFees)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }


        foreach (var valueInfoDto in burntFees)
        {
            var valueSymbol = valueInfoDto.Value.Symbol;
            var valueAmount = valueInfoDto.Value.Amount;
            valueInfoDto.Value.NowPrice =
                await _blockChainProvider.TransformTokenToUsdValueAsync(valueSymbol,
                    valueAmount);
            valueInfoDto.Value.AmountString =
                await _blockChainProvider.GetDecimalAmountAsync(valueSymbol, valueAmount, transactionIndex.ChainId);
        }


        detailDto.TransactionFees = transactionFees.Values.OrderByDescending(x => x.Amount).ToList();

        detailDto.TransactionValues = transactionValues.Values.OrderByDescending(x => x.Amount).ToList();
        detailDto.BurntFees = burntFees.Values.OrderByDescending(x => x.Amount).ToList();
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

    public async Task<TransactionsResponseDto> GetTransactionsAsync(TransactionsRequestDto requestDto)
    {
        var result = new TransactionsResponseDto();
        result.Transactions = new List<TransactionResponseDto>();

        try
        {
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
                };


                transactionRespDto.From = ConvertAddress(transactionIndex.From, requestDto.ChainId);

                transactionRespDto.To = ConvertAddress(transactionIndex.To, requestDto.ChainId);
                result.Transactions.Add(transactionRespDto);
            }

            result.Transactions = result.Transactions.OrderByDescending(item => item.BlockHeight)
                .ThenByDescending(item => item.TransactionId)
                .ToList();

            result.Total = indexerTransactionList.TotalCount;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetLatestTransactionsAsync error");
            return result;
        }

        return result;
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