
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Common.Helper;

public class TokenSymbolHelper
{
    public const char NFTSymbolSeparator = '-';
    public const string CollectionSymbolSuffix = "0";

    public static SymbolType GetSymbolType(string symbol)
    {
        var words = symbol.Split(NFTSymbolSeparator);
        if (words.Length == 1) return SymbolType.Token;
        return words[1] == CollectionSymbolSuffix ? SymbolType.Nft_Collection : SymbolType.Nft;
    }

    public static string GetCollectionSymbol(string symbol)
    {
        var words = symbol.Split(NFTSymbolSeparator);
        return words.Length == 1 || words[1] == CollectionSymbolSuffix
            ? null
            : $"{words[0]}{NFTSymbolSeparator}{CollectionSymbolSuffix}";
    }

    public static bool IsCollection(string symbol)
    {
        return symbol.EndsWith("-0");
    }
}