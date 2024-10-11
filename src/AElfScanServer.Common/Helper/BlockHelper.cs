using System;
using AElf.ExceptionHandler;
using AElf.Types;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.Options;

namespace AElfScanServer.Common.Helper;

public class BlockHelper
{
    public static bool IsTxHash(string transactionId)
    {
        try
        {
            Hash.LoadFromHex(transactionId);
        }
        catch
        {
            return false;
        }

        return true;
    }


    public static bool IsAddress(string address)
    {
        try
        {
            AElf.Types.Address.FromBase58(address);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static bool IsBlockHeight(string input)
    {
        if (int.TryParse(input, out int height) && height >= 0)
        {
            return true;
        }

        return false;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException))]
    public static string GetContractName(GlobalOptions option, string chainId, string address)
    {
        if (option.ContractNames.TryGetValue(chainId, out var names))
        {
            if (names.TryGetValue(address, out var name))
            {
                return name;
            }
        }

        return "";
    }
}