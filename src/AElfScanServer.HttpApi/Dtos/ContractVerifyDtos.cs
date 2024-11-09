using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AElfScanServer.HttpApi.Dtos;

public enum VerifyErrCode
{
    VerifyErr,
    NetVersionErr,
    PathErr
}

public class UploadContractFileResponseDto
{
    public string Message { get; set; }

    public string ErrMessage { get; set; }
    public bool CodeIsSame { get; set; }

    public List<string> DiffFileNames { get; set; }
    public VerifyErrCode ErrCode { get; set; }

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

    public static UploadContractFileResponseDto Fail(string message, VerifyErrCode err,
        List<string> diffFileNames = null)
    {
        return new UploadContractFileResponseDto
        {
            Message = message,
            Result = ContractFileResult.Fail,
            ErrCode = err,
            DiffFileNames = diffFileNames
        };
    }

    public static UploadContractFileResponseDto SystemFail(string message, VerifyErrCode err, string errMsg)
    {
        return new UploadContractFileResponseDto
        {
            Message = message,
            Result = ContractFileResult.Fail,
            ErrCode = err,
            ErrMessage = errMsg
        };
    }
}

public enum ContractFileResult
{
    Success,
    Fail,
    Exist
}