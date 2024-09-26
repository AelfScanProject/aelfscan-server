using System;

namespace AElfScanServer.Common.Constant;

public static class CommonConstant
{
    public const long LongError = -1;
    public const string Comma = ",";
    public const string Underline = "_";
    public const int UsdValueDecimals = 4;
    public const int UsdPriceValueDecimals = 8;
    public const int ElfValueDecimals = 8;
    public const int LargerPercentageValueDecimals = 8;
    public const int PercentageValueDecimals = 4;
    public const string DefaultMarket = "Forest";
    public const int DefaultMaxResultCount = 1000;
    public const string SearchKeyPattern = "[^a-zA-Z0-9-_.]";

    public const int KeyWordAddressMinSize = 9;
    public const int KeyWordContractNameMinSize = 2;

    public const string MainChainId = "AELF";


    public static DateTime AELFOneBlockTime = new DateTime(2020, 12, 15, 21, 04, 20);
    public static DateTime TDVVOneBlockTime = new DateTime(2020, 12, 10, 15, 23, 24);
}

public class CurrencyConstant
{
    public const string UsdCurrency = "USDT";
    public const string ElfCurrency = "ELF";
}