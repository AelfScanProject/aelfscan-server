using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public interface ITwitterProvider
{
    public  Task<List<Tweet>> GetLatestTwittersAsync(string bearerToken, string userId, string sinceTweetId);
}


public class TwitterProvider : ITwitterProvider, ISingletonDependency
{
    private readonly ILogger<ITwitterProvider> _logger;

    public TwitterProvider(ILogger<ITwitterProvider> logger)
    {
        _logger = logger;
    }

    public async Task<List<Tweet>> GetLatestTwittersAsync(string bearerToken, string userId, string sinceTweetId)
    {

        string url = $"https://api.twitter.com/2/users/{userId}/tweets"; 
        int maxResults = 5; 
        _logger.LogDebug($"bearerToken = {bearerToken}, userId = {userId},sinceTweetId = {sinceTweetId}");

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var requestUrl = $"{url}?max_results={maxResults}&since_id={sinceTweetId}";
            var response = await client.GetAsync(requestUrl);
          
            if (response.Headers.Contains("x-rate-limit-limit"))
            {
                var rateLimit = response.Headers.GetValues("x-rate-limit-limit");
                var rateLimitRemaining = response.Headers.GetValues("x-rate-limit-remaining");
                var rateLimitReset = response.Headers.GetValues("x-rate-limit-reset");

                _logger.LogInformation($"rateLimit = {rateLimit.First()}, rateLimitRemaining = {rateLimitRemaining.First()},rateLimitReset = {rateLimitReset.First()}");
                if (rateLimitRemaining.First() == "0")
                {
                    var resetTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(rateLimitReset.First()));
                    var waitTime = resetTime - DateTimeOffset.UtcNow;
                    _logger.LogInformation($"waitTime {waitTime.TotalSeconds} ");
                }
            }

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("responseBody: " + responseBody);
                var responseData = JsonConvert.DeserializeObject<TwitterResponseDto>(responseBody);
                return responseData.Tweets;
            }
          
            string errorResponse = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"response failed，code: {response.StatusCode}, body: {errorResponse}");
            return null;
        }
    }

}