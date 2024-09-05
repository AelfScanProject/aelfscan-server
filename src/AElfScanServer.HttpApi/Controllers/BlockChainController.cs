using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Common.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Controllers;

[AggregateExecutionTime]
[RemoteService]
[ControllerName("Block")]
[Route("api/app/blockchain")]
public class BlockChainController : AbpController
{
    private readonly IHomePageService _homePageService;
    private readonly IBlockChainService _blockChainService;
    private readonly IAddressService _addressService;
    private readonly ISearchService _searchService;
    private readonly IDynamicTransactionService _dynamicTransactionService;


    public BlockChainController(IHomePageService homePageService,
        IAddressService addressService,
        IBlockChainService blockChainService, ISearchService searchService,
        IDynamicTransactionService dynamicTransactionService)
    {
        _homePageService = homePageService;
        _blockChainService = blockChainService;
        _searchService = searchService;
        _addressService = addressService;
        _dynamicTransactionService = dynamicTransactionService;
    }

    [HttpGet]
    [Route("latestBlocks")]
    public async Task<BlocksResponseDto> GetLatestBlocksAsync(LatestBlocksRequestDto requestDto)
    {
        return await _blockChainService.GetBlocksAsync(new BlocksRequestDto()
        {
            ChainId = requestDto.ChainId,
            MaxResultCount = requestDto.MaxResultCount
        });
    }


    [HttpGet]
    [Route("blockDetail")]
    public async Task<BlockDetailResponseDto> GetBlockDetailAsync(BlockDetailRequestDto requestDto)
        => await _blockChainService.GetBlockDetailAsync(requestDto);


    [HttpGet]
    [Route("blocks")]
    public async Task<BlocksResponseDto> GetBlocksAsync(BlocksRequestDto requestDto)
        => await _blockChainService.GetBlocksAsync(requestDto);


    [HttpGet]
    [Route("latestTransactions")]
    public virtual async Task<TransactionsResponseDto> GetLatestTransactionsAsync(
        LatestTransactionsReq req)
    {
        return await _blockChainService.GetTransactionsAsync(new TransactionsRequestDto()
            { ChainId = req.ChainId, SkipCount = 0, MaxResultCount = 6 });
    }

    [HttpGet]
    [Route("transactions")]
    public virtual async Task<TransactionsResponseDto> GetTransactionsAsync(
        TransactionsRequestDto requestDto) => await _dynamicTransactionService.GetTransactionsAsync(requestDto);


    [HttpPost]
    [Route("logEvents")]
    public virtual async Task<LogEventResponseDto> GetLogEventsAsync(
        GetLogEventRequestDto requestDto) => await _blockChainService.GetLogEventsAsync(requestDto);


    [HttpGet]
    [Route("filters")]
    public virtual async Task<object> GetFilterTypeAsync() => await _homePageService.GetFilterType();

    [HttpGet]
    [Route("search")]
    public virtual async Task<object> SearchAsync(SearchRequestDto requestDto) =>
        await _searchService.SearchAsync(requestDto);

    [HttpPost]
    [Route("blockchainOverview")]
    public virtual async Task<HomeOverviewResponseDto> BlockchainOverviewAsync(
        BlockchainOverviewRequestDto req) => await _homePageService.GetBlockchainOverviewAsync(req);

    [HttpPost]
    [Route("transactionDataChart")]
    public virtual async Task<TransactionPerMinuteResponseDto> GetTransactionPerMinuteAsync(
        GetTransactionPerMinuteRequestDto requestDto) =>
        await _homePageService.GetTransactionPerMinuteAsync(requestDto.ChainId);


    [HttpGet]
    [Route("transactionDetail")]
    public virtual async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(
        TransactionDetailRequestDto requestDto) =>
        await _dynamicTransactionService.GetTransactionDetailAsync(requestDto);


    [HttpPost]
    [Route("addressDictionary")]
    public async Task<Dictionary<string, CommonAddressDto>> GetAddressDictionaryAsync(
        AElfAddressInput requestDto) => await _addressService.GetAddressDictionaryAsync(requestDto);
}