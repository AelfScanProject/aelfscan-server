using System.Collections.Generic;

namespace AElfScanServer.Common.BlockChain.Options;

public class AELFIndexerOptions
{
    public string GetTokenHost { get; set; }

    public string AELFIndexerHost { get; set; }
    

    public string ClientId { get; set; }

    public int AccessTokenExpireDurationSeconds { get; set; }

    public int TransactionRateKeyExpireDurationSeconds { get; set; }

    public string ClientSecret { get; set; }


    public List<string> ChainIds { get; set; }

    public long PullHeightInterval { get; set; }

    public string RetryCount { get; set; }
}