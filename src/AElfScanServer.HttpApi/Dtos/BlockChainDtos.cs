using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;
using Google.Protobuf;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.Application.Dtos;

namespace AElfScanServer.HttpApi.Dtos;

public class LatestTransactionsReq
{
    public string ChainId { get; set; }
    public int MaxResultCount { get; set; }
}

public class MergeBlockInfoReq
{
    public string ChainId { get; set; }
    public int MaxResultCount { get; set; }
}

public class TransactionsByHashRequestDto
{
    public List<string> Hashs { get; set; }
    public long SkipCount { get; set; }
    public long MaxResultCount { get; set; } = 10;
}

public class TransactionsRequestDto : BaseInput
{
    public string TransactionId { get; set; } = "";
    public int BlockHeight { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public string Address { get; set; } = "";

    public void SetDefaultSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockHeight, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }


    public void SetFirstTransactionSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockHeight, SortDirection.Asc), (SortField.TransactionId, SortDirection.Asc));
    }

    public void SetLastTransactionSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockHeight, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }
}

public class BpDataRequestDto
{
    public string ChainId { get; set; }
}

public class CommonRequest
{
    public string ChainId { get; set; }
}

public class BlockchainOverviewRequestDto
{
    public string ChainId { get; set; }
}

public class HomeOverviewResponseDto
{
    public decimal TokenPriceInUsd { get; set; }
    public decimal TokenPriceRate24h { get; set; }
    public long Transactions { get; set; }
    public string Tps { get; set; }

    public DateTime TpsTime { get; set; }
    public string Reward { get; set; }
    public long BlockHeight { get; set; }
    public int Accounts { get; set; }
    public string CitizenWelfare { get; set; }
}

public class BlocksRequestDto : PagedResultRequestDto
{
    public string ChainId { get; set; }

    public bool IsLastPage { get; set; }
}

public class LogEventRequestDto : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string ContractName { get; set; }
}

public class WebSocketMergeBlockInfoDto
{
    public TransactionsResponseDto LatestTransactions { get; set; }
    public BlocksResponseDto LatestBlocks { get; set; }
}

public class TransactionsResponseDto
{
    public long Total { get; set; }
    public List<TransactionResponseDto> Transactions { get; set; } = new List<TransactionResponseDto>();
}

public class SearchRequestDto : IValidatableObject
{
    public string ChainId { get; set; }
    [Required] public string Keyword { get; set; }
    public FilterTypes FilterType { get; set; }
    public SearchTypes SearchType { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(FilterTypes), FilterType) || !Enum.IsDefined(typeof(SearchTypes), SearchType))
        {
            yield return new ValidationResult(
                "Invalid Request"
            );
        }
    }
}

public class TransactionDetailRequestDto
{
    public long BlockHeight { get; set; }
    public string TransactionId { get; set; }
    public string ChainId { get; set; }
}

public class GetTransactionPerMinuteRequestDto
{
    public string ChainId { get; set; }
}

public class TransactionDetailResponseDto
{
    public List<TransactionDetailDto> List { get; set; } = new List<TransactionDetailDto>();
}

public class TransactionDetailDto
{
    public string TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public long BlockHeight { get; set; }
    public long BlockConfirmations { get; set; }
    public long Timestamp { get; set; }
    public string Method { get; set; }


    public CommonAddressDto From { get; set; }

    public CommonAddressDto To { get; set; }
    public List<TokenTransferredDto> TokenTransferreds { get; set; } = new List<TokenTransferredDto>();
    public List<NftsTransferredDto> NftsTransferreds { get; set; } = new List<NftsTransferredDto>();

    public List<ValueInfoDto> TransactionValues { get; set; }

    public List<ValueInfoDto> TransactionFees { get; set; }

    public string ResourcesFee { get; set; }

    public List<ValueInfoDto> BurntFees { get; set; } = new List<ValueInfoDto>();

    public string TransactionRefBlockNumber { get; set; }

    public string TransactionRefBlockPrefix { get; set; }

    public string TransactionParams { get; set; }

    public string ReturnValue { get; set; }

    public string TransactionSignature { get; set; }

    public bool Confirmed { get; set; }

    public string Version { get; set; }

    public string Bloom { get; set; }
    public string Error { get; set; }

    public string TransactionSize { get; set; }

    public string ResourceFee { get; set; }

    public List<LogEventInfoDto> LogEvents { get; set; } = new List<LogEventInfoDto>();

    public List<IMessage> ParseLogEvents { get; set; } = new();

    public void AddParseLogEvents(IMessage message)
    {
        if (message != null)
        {
            ParseLogEvents.Add(message);
        }
    }
}

public class LogEventInfoDto
{
    public CommonAddressDto ContractInfo { get; set; }


    public string EventName { get; set; }

    public string Indexed { get; set; }

    public string NonIndexed { get; set; }
}

public class ValueInfoDto
{
    public string Symbol { get; set; }
    public long Amount { get; set; }

    public string AmountString { get; set; }
    public string NowPrice { get; set; }
    public string TradePrice { get; set; }
}

public class TokenTransferredDto
{
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
    public string Symbol { get; set; }
    public string Name { get; set; }

    public long Amount { get; set; }

    public string AmountString { get; set; }

    public string TradePrice { get; set; }
    public string NowPrice { get; set; }
    public string ImageUrl { get; set; }
    public string ImageBase64 { get; set; }
}

public class NftsTransferredDto
{
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
    public string Symbol { get; set; }

    public string Name { get; set; }
    public long Amount { get; set; }
    public string AmountString { get; set; }
    public string TradePrice { get; set; }
    public string NowPrice { get; set; }
    public string ImageUrl { get; set; }
    public string ImageBase64 { get; set; }
    public bool IsCollection { get; set; }
}

public class SearchResponseDto
{
    public List<SearchToken> Tokens { get; set; } = new();
    public List<SearchToken> Nfts { get; set; } = new();
    public List<string> Accounts { get; set; } = new();
    public List<SearchContract> Contracts { get; set; } = new();
    public SearchBlock Block { get; set; }
    public SearchTransaction Transaction { get; set; }
}

public class SearchBlock
{
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
}

public class SearchTransaction
{
    public string TransactionId { get; set; }
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
}

public class SearchToken
{
    public string Image { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public SymbolType Type { get; set; }
}

public class SearchContract
{
    public string Name { get; set; }
    public string Address { get; set; }
}

public class LatestBlocksRequestDto
{
    public string ChainId { get; set; }
    public int MaxResultCount { get; set; } = 6;
}

public class LatestTransactionsResponseSto
{
    public long Total { get; set; }
    public List<TransactionResponseDto> Transactions { get; set; }
}

public class BlocksResponseDto
{
    public List<BlockResponseDto> Blocks { get; set; }
    public long Total { get; set; }
}

public class BlockDetailRequestDto
{
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
}

public class BlockDetailResponseDto
{
    public long BlockHeight { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long Timestamp { get; set; }
    public bool Confirmed { get; set; }
    public RewardDto Reward { get; set; }

    public string PreviousBlockHash { get; set; }

    public long PreBlockHeight { get; set; }

    public long NextBlockHeight { get; set; }
    public string BlockSize { get; set; }
    public string MerkleTreeRootOfTransactions { get; set; }
    public string MerkleTreeRootOfWorldState { get; set; }
    public string MerkleTreeRootOfTransactionState { get; set; }
    public string Extra { get; set; }
    public Producer Producer { get; set; }
    public BurntFee BurntFee { get; set; }
    public long Total { get; set; }
    public List<TransactionResponseDto> Transactions { get; set; }
}

public class Producer
{
    public string address { get; set; }
    public string name { get; set; }
}

public class BurntFee
{
    public string UsdFee { get; set; }
    public string ElfFee { get; set; }
}

public class RewardDto
{
    public string UsdReward { get; set; }
    public string ElfReward { get; set; }
}

public class BlockResponseDto
{
    public long BlockHeight { get; set; }
    public long Timestamp { get; set; }
    public int TransactionCount { get; set; }
    public string TimeSpan { get; set; }
    public string Reward { get; set; }
    public string BurntFees { get; set; }

    public string ProducerName { get; set; }

    public string ProducerAddress { get; set; }
}

public class BlockProduceInfoDto
{
    public List<BlockProduceDto> List { get; set; }
}

public class BlockProduceDto
{
    public int BlockCount { get; set; }
    public int MissedCount { get; set; }
    public string ProducerName { get; set; }
    public bool IsMinning { get; set; }

    public int Order { get; set; }

    public string ProducerAddress { get; set; }
}

public class FilterTypeResponseDto
{
    public List<FilterTypeDto> FilterTypes { get; set; }
}

public enum FilterTypes
{
    AllFilter,
    Tokens,
    Accounts,
    Contracts,
    Nfts
}

public enum SearchTypes
{
    FuzzySearch,
    ExactSearch
}

public class FilterTypeDto
{
    public int FilterType { get; set; }
    public string FilterInfo { get; set; }
}

public class TransactionResponseDto
{
    public string TransactionId { get; set; }

    public long BlockHeight { get; set; }

    public string Method { get; set; }

    public TransactionStatus Status { get; set; }
    public CommonAddressDto From { get; set; }

    public CommonAddressDto To { get; set; }

    public long Timestamp { get; set; }

    public string TransactionValue { get; set; }

    public string TransactionFee { get; set; }
}

public class TransactionPerMinuteResponseDto
{
    public List<TransactionCountPerMinuteDto> All { get; set; }
    public List<TransactionCountPerMinuteDto> Owner { get; set; }
}

public class TransactionCountPerMinuteDto
{
    public long Start { get; set; }

    public long End { get; set; }
    public long Count { get; set; }
}

public enum OrderField
{
    BlockHeight,
    Timestamp
}

// public class GetLogEventRequestDto : PagedResultRequestDto
// {
//     public string ChainId { get; set; }
//     public string ContractName { get; set; }
//     public long BlockHeight { get; set; }
//     public int Index { get; set; }
//     public SortOrder SortOrder { get; set; }
// }

public class GetLogEventRequestDto : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string ContractAddress { get; set; }
    public SortOrder SortOrder { get; set; }
}

public class LogEventResponseDto
{
    public long Total { get; set; }
    public List<LogEventIndex> LogEvents { get; set; }
}