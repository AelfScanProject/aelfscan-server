using System.Threading.Tasks;
using AElfScanServer.HttpApi.Service;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer;

public class AddressTypeServiceTest : AElfScanServerApplicationTestBase
{
    private readonly IAddressTypeService _addressTypeService;

    public AddressTypeServiceTest(ITestOutputHelper output) : base(output)
    {
        _addressTypeService = GetRequiredService<IAddressTypeService>();
    }

    [Fact]
    public async Task GetAddressTypeList_Test()
    {
        var chainId = "AELF";
        var address = "SampleAddress";

        // Act
        var result = await _addressTypeService.GetAddressTypeList(chainId, address);

        result.ShouldNotBeNull(); 
        result.Count.ShouldBeGreaterThanOrEqualTo(0); 
    }
}