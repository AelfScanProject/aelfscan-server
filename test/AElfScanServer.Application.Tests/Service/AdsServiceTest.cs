using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos.AdsData;
using AElfScanServer.HttpApi.Service;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer;

public class AdsServiceTest : AElfScanServerApplicationTestBase
{
    private readonly IAdsService _adsService;

    public AdsServiceTest(ITestOutputHelper output) : base(output)
    {
        _adsService = GetRequiredService<IAdsService>();
    }

    [Fact]
    public async Task GetAds_Test()
    {
        await UpdateAds_Test();
        await Task.Delay(2000);
        // Arrange
        var req = new AdsReq
        {
            Label = "Sample",
            SearchKey = "SearchKey"
        };

        // Act
        var result = await _adsService.GetAds(req);

        // Assert
        result.ShouldNotBeNull(); 
    }

    [Fact]
    public async Task GetAdsDetailList_Test()
    {
       await UpdateAds_Test();
       await Task.Delay(2000);
        // Arrange
        int size = 1;

        // Act
        var result = await _adsService.GetAdsDetailList(size);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(size);
    }

    [Fact]
    public async Task UpdateAds_Test()
    {
        // Arrange
        var req = CreateUpdateAdsReq();

        // Act
        var result = await _adsService.UpdateAds(req);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAds_Test()
    {
        // Arrange
        var req = new DeleteAdsReq { /* 初始化参数 */ };

        // Act
        var result = await _adsService.DeleteAds(req);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAdsList_Test()
    {
        await UpdateAds_Test();
        // Arrange
        var req = new GetAdsListReq
        {
            AdsId = CreateUpdateAdsReq().AdsId,
            Labels = CreateUpdateAdsReq().Labels
        };

        // Act
        var result = await _adsService.GetAdsList(req);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAdsBanner_Test()
    {
        // Arrange
        var req = new AdsBannerReq {  };

        // Act
        var result = await _adsService.GetAdsBanner(req);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateAdsBanner_Test()
    {
        // Arrange
        var req = new UpdateAdsBannerReq {  };

        // Act
        var result = await _adsService.UpdateAdsBanner(req);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAdsBanner_Test()
    {
        // Arrange
        var req = new DeleteAdsBannerReq {  };

        // Act
        var result = await _adsService.DeleteAdsBanner(req);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAdsBannerList_Test()
    {
        // Arrange
        var req = new GetAdsBannerListReq { /* 初始化参数 */ };

        // Act
        var result = await _adsService.GetAdsBannerList(req);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetLatestTwitterListAsync_Test()
    {
        // Arrange
        int maxResultCount = 5;

        // Act
        var result = await _adsService.GetLatestTwitterListAsync(maxResultCount);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(maxResultCount);
    }

    [Fact]
    public async Task SaveTwitterListAsync_Test()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () => await _adsService.SaveTwitterListAsync());
    }
    
    public static UpdateAdsReq CreateUpdateAdsReq()
    {
        return new UpdateAdsReq
        {
            AdsId = "sample-ads-id",
            Head = "Sample Headline",
            Logo = "http://example.com/logo.png",
            AdsText = "This is a sample advertisement text.",
            ClickText = "Click Here",
            ClickLink = "http://example.com",
            Labels = new List<string> { "Sample", "Ads" },
            Createtime = DateTime.UtcNow,
            StartTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds(),
            EndTime = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds(),
            TotalVisitCount = 0
        };
    }
}