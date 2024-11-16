namespace AElfScanServer.HttpApi.Options;

public class SecretOptions
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    public string CMCApiKey { get; set; }

    public string S3AccessKey { get; set; }

    public string S3SecretKey { get; set; }
    
    public string TwitterBearToken { get; set; }
}