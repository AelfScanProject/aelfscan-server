using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.OpenTelemetry.ExecutionTime;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Service;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Ads;
using AElfScanServer.HttpApi.Dtos.AdsData;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Caching;

namespace AElfScanServer.HttpApi.Controllers;

[RemoteService]
[ControllerName("Ads")]
[Route("api/app/ads")]
public class AdsController : AbpController
{
    private readonly IAdsService _adsService;


    public AdsController(IAdsService adsService)
    {
        _adsService = adsService;
    }

    [HttpGet]
    [Route("detail")]
    public async Task<AdsResp> GetAdsDetailAsync(AdsReq req)
    {
        var searchKey = HttpContext.Request.Headers["SearchKey"].FirstOrDefault();
        req.SearchKey = searchKey;
        return await _adsService.GetAds(req);
    }

    [HttpGet]
    [Route("detail/list")]
    public async Task<List<AdsResp>> GetAdsDetailListAsync(int size)
    {
        return await _adsService.GetAdsDetailList(size);
    }

    [HttpGet]
    [Route("banner/detail")]
    public async Task<AdsBannerResp> GetAdsBannerDetailAsync(AdsBannerReq req)
    {
        var searchKey = HttpContext.Request.Headers["SearchKey"].FirstOrDefault();
        req.SearchKey = searchKey;
        return await _adsService.GetAdsBanner(req);
    }


    [HttpPost]
    [Route("detail")]
    [Authorize]
    public async Task<AdsIndex> UpdateAdsDetailAsync(UpdateAdsReq req)
    {
        return await _adsService.UpdateAds(req);
    }


    [HttpPost]
    [Route("banner/detail")]
    [Authorize]
    public async Task<AdsBannerIndex> UpdateAdsBannerDetailAsync(UpdateAdsBannerReq req)
    {
        return await _adsService.UpdateAdsBanner(req);
    }


    [HttpDelete]
    [Route("detail")]
    [Authorize]
    public async Task<AdsIndex> DeleteAdsDetailAsync(DeleteAdsReq req)
    {
        return await _adsService.DeleteAds(req);
    }

    [HttpDelete]
    [Route("banner/detail")]
    [Authorize]
    public async Task<AdsBannerIndex> DeleteAdsBannerDetailAsync(DeleteAdsBannerReq req)
    {
        return await _adsService.DeleteAdsBanner(req);
    }


    [HttpGet]
    [Route("list")]
    [Authorize]
    public async Task<AdsListResp> GetAdsListAsync(GetAdsListReq req)
    {
        return await _adsService.GetAdsList(req);
    }

    [HttpGet]
    [Route("banner/list")]
    [Authorize]
    public async Task<AdsBannerListResp> GetAdsBannerListAsync(GetAdsBannerListReq req)
    {
        return await _adsService.GetAdsBannerList(req);
    }

    [HttpGet]
    [Route("LatestTwitter")]
    public async Task<List<TwitterIndex>> GetLatestTwitterListAsync(int maxResultCount)
    {
        return await _adsService.GetLatestTwitterListAsync(maxResultCount);
    }
}