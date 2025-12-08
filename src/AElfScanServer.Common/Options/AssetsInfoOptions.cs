using System;

namespace AElfScanServer.Common.Options;

public class AssetsInfoOptions
{
    public string ImageUrlPrefix { get; set; }
    
    public string ImageUrlSuffix { get; set; }
    
    public bool IsEmpty()
    {
        return ImageUrlPrefix.IsNullOrWhiteSpace() || ImageUrlSuffix.IsNullOrWhiteSpace();
    }

    public string BuildImageUrl(string symbol)
    {
        return $"{ImageUrlPrefix}{symbol}{ImageUrlSuffix}";
    }
}