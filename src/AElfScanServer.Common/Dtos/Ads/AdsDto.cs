using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElfScanServer.Domain.Common.Entities;
using AElfScanServer.Domain.Shared.Common;
using Nest;
using Orleans;

namespace AElfScanServer.Common.Dtos.Ads;

public class AdsResp
{
    public string AdsId { get; set; }
    public string Head { get; set; }
    public string Logo { get; set; }
    public string AdsText { get; set; }
    public string ClickText { get; set; }
    public string ClickLink { get; set; }
    public string SearchKey { get; set; }
}

public class AdsBannerResp
{
    public string AdsBannerId { get; set; }
    public string Image { get; set; }
    public string ClickLink { get; set; }
    public string Text { get; set; }
    public string MobileImage { get; set; }
    public string SearchKey { get; set; }
}

[GenerateSerializer]
public class AdsDto
{
    [Id(0)] public AdsInfoDto CurAds { get; set; }
    [Id(1)] public Dictionary<string, AdsVisitFinishedRecordDto> Records { get; set; }
}

[GenerateSerializer]
public class AdsVisitFinishedRecordDto
{
    [Id(0)] public string AdsId { get; set; }
    [Id(1)] public long FinishedTime { get; set; }
    [Id(2)] public int VisitCount { get; set; }
    [Id(3)] public int TotalVisitCount { get; set; }
}

[GenerateSerializer]
public class AdsInfoDto
{
    [Id(0)] public string AdsId { get; set; }
    [Id(1)] public string Head { get; set; }
    [Id(2)] public string Logo { get; set; }
    [Id(3)] public string AdsText { get; set; }
    [Id(4)] public string ClickText { get; set; }
    [Id(5)] public string ClickLink { get; set; }
    [Id(6)] public List<string> Labels { get; set; }
    [Id(7)] public DateTime CreateTime { get; set; }
    [Id(8)] public long StartTime { get; set; }
    [Id(9)] public long EndTime { get; set; }
    [Id(10)] public int VisitCount { get; set; }
    [Id(11)] public int TotalVisitCount { get; set; }
}

public class AdsIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword] public string AdsId { get; set; }
    [Keyword] public string Head { get; set; }
    [Keyword] public string Logo { get; set; }
    [Keyword] public string AdsText { get; set; }
    [Keyword] public string ClickText { get; set; }
    [Keyword] public string ClickLink { get; set; }

    public List<string> Labels { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }

    public int TotalVisitCount { get; set; }
}

public class AdsBannerIndex : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword] public string AdsBannerId { get; set; }
    [Keyword] public string Image { get; set; }

    public List<string> Labels { get; set; }

    public string Text { get; set; }

    public string ClickLink { get; set; }
    
    public string MobileImage { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }

    public int TotalVisitCount { get; set; }
}