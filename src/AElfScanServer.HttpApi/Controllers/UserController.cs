using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Controllers;

// [AggregateExecutionTime]
// [RemoteService]
[Area("app")]
[ApiController]
[ControllerName("User")]
[Route("api/app/user")]
public class UserController : AbpController
{
    private readonly IUserAppService _userService;

    public UserController(IUserAppService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [Route("add")]
    [Authorize]
    public async Task<UserResp> CreateUser(UserReq input)
    {
        return await _userService.CreateUser(input);
    }


    [HttpGet]
    public async Task ResetAdminPwd()
    {
        await _userService.ResetAdminPwd();
    }
}