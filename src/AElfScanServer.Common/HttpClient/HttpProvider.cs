using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AElfScanServer.Common.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.HttpClient;

public interface IHttpProvider : ISingletonDependency
{
    Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
        object pathParams = null,
        object param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true);

    Task<T> PostAsync<T>(string url, RequestMediaType requestMediaType, object paramObj,
        Dictionary<string, string> headers = null);

    Task<string> PostAsync(string url, RequestMediaType requestMediaType, object paramObj,
        Dictionary<string, string> headers = null);

    // Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
    //     Dictionary<string, string> pathParams = null,
    //     Dictionary<string, string> param = null,
    //     string body = null,
    //     Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
    //     bool withInfoLog = false, bool withDebugLog = true);
    //
    Task<T> InvokeAsync<T>(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true);

    public Task<T> PostInternalServerAsync<T>(string url, object paramObj);
}

public class HttpProvider : IHttpProvider
{
    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    private const int DefaultTimeout = 10000;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpProvider> _logger;

    public HttpProvider(IHttpClientFactory httpClientFactory, ILogger<HttpProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var resp = await InvokeAsync(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header, timeout,
            withInfoLog, withDebugLog);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error deserializing service [{apiInfo.Path}] response body: {resp}", ex);
        }
    }

    public async Task<T> InvokeAsync<T>(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var resp = await InvokeAsync(method, url, pathParams, param, body, header, timeout, withInfoLog, withDebugLog);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error deserializing service [{url}] response body: {resp}", ex);
        }
    }

    public async Task<T> PostAsync<T>(string url, RequestMediaType requestMediaType, object paramObj,
        Dictionary<string, string> headers)
    {
        if (requestMediaType == RequestMediaType.Json)
        {
            return await PostJsonAsync<T>(url, paramObj, headers);
        }

        return await PostFormAsync<T>(url, (Dictionary<string, string>)paramObj, headers);
    }


    public async Task<string> PostAsync(string url, RequestMediaType requestMediaType, object paramObj,
        Dictionary<string, string> headers)
    {
        return await PostJsonAsync(url, paramObj, headers);
    }

    public async Task<T> PostInternalServerAsync<T>(string url, object paramObj)
    {
        var result = await PostJsonAsync<CommonResponseDto<T>>(url, paramObj, null);
        if (result.Code != "20000")
        {
            throw new UserFriendlyException($"Token service post request failed, message:{result.Message}.");
        }

        return result.Data;
    }


    public async Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
        object pathParams = null,
        object param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true)
    {
        var resp = await InvokeAsync(apiInfo.Method, domain + apiInfo.Path, ObjectToDictionary(pathParams),
            ObjectToDictionary(param), body, header,
            withInfoLog, withDebugLog);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error deserializing service [{apiInfo.Path}] response body: {resp}", ex);
        }
    }

    private async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> paramDic,
        Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient();

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var param = new List<KeyValuePair<string, string>>();
        if (paramDic is { Count: > 0 })
        {
            param.AddRange(paramDic.ToList());
        }

        var response = await client.PostAsync(url, new FormUrlEncodedContent(param));
        var content = await response.Content.ReadAsStringAsync();

        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramDic));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }


    private async Task<T> PostJsonAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient();

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (ResponseSuccess(response.StatusCode)) return JsonConvert.DeserializeObject<T>(content);

        _logger.LogError(
            "Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
            url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

        throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
    }

    private async Task<string> PostJsonAsync(string url, object paramObj, Dictionary<string, string> headers)
    {
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient();

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (ResponseSuccess(response.StatusCode)) return content;

        _logger.LogError(
            "Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
            url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

        throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
    }


    private async Task<string> InvokeAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var response =
            await InvokeResponseAsync(method, url, pathParams, param, body, header, withInfoLog, withDebugLog);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Server [{url}] returned status code {response.StatusCode} : {content}", null, response.StatusCode);
        }

        return content;
    }

    private async Task<HttpResponseMessage> InvokeResponseAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        bool withLog = false, bool debugLog = true)
    {
        // url params
        var fullUrl = PathParamUrl(url, pathParams);

        var builder = new UriBuilder(fullUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (var item in param ?? new Dictionary<string, string>())
            query[item.Key] = item.Value;
        builder.Query = query.ToString() ?? string.Empty;

        var request = new HttpRequestMessage(method, builder.ToString());

        // headers
        foreach (var h in header ?? new Dictionary<string, string>())
            request.Headers.Add(h.Key, h.Value);

        // body
        if (body != null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders
            .Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await client.SendAsync(request);


        return response;
    }

    private bool ResponseSuccess(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;

    private static string PathParamUrl(string url, Dictionary<string, string> pathParams)
    {
        return pathParams.IsNullOrEmpty()
            ? url
            : pathParams.Aggregate(url, (current, param) => current.Replace($"{{{param.Key}}}", param.Value));
    }

    private static Dictionary<string, string> ObjectToDictionary(object obj)
    {
        if (obj == null) return new Dictionary<string, string>();

        if (obj.GetType() == typeof(Dictionary<string, string>)) return (Dictionary<string, string>)obj;

        var dict = new Dictionary<string, string>();

        foreach (var property in obj.GetType().GetProperties())
        {
            var value = property.GetValue(obj);

            if (value != null)
            {
                dict.Add(property.Name, value.ToString());
            }
        }

        return dict;
    }

    public async Task<string> InvokeAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        int? timeout = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var response = await InvokeResponseAsync(method, url, pathParams, param, body, header, timeout, withInfoLog,
            withDebugLog);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Server [{url}] returned status code {response.StatusCode} : {content}", null, response.StatusCode);
        }

        return content;
    }

    public async Task<HttpResponseMessage> InvokeResponseAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        int? timeout = null,
        bool withLog = false, bool debugLog = true)
    {
        // url params
        var fullUrl = PathParamUrl(url, pathParams);

        var builder = new UriBuilder(fullUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (var item in param ?? new Dictionary<string, string>())
            query[item.Key] = item.Value;
        builder.Query = query.ToString() ?? string.Empty;

        var request = new HttpRequestMessage(method, builder.ToString());

        // headers
        foreach (var h in header ?? new Dictionary<string, string>())
            request.Headers.Add(h.Key, h.Value);

        // body
        if (body != null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // send
        var stopwatch = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(timeout ?? DefaultTimeout);
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var time = stopwatch.ElapsedMilliseconds;
        // log
        if (withLog)
            _logger.LogInformation(
                "Request To {FullUrl}, statusCode={StatusCode}, time={Time}, query={Query}, body={Body}, resp={Content}",
                fullUrl, response.StatusCode, time, builder.Query, body, content);
        else if (debugLog)
            _logger.LogDebug(
                "Request To {FullUrl}, statusCode={StatusCode}, time={Time}, query={Query}, header={Header}, body={Body}, resp={Content}",
                fullUrl, response.StatusCode, time, builder.Query, request.Headers.ToString(), body, content);
        return response;
    }
}

public enum RequestMediaType
{
    Json,
    Form
}