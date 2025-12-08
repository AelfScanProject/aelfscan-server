namespace AElfScanServer.Common.Dtos;

public class BlockSizeDto
{
    public int BlockSize { get; set; }

    public bool PullFalse { get; set; }

    public Header Header { get; set; }
}

public class Header
{
    public string PreviousBlockHash { get; set; }
    public string MerkleTreeRootOfTransactions { get; set; }
    public string MerkleTreeRootOfWorldState { get; set; }
    public string MerkleTreeRootOfTransactionState { get; set; }

    public string Height { get; set; }
    public string Time { get; set; }
}