using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Service;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer;

public class AddressServiceTest  : AElfScanServerApplicationTestBase
{
    private readonly IAddressService _addressService;
    public AddressServiceTest(ITestOutputHelper output) : base(output)
    {
        _addressService = GetRequiredService<IAddressService>();
    }

    [Fact]
    public async Task GetAddressDetailAsync_Test()
    {
        var result = await _addressService.GetAddressDictionaryAsync(new AElfAddressInput
        {
            ChainId = "AELF",
            Name = "Test",
            IsManager = true,
            IsProducer = true,
            Addresses = new List<string>
            {
                "From"
            }

        });
        result.Count.ShouldBe(0);
    }
}