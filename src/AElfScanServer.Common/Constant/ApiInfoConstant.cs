using System.Net.Http;
using AElfScanServer.Common.HttpClient;

namespace AElfScanServer.Common.Constant;

public class ApiInfoConstant
{
    public static string ForestServer = "ForestServer";
    public static ApiInfo NftCollectionFloorPrice = new(HttpMethod.Post, "api/app/open/search-collections-floor-price");
}