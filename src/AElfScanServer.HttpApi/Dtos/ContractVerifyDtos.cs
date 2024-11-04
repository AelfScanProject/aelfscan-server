using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Dtos;

// public class AddContractFileInput
// {
//     [ModelBinder(BinderType = typeof(JsonModelBinder))]
//     public SubscriptionManifestDto Manifest { get; set; }
//
//     public IFormFile Code { get; set; }
//     public List<IFormFile> AttachmentList { get; set; }
// }

public class AddContractFileInput
{
    public string ChainId { get; set; }
    public string ContractAddress { get; set; }
}

public class UploadContractFileResponseDto
{
    public string FileKey { get; set; }
    public string Message { get; set; }

    public bool CodeIsSame { get; set; }
    public ContractFileResult Result { get; set; }

    public static UploadContractFileResponseDto Success(string msg, bool codeIsSame = false)
    {
        return new UploadContractFileResponseDto
        {
            Result = ContractFileResult.Success,
            Message = msg,
            CodeIsSame = codeIsSame
        };
    }

    public static UploadContractFileResponseDto Fail(string message)
    {
        return new UploadContractFileResponseDto
        {
            Message = message,
            Result = ContractFileResult.Fail,
        };
    }
}

public enum ContractFileResult
{
    Success,
    Fail,
    Exist
}