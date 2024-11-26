/*using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Service;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer;

public class AddressAppServiceTest : AElfScanServerApplicationTestBase
{
    private readonly IAddressAppService _addressAppService;
    private readonly IObjectMapper _objectMapper;

    public AddressAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _addressAppService = GetRequiredService<IAddressAppService>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }

    [Fact]
    public async Task GetAddressDetailAsync_Test()
    {
        var result = await _addressAppService.GetAddressDetailAsync(new GetAddressDetailInput
        {
            Address = "0x0000000000000000000000000000000000000000",
            ChainId = "MainChain"
        });
        result.ChainIds.ShouldContain("AELF");
    }
    
    public async Task GetAddressListAsync_Test()
    {
        var result = await _addressAppService.GetAddressListAsync(new GetListInputInput()
        {
            ChainId = "AELF"
        });
        result.List.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task GetAddressTokenListAsync_Test()
    {
        var result = await _addressAppService.GetAddressTokenListAsync(new GetAddressTokenListInput()
        {
            ChainId = "AELF"
        });
        result.List.Count.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public async Task GetAddressNftListAsync_Test()
    {
        var result = await _addressAppService.GetAddressNftListAsync(new GetAddressTokenListInput()
        {
            ChainId = "AELF"
        });
        result.List.Count.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public async Task GetTransferListAsync_Test()
    {
        var result = await _addressAppService.GetTransferListAsync(new GetTransferListInput()
        {
            ChainId = "AELF"
        });
        result.List.Count.ShouldBeGreaterThan(0);
    }
}*/