using System.Collections.Generic;
using System.Threading.Tasks;

using AElfScanServer.HttpApi.Options;
using AElfScanServer.Common.HttpClient;
using AElfScanServer.HttpApi.Dtos.address;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.HttpApi.Provider;

public interface IDecompilerProvider
{
    public Task<GetContractFilesResponseDto> GetFilesAsync(string base64String);
}

public class DecompilerProvider : IDecompilerProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly ILogger<DecompilerProvider> _logger;
    private readonly DecompilerOption _decompilerOptions;

    public DecompilerProvider(IHttpProvider httpProvider, IOptionsSnapshot<DecompilerOption> decompilerOptions,
        ILogger<DecompilerProvider> logger)
    {
        _logger = logger;
        _httpProvider = httpProvider;
        _decompilerOptions = decompilerOptions.Value;
    }

    public async Task<GetContractFilesResponseDto> GetFilesAsync(string base64String)
    {
        return await _httpProvider.PostAsync<GetContractFilesResponseDto>(_decompilerOptions.Url,
            RequestMediaType.Json, new Dictionary<string, string> { { "Base64String", base64String } },
            new Dictionary<string, string>());
    }
}