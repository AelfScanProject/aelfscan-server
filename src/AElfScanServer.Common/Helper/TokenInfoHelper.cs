using System;
using System.Collections.Generic;
using System.Linq;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Enums;


using Newtonsoft.Json;

namespace AElfScanServer.Common.Helper;

public class TokenInfoHelper
{
    public static TransactionStatus OfTransactionStatus(string status)
    {
        return EnumConverter.ConvertToEnum<TransactionStatus>(status);
    }

    public static List<TransactionFeeDto> GetTransactionFee(List<ExternalInfoDto> externalInfos)
    {
        var extraProperties = externalInfos.ToDictionary(i => i.Key, i => i.Value);
        var feeMap = new Dictionary<string, long>();
        if (extraProperties.TryGetValue("TransactionFee", out var transactionFee))
        {
            feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                     new Dictionary<string, long>();
        }

        if (extraProperties.TryGetValue("ResourceFee", out var resourceFee))
        {
            var resourceFeeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(resourceFee) ??
                                 new Dictionary<string, long>();
            foreach (var (symbol, fee) in resourceFeeMap)
            {
                if (feeMap.ContainsKey(symbol))
                {
                    feeMap[symbol] += fee;
                }
                else
                {
                    feeMap[symbol] = fee;
                }
            }
        }
        return feeMap.Select(o => new TransactionFeeDto
        {
            Symbol = o.Key,
            Amount = DecimalHelper.Divide(o.Value, 8)
        }).ToList();
    }
    
    public static string GetImageUrl(List<ExternalInfoDto> externalInfo, Func<string> getImageUrlFunc)
    {
        var keysToCheck = new List<TokenCreatedExternalInfoEnum>
        {
            TokenCreatedExternalInfoEnum.NFTLogoImageUrl,
            TokenCreatedExternalInfoEnum.SpecialInscriptionImage,
            TokenCreatedExternalInfoEnum.InscriptionImage,
            TokenCreatedExternalInfoEnum.NFTImageUri,
            TokenCreatedExternalInfoEnum.NFTImageUrl
        };

        foreach (var key in keysToCheck)
        {
            var imageUrl = OfExternalInfoKeyValue(externalInfo, key);
            if (!imageUrl.IsNullOrWhiteSpace())
            {
                return imageUrl;
            }
        }
        return getImageUrlFunc();
    }
    
    public static string OfExternalInfoKeyValue(List<ExternalInfoDto> externalInfo, TokenCreatedExternalInfoEnum keyEnum)
    {
        var key = EnumDescriptionHelper.GetEnumDescription(keyEnum);
        return externalInfo.Where(e => e.Key == key).Select(e => e.Value).FirstOrDefault();
    }
    

}