namespace AElfScanServer.Common.Dtos;

public class TokenBaseInfoDto
{
    public string Id { get; set; }

    public string ChainId { get; set; }

    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public string PreviousBlockHash { get; set; }

    public string Symbol { get; set; }

    /// <summary>
    ///     token contract address
    /// </summary>
    public string TokenContractAddress { get; set; }

    public int Decimals { get; set; }

    public long TotalSupply { get; set; }

    public string TokenName { get; set; }

    public string Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }
}