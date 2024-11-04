using System.IO;
using System.Net;
using System.Threading.Tasks;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Options;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElfScanServer.Common.Provider;

public interface IAwsS3Provider
{
    Task<string> UpLoadJsonFileAsync(Stream stream, string directory, string fileName);

    Task<byte[]> GetContractFileAsync(string directory, string fileName);

    Task DeleteJsonFileAsync(string directory, string fileName);
}

public class AwsS3Provider : IAwsS3Provider
{
    private readonly ILogger<AwsS3Provider> _logger;
    private readonly IOptionsMonitor<SecretOptions> _secretOptions;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private AmazonS3Client _amazonS3Client;

    public AwsS3Provider(ILogger<AwsS3Provider> logger,
        IOptionsMonitor<SecretOptions> secretOptions, IOptionsMonitor<GlobalOptions> globalOptions)
    {
        _logger = logger;
        _secretOptions = secretOptions;
        _globalOptions = globalOptions;
        InitAmazonS3Client();
    }

    private void InitAmazonS3Client()
    {
        var accessKeyID = _globalOptions.CurrentValue.S3AccessKey;
        var ServiceURL = _globalOptions.CurrentValue.S3ServiceURL;

        var config = new AmazonS3Config()
        {
            ServiceURL = ServiceURL,
            RegionEndpoint = Amazon.RegionEndpoint.APNortheast1
        };
        _amazonS3Client = new AmazonS3Client(accessKeyID, _globalOptions.CurrentValue.S3SecretKey, config);
    }

    public async Task<string> UpLoadJsonFileAsync(Stream stream, string directory, string fileName)
    {
        var s3Key = GetS3Key(directory, fileName);
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = stream,
            BucketName = _globalOptions.CurrentValue.S3ContractFileBucket,
            Key = s3Key
        };
        var putObjectResponse = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        if (putObjectResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Upload json file failed with HTTP status code: {StatusCode}",
                putObjectResponse.HttpStatusCode);
            return string.Empty;
        }

        return s3Key;
    }


    public async Task<byte[]> GetContractFileAsync(string directory, string fileName)
    {
        var s3Key = GetS3Key(directory, fileName);
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = _globalOptions.CurrentValue.S3ContractFileBucket,
            Key = s3Key
        };

        using (var response = await _amazonS3Client.GetObjectAsync(getObjectRequest))
        using (var memoryStream = new MemoryStream())
        {
            await response.ResponseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    public async Task DeleteJsonFileAsync(string directory, string fileName)
    {
        var s3Key = GetS3Key(directory, fileName);
        var request = new DeleteObjectRequest
        {
            BucketName = _globalOptions.CurrentValue.S3ContractFileBucket,
            Key = s3Key
        };

        await _amazonS3Client.DeleteObjectAsync(request);
    }

    private string GetS3Key(string directory, string fileName)
    {
        return $"{directory}/{fileName}";
    }
}