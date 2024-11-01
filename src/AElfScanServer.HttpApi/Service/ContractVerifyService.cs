using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElfScanServer.Common.Commons;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Provider;
using AElfScanServer.Grains;
using AElfScanServer.Grains.Grain.Contract;
using AElfScanServer.HttpApi.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AElfScanServer.HttpApi.Service;

public interface IContractVerifyService
{
    // Task UploadAppAttachmentAsync(IFormFile file, string appId, string version);
    // Task DeleteAppAttachmentAsync(string appId, string version, string fileKey);


    Task<UploadContractFileResponseDto> UploadContractFileAsync(IFormFile file, string chainId, string contractAddress,
        string csprojPath);
    // Task DeleteAllAppAttachmentsAsync(string appId, string version);
    // Task<string> GetAppAttachmentContentAsync(string appId, string version, string fileKey);
    // Task UploadAppAttachmentListAsync(List<IFormFile> attachmentList, string appId, string version);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ContractVerifyService : IContractVerifyService
{
    private readonly IClusterClient _clusterClient;
    private readonly IAwsS3Provider _awsS3ClientService;
    private const long MaxFileSize = 10 * 1024 * 1024;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;

    public ContractVerifyService(IClusterClient clusterClient, IAwsS3Provider awsS3ClientService,
        IOptionsMonitor<GlobalOptions> globalOptions)
    {
        _clusterClient = clusterClient;
        _awsS3ClientService = awsS3ClientService;
        _globalOptions = globalOptions;
    }

    public async Task<UploadContractFileResponseDto> UploadContractFileAsync(IFormFile file, string chainId,
        string contractAddress, string csprojPath)
    {
        if (file == null || file.Length == 0)
            return UploadContractFileResponseDto.Fail("The file cannot be empty");

        if (file.Length > MaxFileSize)
            return UploadContractFileResponseDto.Fail("File size exceeds limit");

        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (fileExtension != ".zip")
            return UploadContractFileResponseDto.Fail("Only ZIP files can be uploaded");


        var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var fileKey = await _awsS3ClientService.UpLoadJsonFileAsync(memoryStream,
            _globalOptions.CurrentValue.S3ContractFileDirectory,
            GrainIdHelper.GenerateContractFileKey(chainId, contractAddress));


        var pathParts = csprojPath.Split('/');
        if (pathParts.Length > 1)
        {
            pathParts = pathParts.Take(pathParts.Length - 1).ToArray();
        }


        if (await ValidContractFile(chainId, contractAddress))
        {
            await SaveContractFileToGrain(file, chainId, contractAddress, csprojPath);
        }


        return UploadContractFileResponseDto.Success("Upload success");
    }


    public async Task<bool> ValidContractFile(string chainId, string contractAddress)
    {
        var contractFileAsync =
            await _awsS3ClientService.GetContractFileAsync(_globalOptions.CurrentValue.S3ContractFileDirectory,
                GrainIdHelper.GenerateContractFileKey(chainId, contractAddress));

        return true;
    }

    public async Task SaveContractFileToGrain(IFormFile file, string chainId, string address, string csprojPath)
    {
        var contractFileResult = new ContractFileResultDto
        {
            ContractSourceCode = new List<DecompilerContractFileDto>(),
            ChainId = chainId,
            Address = address
        };

        var directoryName = Path.GetDirectoryName(csprojPath);

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    if (!entry.FullName.Contains(directoryName))
                    {
                        continue;
                    }

                    if (entry.Length == 0)
                    {
                        AddDirectory(contractFileResult.ContractSourceCode, entry.FullName);
                    }
                    else
                    {
                        var base64Content = await GetBase64Content(entry);
                        AddFileToDirectory(contractFileResult.ContractSourceCode, entry.FullName, base64Content);
                    }
                }
            }
        }

        var contractInfo = await _clusterClient
            .GetGrain<IContractFileCodeGrain>(GrainIdHelper.GenerateContractFileKey(chainId, address)).GetAsync();

        contractInfo.ContractSourceCode = contractFileResult.ContractSourceCode;
        await _clusterClient
            .GetGrain<IContractFileCodeGrain>(GrainIdHelper.GenerateContractFileKey(chainId, address))
            .SaveAndUpdateAsync(contractInfo);
    }


    private async Task<string> GetBase64Content(ZipArchiveEntry entry)
    {
        using (var entryStream = entry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            var content = await reader.ReadToEndAsync();
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        }
    }

    private void AddFileToDirectory(List<DecompilerContractFileDto> directories, string fullPath, string base64Content)
    {
        var pathParts = fullPath.Split('/');

        var fileName = "";
        var list = new List<string>();
        for (var i = 0; i < pathParts.Length; i++)
        {
            var pathPart = pathParts[i];
            if (i == 0 || pathPart.IsNullOrEmpty())
            {
                continue;
            }

            if (i != pathParts.Length - 1)
            {
                list.Add(pathPart);
            }

            fileName = pathPart;
        }

        pathParts = list.ToArray();
        var currentDirectory = directories;
        for (var i = 0; i < pathParts.Length; i++)
        {
            var part = pathParts[i];

            if (part.IsNullOrEmpty())
            {
                continue;
            }


            var directory = currentDirectory.Find(d => d.Name == part);
            if (directory == null)
            {
                directory = new DecompilerContractFileDto
                {
                    Name = part,
                    Files = new List<DecompilerContractFileDto>(),
                };
                currentDirectory.Add(directory);
            }

            if (i != pathParts.Length - 1)
            {
                currentDirectory = directory.Files;
            }
        }


        if (currentDirectory.Count > 0)
        {
            var file = new DecompilerContractFileDto
            {
                Name = fileName,
                Content = base64Content,
            };
            currentDirectory.Last().Files.Add(file);
        }
    }

    private void AddDirectory(List<DecompilerContractFileDto> directories, string fullPath)
    {
        var pathParts = fullPath.Split('/');
        var currentDirectory = directories;

        pathParts = pathParts.Skip(1).ToArray();
        foreach (var part in pathParts)
        {
            if (part.IsNullOrEmpty())
            {
                continue;
            }

            var directory = currentDirectory.Find(d => d.Name == part);
            if (directory == null)
            {
                directory = new DecompilerContractFileDto
                {
                    Name = part,
                    Files = new List<DecompilerContractFileDto>(),
                };
                currentDirectory.Add(directory);
            }

            currentDirectory = directory.Files;
        }
    }

    // public async Task<Stream> ConvertToFileStream(IFormFile file)
    // {
    //     var memoryStream = new MemoryStream();
    //     file.CopyTo(memoryStream);
    //     memoryStream.ToArray()
    //     var compressedData = ZipHelper.ConvertIFormFileToByteArray(attachment);
    // }

    private string GenerateAppAwsS3FileName(string version, string fileName)
    {
        return version + "-" + fileName;
    }
}