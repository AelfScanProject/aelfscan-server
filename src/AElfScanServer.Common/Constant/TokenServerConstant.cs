namespace AElfScanServer.Common.Constant;

public class TokenServerConstant
{
    private const string TokenServerBaseUri = "api/app/token/";

    public const string TokenInfo = TokenServerBaseUri + "info";
    public const string TokenList = TokenServerBaseUri + "list";
    public const string TokenListByAddress = TokenServerBaseUri + "addressTokenList";
    public const string TokenPrice = TokenServerBaseUri + "price";
    public const string TokenTransfersByAddress = TokenServerBaseUri + "transfers";

    // nft uri
    public const string NftListByAddress = TokenServerBaseUri + "nft/list";
    public const string NftTransfersByAddress = TokenServerBaseUri + "nft/transfers";
}