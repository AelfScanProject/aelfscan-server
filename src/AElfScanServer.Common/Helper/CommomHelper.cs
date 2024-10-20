using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfScanServer.Common.Dtos;
using Google.Protobuf;
using Nethereum.Util;
using Newtonsoft.Json;
using Serilog;

namespace AElfScanServer.Common.Helper;

public class CommomHelper
{
    public static long TimeToReduceMiningRewardByHalf = 126144000; // 60 * 60 * 24 * 365 * 4
    public const long InitialMiningRewardPerBlock = 12500000;
 

    public static bool IsValidAddress(string address)
    {
        try
        {
            AElf.Types.Address.FromBase58(address);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static string GetNftImageKey()
    {
        return "__nft_image_url";
    }


    public static string GetInscriptionImageKey()
    {
        return "inscription_image";
    }


    public static DateTime ConvertStringToDate(string s)
    {
        return DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(s)).DateTime;
    }


    public static long GetMiningRewardPerBlock(long blockChainStartTimestamp)
    {
        var miningReward = InitialMiningRewardPerBlock;
        var blockAge = GetBlockchainAge(blockChainStartTimestamp);
        var denominator = blockAge.Div(TimeToReduceMiningRewardByHalf);
        for (var i = 0; i < denominator; i++) miningReward = miningReward.Div(2);

        return miningReward;
    }


    private static long GetBlockchainAge(long blockChainStartTimestamp)
    {
        return DateTime.UtcNow.ToUtcSeconds() - blockChainStartTimestamp;
    }
}

public static class LogEventHelper
{
    public static LogEvent ParseLogEventExtraProperties(Dictionary<string, string> properties)
    {
        properties.TryGetValue("Indexed", out var indexed);
        properties.TryGetValue("NonIndexed", out var nonIndexed);

        var indexedList = indexed != null
            ? JsonConvert.DeserializeObject<List<string>>(indexed)
            : new List<string>();
        var logEvent = new LogEvent
        {
            Indexed = { indexedList?.Select(ByteString.FromBase64) },
        };

        if (nonIndexed != null)
        {
            logEvent.NonIndexed = ByteString.FromBase64(nonIndexed);
        }

        return logEvent;
    }

    public static long ParseTransactionFees(Dictionary<string, string> extraProperties)
    {
        var result = 0l;
        try
        {
            var feeMap = new Dictionary<string, long>();
            if (extraProperties == null)
            {
                return 0;
            }

            if (extraProperties.TryGetValue("TransactionFee", out var transactionFee))
            {
                feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                         new Dictionary<string, long>();
                if (feeMap.TryGetValue("ELF", out var fee))
                {
                    result += fee;
                }
            }

            if (extraProperties.TryGetValue("ResourceFee", out var resourceFee))
            {
                var resourceFeeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(resourceFee) ??
                                     new Dictionary<string, long>();
                if (resourceFeeMap.TryGetValue("ELF", out var fee))
                {
                    result += fee;
                }
            }
        }
        catch (JsonSerializationException e)
        {
            // ignore test data
            Log.Error(e, $"ParseTransactionFees error");
        }

        return result;
    }

    public static long ParseBurnt(long amount, string address, string symbol, string chainId)
    {
        if (TokenSymbolHelper.GetSymbolType(symbol) == SymbolType.Nft &&
            "SEED-0".Equals(TokenSymbolHelper.GetCollectionSymbol(symbol)))
        {
            return 0;
        }

        if (AddressListMap.TryGetValue(chainId, out var addressList) && addressList.Contains(address))
        {
            return amount;
        }

        return 0;
    }


    public static readonly Dictionary<string, List<string>> AddressListMap = new()
    {
        {
            "AELF", new List<string>()
            {
                "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                "2ZUgaDqWSh4aJ5s5Ker2tRczhJSNep4bVVfrRBRJTRQdMTbA5W",
                "SietKh9cArYub9ox6E4rU94LrzPad6TB72rCwe3X1jQ5m1C34"
            }
        },
        {
            "tDVV", new List<string>()
            {
                "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                "2YkY2kjG7dTPJuHcTP3fQyMqat2CMfo7kZoRr7QdejyHHbT4rk"
            }
        },
        {
            "tDVW", new List<string>()
            {
                "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
                "2YkY2kjG7dTPJuHcTP3fQyMqat2CMfo7kZoRr7QdejyHHbT4rk"
            }
        }
    };
}

public static class NumberFormatter
{
    public static string ToDecimalsString(this long number, int decimals)
    {
        var num = number / Math.Pow(10, decimals);
        return new BigDecimal(num).ToNormalizeString();
    }

    public static string ToDecimalsString(this double number, int decimals)
    {
        var num = number / Math.Pow(10, decimals);
        return new BigDecimal(num).ToNormalizeString();
    }

    public static string ToNormalizeString(this BigDecimal bigDecimal)
    {
        if (bigDecimal >= 0)
        {
            return bigDecimal.ToString();
        }

        var value = -bigDecimal;
        return "-" + value;
    }
}