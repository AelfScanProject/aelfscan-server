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
    private readonly ISearchService _searchService;
    private readonly IDynamicTransactionService _dynamicTransactionService;


    public BlockChainController(IHomePageService homePageService,
        IBlockChainService blockChainService, ISearchService searchService,
        IDynamicTransactionService dynamicTransactionService)
    {
        _homePageService = homePageService;
        _blockChainService = blockChainService;
        _searchService = searchService;
        _dynamicTransactionService = dynamicTransactionService;
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

    [HttpGet]
    [Route("filters")]
    public virtual async Task<object> GetFilterTypeAsync() => await _homePageService.GetFilterType();

    [HttpGet]
    [Route("search")]
    public virtual async Task<object> SearchAsync(SearchRequestDto requestDto) =>
        await _searchService.SearchAsync(requestDto);


    [HttpGet]
    [Route("transactionDetail")]
    public virtual async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(
        TransactionDetailRequestDto requestDto) =>
        await _dynamicTransactionService.GetTransactionDetailAsync(requestDto);
}