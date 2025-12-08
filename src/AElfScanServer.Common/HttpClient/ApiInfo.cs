using System.Net.Http;

namespace AElfScanServer.Common.HttpClient;

public class ApiInfo
{
    public string Path { get; set; }
    public HttpMethod Method { get; set; }

    public ApiInfo(HttpMethod method, string path)
    {
        Path = path;
        Method = method;
    }
}