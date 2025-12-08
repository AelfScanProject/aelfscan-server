using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.Common.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Controllers;

[AggregateExecutionTime]
[RemoteService]
[Area("app")]
[ControllerName("Address")]
[Route("api/app/address")]
public class AddressController : AbpController
{
    private readonly IAddressAppService _addressAppService;
    private readonly IContractAppService _contractAppService;

    public AddressController(IAddressAppService addressAppService, IContractAppService contractAppService)
    {
        _addressAppService = addressAppService;
        _contractAppService = contractAppService;
    }

    // address common
    [HttpGet("detail")]
    public async Task<GetAddressDetailResultDto> GetAddressDetailAsync(GetAddressDetailInput input)
        => await _addressAppService.GetAddressDetailAsync(input);

    [HttpGet("tokens")]
    public async Task<GetAddressTokenListResultDto> GetAddressTokenListAsync(
        GetAddressTokenListInput input) => await _addressAppService.GetAddressTokenListAsync(input);

    [HttpGet("nft-assets")]
    public async Task<GetAddressNftListResultDto> GetNftAssetsAsync(
        GetAddressTokenListInput input) => await _addressAppService.GetAddressNftListAsync(input);

    [HttpGet("transfers")]
    public async Task<GetTransferListResultDto> GetTransferListAsync(GetTransferListInput input)
        => await _addressAppService.GetTransferListAsync(input);


    // account
    [HttpGet("accounts")]
    public async Task<GetAddressListResultDto> GetAddressListAsync(GetListInputInput input)
        => await _addressAppService.GetAddressListAsync(input);

    // contract
    [HttpGet("contracts")]
    public async Task<GetContractListResultDto> GetContractListAsync(GetContractContracts input)
        => await _contractAppService.GetContractListAsync(input);

    [HttpGet("contract/file")]
    public async Task<ContractFileResultDto> GetContractFileAsync(
        GetContractFileInput input) => await _contractAppService.GetContractFileAsync(input);
    

    [HttpGet("contract/events")]
    public async Task<GetContractEventResp> GetContractEventsAsync(
        GetContractEventReq input) => await _contractAppService.GetContractEventsAsync(input);


    [HttpGet("contract/history")]
    public async Task<GetContractHistoryResultDto> GetContractHistoryAsync(
        GetContractHistoryInput input) => await _contractAppService.GetContractHistoryAsync(input);
}