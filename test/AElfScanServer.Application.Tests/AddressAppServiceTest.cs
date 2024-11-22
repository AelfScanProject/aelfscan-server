using System.Threading.Tasks;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Token;
using AElfScanServer.Common.Token.Provider;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Provider;
using AElfScanServer.HttpApi.Service;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer;

public class AddressAppServiceTest : AElfScanServerApplicationTestBase
{
    private readonly IAddressAppService _addressAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AddressAppService> _logger;
    private readonly IIndexerGenesisProvider _indexerGenesisProvider;
    private readonly ITokenIndexerProvider _tokenIndexerProvider;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly ITokenInfoProvider _tokenInfoProvider;

    public AddressAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _addressAppService = GetRequiredService<IAddressAppService>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _logger = GetRequiredService<ILogger<AddressAppService>>();
        _indexerGenesisProvider = GetRequiredService<IIndexerGenesisProvider>();
        _tokenIndexerProvider = GetRequiredService<ITokenIndexerProvider>();
        _tokenPriceService = GetRequiredService<ITokenPriceService>();
        _tokenInfoProvider = GetRequiredService<ITokenInfoProvider>();
    }

    [Fact]
    public async Task GetAddressDetailAsync_Test()
    {
        var result = await _addressAppService.GetAddressDetailAsync(new GetAddressDetailInput
        {
            Address = "0x0000000000000000000000000000000000000000",
            ChainId = "MainChain"
        });
    }
}