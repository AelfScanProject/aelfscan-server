using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ETransferServer.Order;

[Collection(ClusterCollection.Name)]
public class OrderWithdrawTest : AElfScanServerApplicationTestBase
{
    /*private readonly IOrderWithdrawAppService _withdrawAppService;

    public OrderWithdrawTest(ITestOutputHelper output) : base(output)
    {
        _withdrawAppService = GetRequiredService<IOrderWithdrawAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockNetworkOptions());
        services.AddSingleton(MockTokenLimitGrain());
        services.AddSingleton(MockNetworkAppService());
        services.AddSingleton(MockUserWithdrawGrain());
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task GetWithdrawInfoTest()
    {
        try
        {
            var result = await _withdrawAppService.GetWithdrawInfoAsync(new GetWithdrawListRequestDto()
            {
                ChainId = "AELF",
                Network = "ETH",
                Amount = (decimal)100.002,
                Symbol = "USDT"
            });

            result.ShouldNotBeNull();
            result.WithdrawInfo.LimitCurrency.ShouldBe("USDT");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetWithdrawInfo_Network_Not_Exists_Test()
    {
        try
        {
            var result = await _withdrawAppService.GetWithdrawInfoAsync(new GetWithdrawListRequestDto()
            {
                ChainId = "AELF",
                Network = "GETH",
                Symbol = "USDT"
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task CreateWithdrawOrderInfoTest()
    {
        try
        {
            var result = await _withdrawAppService.CreateWithdrawOrderInfoAsync(new GetWithdrawOrderRequestDto
            {
                FromChainId = "AELF",
                Network = "ETH",
                Amount = (decimal)100.002,
                Symbol = "USDT",
                ToAddress = "test",
                RawTransaction = "test"
            });

            result.ShouldNotBeNull();
            result.OrderId.ShouldBe(Guid.Empty.ToString());
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    private IOptionsSnapshot<NetworkOptions> MockNetworkOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<NetworkOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new NetworkOptions
            {
                NetworkMap = new Dictionary<string, List<NetworkConfig>>()
                {
                    ["USDT"] = new List<NetworkConfig>()
                    {
                        new NetworkConfig()
                        {
                            NetworkInfo = new NetworkInfo()
                            {
                                Network = "ETH"
                            },
                            DepositInfo = new DepositInfo()
                            {
                                MinDeposit = "1",
                                ExtraNotes = new List<string>() { "test" }
                            },
                            WithdrawInfo = new WithdrawInfo()
                            {
                                MinWithdraw = "1",
                                Decimals = 6,
                                WithdrawFee = 10,
                                WithdrawLimit24h = "10000"
                            }
                        }
                    }
                }
            });
        return mockOptionsSnapshot.Object;
    }

    [Fact]
    public async Task AddOrUpdateTest()
    {
        var result = await _withdrawAppService.AddOrUpdateAsync(new WithdrawOrderDto()
        {
            Id = Guid.NewGuid()
        });
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AddOrUpdate_Fail_Test()
    {
        // try
        // {
        //     var result = await _withdrawAppService.AddOrUpdateAsync(null);
        //     result.ShouldBeFalse();
        // }
        // catch (Exception e)
        // {
        //     e.ShouldNotBeNull();
        // }
    }

    private ITokenWithdrawLimitGrain MockTokenLimitGrain()
    {
        var tokenLimit = new Mock<ITokenWithdrawLimitGrain>();

        tokenLimit
            .Setup(x => x.GetLimit())
            .ReturnsAsync(new TokenLimitGrainDto()
            {
                RemainingLimit = 100000
            });

        return tokenLimit.Object;
    }

    private IUserWithdrawGrain MockUserWithdrawGrain()
    {
        var withdraw = new Mock<IUserWithdrawGrain>();

        withdraw
            .Setup(x => x.CreateOrder(It.IsAny<WithdrawOrderDto>()))
            .ReturnsAsync(new WithdrawOrderDto()
            {
                Id = Guid.Empty
            });

        return withdraw.Object;
    }

    private INetworkAppService MockNetworkAppService()
    {
        var network = new Mock<INetworkAppService>();

        network.Setup(t => t.GetNetworkListAsync(It.IsAny<GetNetworkListRequestDto>(), null)).ReturnsAsync(
            new GetNetworkListDto()
            {
                NetworkList=new List<NetworkDto>()
                {
                    new NetworkDto()
                    {
                        Network = "ETH"
                    }
                }
            });

        return network.Object;
    }*/
    public OrderWithdrawTest(ITestOutputHelper output) : base(output)
    {
    }
}