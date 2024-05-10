using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Address.HttpApi.Dtos;
using AElfScanServer.Address.HttpApi.Provider;
using AElfScanServer.Address.HttpApi.Provider.Entity;
using AElfScanServer.BlockChain;
using AElfScanServer.BlockChain.Dtos;
using AElfScanServer.Constant;
using AElfScanServer.Token;
using AElfScanServer.Token.Dtos;
using AElfScanServer.Token.Dtos.Input;
using AElfScanServer.Dtos;
using AElfScanServer.Helper;
using AElfScanServer.TokenDataFunction.Dtos.Indexer;
using AElfScanServer.TokenDataFunction.Dtos.Input;
using AElfScanServer.TokenDataFunction.Provider;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace AElfScanServer.Address.HttpApi.AppServices;

public interface IAddressAppService
{
    Task<GetAddressListResultDto> GetAddressListAsync(GetListInputInput input);
    Task<GetAddressDetailResultDto> GetAddressDetailAsync(GetAddressDetailInput input);
    Task<GetAddressTokenListResultDto> GetAddressTokenListAsync(GetAddressTokenListInput input);
    Task<GetTransferListResultDto> GetTransferListAsync(GetTransferListInput input);
    Task<GetTransactionListResultDto> GetTransactionListAsync(GetTransactionListInput input);
}

public class AddressAppService : IAddressAppService
{
    private readonly IObjectMapper _objectMapper;
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<AddressAppService> _logger;
    private readonly IBlockChainProvider _blockChainProvider;
    private readonly IIndexerTokenProvider _indexerTokenProvider;
    private readonly IIndexerGenesisProvider _indexerGenesisProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;

    public AddressAppService(IObjectMapper objectMapper, ITokenProvider tokenProvider,
        ILogger<AddressAppService> logger, IIndexerTokenProvider indexerTokenProvider,
        BlockChainProvider blockChainProvider, IIndexerGenesisProvider indexerGenesisProvider, 
        ITokenIndexerProvider tokenIndexerProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _tokenProvider = tokenProvider;
        _blockChainProvider = blockChainProvider;
        _indexerTokenProvider = indexerTokenProvider;
        _indexerGenesisProvider = indexerGenesisProvider;
        _tokenIndexerProvider = tokenIndexerProvider;
    }

    public async Task<GetAddressListResultDto> GetAddressListAsync(GetListInputInput input)
    {
        var holderInput = new TokenHolderInput
        {
            ChainId = input.ChainId, Symbol = CurrencyConstant.ElfCurrency,
            SkipCount = input.SkipCount, MaxResultCount = input.MaxResultCount
        };
        holderInput.SetDefaultSort();
        var indexerTokenHolderInfo = await _tokenIndexerProvider.GetTokenHolderInfoAsync(holderInput);
        var indexerTokenList =
            await _tokenIndexerProvider.GetTokenDetailAsync(input.ChainId, CurrencyConstant.ElfCurrency);
        var tokenInfo = indexerTokenList[0];
        var result = new GetAddressListResultDto
        {
            Total = indexerTokenHolderInfo.TotalCount,
            TotalBalance = DecimalHelper.Divide(tokenInfo.Supply, tokenInfo.Decimals)
        };
        var addressInfos = await _blockChainProvider.GetAddressDictionaryAsync(new AElfAddressInput
        {
            ChainId = input.ChainId,
            Addresses = indexerTokenHolderInfo.Items.Select(address => address.Address).ToList()
        });
        var addressList = new List<GetAddressInfoResultDto>();
        foreach (var info in indexerTokenHolderInfo.Items)
        {
            var addressResult = _objectMapper.Map<IndexerTokenHolderInfoDto, GetAddressInfoResultDto>(info);
            addressResult.Percentage = Math.Round((decimal)info.Amount / tokenInfo.Supply * 100, CommonConstant.PercentageValueDecimals);
            if (addressInfos.TryGetValue(info.Address, out var addressInfo))
            {
                addressResult.AddressType = addressInfo.AddressType;
            }
            addressList.Add(addressResult);
        }
        //add sort 
        addressList = addressList.OrderByDescending(item => item.Balance)   
            .ThenByDescending(item => item.TransactionCount)
            .ToList();
        result.List = addressList;
        return result;
    }

    public async Task<GetAddressDetailResultDto> GetAddressDetailAsync(GetAddressDetailInput input)
    {
        _logger.LogInformation("GetAddressDetailAsync");
        var result = new GetAddressDetailResultDto();

        if (input.AddressType is AddressType.ContractAddress)
        {
            var contractInfo = await _indexerGenesisProvider.GetContractAsync(input.ChainId, input.Address);
            result = _objectMapper.Map<ContractInfoDto, GetAddressDetailResultDto>(contractInfo);
            var addressInfos = await _blockChainProvider.GetAddressDictionaryAsync(new AElfAddressInput
            {
                Addresses = new List<string>(new[] { input.Address })
            });

            result.ContractName = addressInfos.TryGetValue(input.Address, out var addressInfo)
                ? addressInfo.Name
                : "ContractName";

            // todo: indexer add time sort
            var contractRecords = await _indexerGenesisProvider.GetContractRecordAsync(input.ChainId, input.Address);
            if (contractRecords.Count > 0) result.ContractTransactionHash = contractRecords[0].TransactionId;
        }

        result.ElfBalance = await _indexerTokenProvider.GetAddressElfBalanceAsync(input.ChainId, input.Address);


        var getTokenListByAddressResult = await _tokenProvider.GetTokenListByAddressAsync(new GetTokenListInput
        {
            ChainId = input.ChainId,
            SkipCount = 0,
            MaxResultCount = 1000,
            Address = input.Address
        });
        // todo change token [list][count] to [total]
        result.TokenHoldings = getTokenListByAddressResult.List.Count;
        result.ElfPriceInUsd = getTokenListByAddressResult.List.Where(t => t.Token.Symbol == "ELF")
            .Select(t => t.PriceInUsd).FirstOrDefault();
        result.TokenTotalPriceInElf = getTokenListByAddressResult.List.Sum(t => t.TotalPriceInElf);
        result.TokenTotalPriceInUsd = getTokenListByAddressResult.List.Sum(t => t.TotalPriceInUsd);

        // todo 
        result.TokenTotalPriceInUsdRate = getTokenListByAddressResult.List.Sum(t => t.UsdPercentChange);

        // transfer info
        // todo: add time sort
        var lastTransactions = await _indexerTokenProvider.GetTransferInfoListAsync(input.ChainId, input.Address);
        result.LastTransactionSend = new TransactionInfoDto
        {
            TransactionId = lastTransactions[0].TransactionId,
            BlockTime = lastTransactions[0].BlockTime
        };
        var firstTransactions = await _indexerTokenProvider.GetTransferInfoListAsync(input.ChainId, input.Address);
        result.FirstTransactionSend = new TransactionInfoDto
        {
            TransactionId = firstTransactions[0].TransactionId,
            BlockTime = firstTransactions[0].BlockTime
        };

        return result;
    }

    public async Task<GetAddressTokenListResultDto> GetAddressTokenListAsync(
        GetAddressTokenListInput input)
    {
        _logger.LogInformation("GetTokenListByAddressAsync");
        var result = new GetAddressTokenListResultDto();

        switch (input.TokenType)
        {
            case TokenType.Token:
                var getAddressTokenListInput = _objectMapper.Map<GetAddressTokenListInput, GetTokenListInput>(input);
                var getTokenListByAddressResult =
                    await _tokenProvider.GetTokenListByAddressAsync(getAddressTokenListInput);
                result.AssetInUsd = getTokenListByAddressResult.AssetInUsd;
                result.Total = getTokenListByAddressResult.List.Count;
                result.Tokens = getTokenListByAddressResult.List;
                break;
            case TokenType.Nft:
                var getAddressNftListInput = _objectMapper.Map<GetAddressTokenListInput, GetNftListInput>(input);
                var getNftListByAddressResult = await _tokenProvider.GetNftListByAddressAsync(getAddressNftListInput);
                result.Total = getNftListByAddressResult.List.Count;
                result.Nfts = getNftListByAddressResult.List;
                break;
            default:
                throw new UserFriendlyException("Unsupported token type!");
        }

        return result;
    }

    public async Task<GetTransferListResultDto> GetTransferListAsync(GetTransferListInput input)
    {
        try
        {
            _logger.LogInformation("GetTransferListByAddressAsync");
            var result = new GetTransferListResultDto();

            var getAddressTransferListInput = _objectMapper.Map<GetTransferListInput, TokenTransferInput>(input);

            switch (input.TokenType)
            {
                case TokenType.Token:
                    getAddressTransferListInput.Types = new List<SymbolType> { SymbolType.Token };
                    var getTokenTransferListResult =
                        await _tokenProvider.GetTransferListByAddressAsync(getAddressTransferListInput);
                    result.Tokens = getTokenTransferListResult.List;
                    break;
                case TokenType.Nft:
                    getAddressTransferListInput.Types = new List<SymbolType> { SymbolType.Nft };
                    var getNftTransferListResult =
                        await _tokenProvider.GetTransferListByAddressAsync(getAddressTransferListInput);
                    result.Nfts = getNftTransferListResult.List;
                    break;
                default:
                    throw new UserFriendlyException("Unsupported token type!");
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTransferListByAddressAsync failed");
            throw;
        }
    }

    public async Task<GetTransactionListResultDto> GetTransactionListAsync(GetTransactionListInput input)
        => _objectMapper.Map<TransactionsResponseDto, GetTransactionListResultDto>(
            await _blockChainProvider.GetTransactionsAsync(input.ChainId, input.Address));
}