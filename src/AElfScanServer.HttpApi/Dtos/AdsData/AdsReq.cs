using System;
using System.Collections.Generic;
using AElfScanServer.Common.Dtos.Ads;

namespace AElfScanServer.HttpApi.Dtos.AdsData;

public class AdsReq
{
    public string Label { get; set; }
    public string SearchKey { get; set; } = "";
}

public class AdsBannerReq
{
    public string Label { get; set; }
    public string SearchKey { get; set; } = "";
}

public class UpdateAdsReq
{
    public string AdsId { get; set; } = "";
    public string Head { get; set; }
    public string Logo { get; set; }
    public string AdsText { get; set; }
    public string ClickText { get; set; }
    public string ClickLink { get; set; }
    public List<string> Labels { get; set; }
    public DateTime Createtime { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public int TotalVisitCount { get; set; }
}

public class UpdateAdsBannerReq
{
    public string AdsBannerId { get; set; } = "";
    public string Text { get; set; } = "";
    public string ClickLink { get; set; } = "";
    public string Image { get; set; }
    public string MobileImage { get; set; }
    public List<string> Labels { get; set; }
    public DateTime Createtime { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public int TotalVisitCount { get; set; }
}

public class AdsBannerListResp
{
    public int Total { get; set; }
    public List<AdsBannerIndex> List { get; set; }
}

public class AdsListResp
{
    public int Total { get; set; }
    public List<AdsIndex> List { get; set; }
}

public class DeleteAdsReq
{
    public string AdsId { get; set; }
}

public class DeleteAdsBannerReq
{
    public string AdsBannerId { get; set; }
}

public class GetAdsBannerListReq
{
    public string AdsBannerId { get; set; } = "";
    public List<string> Labels { get; set; } = new List<string>();
}

public class GetAdsListReq
{
    public string AdsId { get; set; } = "";
    public List<string> Labels { get; set; } = new List<string>();
}