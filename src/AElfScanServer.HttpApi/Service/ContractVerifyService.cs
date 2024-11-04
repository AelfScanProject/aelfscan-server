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
using AElfScanServer.HttpApi.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AElfScanServer.HttpApi.Service;

public interface IContractVerifyService
{
    Task<UploadContractFileResponseDto> UploadContractFileAsync(IFormFile file, string chainId, string contractAddress,
        string csprojPath, string dotnetVersion);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ContractVerifyService : IContractVerifyService
{
    private readonly IClusterClient _clusterClient;
    private readonly IAwsS3Provider _awsS3ClientService;
    private const long MaxFileSize = 10 * 1024 * 1024;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly IIndexerGenesisProvider _indexerGenesisProvider;
    private readonly IK8sProvider _k8sProvider;
    private readonly ILogger<ContractVerifyService> _logger;

    public ContractVerifyService(IClusterClient clusterClient, IAwsS3Provider awsS3ClientService,
        IOptionsMonitor<GlobalOptions> globalOptions, IIndexerGenesisProvider indexerGenesisProvider,
        IK8sProvider k8sProvider, ILogger<ContractVerifyService> logger)
    {
        _clusterClient = clusterClient;
        _awsS3ClientService = awsS3ClientService;
        _globalOptions = globalOptions;
        _indexerGenesisProvider = indexerGenesisProvider;
        _k8sProvider = k8sProvider;
        _logger = logger;
    }

    public async Task<UploadContractFileResponseDto> UploadContractFileAsync(IFormFile file, string chainId,
        string contractAddress, string csprojPath, string dotnetVersion)
    {
        _logger.LogInformation(
            "Starting upload for contract file: {FileName}, ChainId: {ChainId}, ContractAddress: {ContractAddress}",
            file.FileName, chainId, contractAddress);

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("The file is empty. Upload failed.");
            return UploadContractFileResponseDto.Fail("The file cannot be empty");
        }

        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("File size exceeds limit: {FileSize} bytes.", file.Length);
            return UploadContractFileResponseDto.Fail("File size exceeds limit");
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (fileExtension != ".zip")
        {
            _logger.LogWarning("Invalid file extension: {FileExtension}. Only ZIP files are allowed.", fileExtension);
            return UploadContractFileResponseDto.Fail("Only ZIP files can be uploaded");
        }

        string contractName = Path.GetFileNameWithoutExtension(csprojPath);

        try
        {
            var contractInfo = await GetContractCode(chainId, contractAddress);
            var contractVersion = contractInfo.Item1;
            var contractCode = contractInfo.Item2;

            _logger.LogInformation("Uploading contract file to S3...");
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                await _awsS3ClientService.UpLoadJsonFileAsync(memoryStream,
                    _globalOptions.CurrentValue.S3ContractFileDirectory,
                    GrainIdHelper.GenerateContractFile(chainId, contractAddress, contractName, contractVersion));
            }

            if (await ValidContractFile(chainId, contractAddress, contractName, dotnetVersion, contractVersion,
                    contractCode))
                
            {
                await SaveContractFileToGrain(file, chainId, contractAddress, csprojPath);
                _logger.LogInformation("Contract file validated and saved successfully.");
                return UploadContractFileResponseDto.Success("Upload success", true);
            }
            else
            {
                _logger.LogWarning("Contract file validation failed");
                return UploadContractFileResponseDto.Success("Contract code verification failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during contract file upload and validation process.");
            return UploadContractFileResponseDto.Fail("An error occurred during upload");
        }
    }


    private async Task<(string, string)> GetContractCode(string chainId, string contractAddress)
    {
        _logger.LogInformation("Fetching contract code for ChainId: {ChainId}, ContractAddress: {ContractAddress}",
            chainId, contractAddress);
        try
        {
            var list = await _indexerGenesisProvider.GetContractListAsync(chainId, 0, 1, "BlockTime", "asc",
                contractAddress);
            var contractInfoDto = list.ContractList.Items.First();
            var version = contractInfoDto.Version;

            var contractRegistration =
                await _indexerGenesisProvider.GetContractRegistrationAsync(chainId, contractInfoDto.CodeHash);
            _logger.LogInformation("Contract code fetched successfully.");
            return (version.ToString(), contractRegistration[0].Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch contract code.");
            throw;
        }
    }


    public async Task<bool> ValidContractFile(string chainId, string contractAddress, string contractName,
        string dotnetVersion, string contractVersion, string contractCode)
    {
        _logger.LogInformation("Starting validation for contract: {ContractName}, Version: {ContractVersion}",
            contractName, contractVersion);
        try
        {
            await _k8sProvider.StartJob(_globalOptions.CurrentValue.Images[dotnetVersion], chainId, contractAddress,
                contractName, contractVersion);
            _logger.LogInformation("Kubernetes job started for contract validation.");

            var result = await _awsS3ClientService.GetContractFileAsync(
                _globalOptions.CurrentValue.S3ContractFileDirectory,
                GrainIdHelper.GenerateContractDLL(chainId, contractAddress, contractName, contractVersion));

            var base64String = Convert.ToBase64String(result);
            bool isValid = base64String == contractCode;

            _logger.LogInformation("Contract validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during contract validation.");
            return false;
        }
    }

    public async Task SaveContractFileToGrain(IFormFile file, string chainId, string address, string csprojPath)
    {
        _logger.LogInformation("Saving contract file to grain for ChainId: {ChainId}, Address: {Address}", chainId,
            address);
        try
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

            _logger.LogInformation("Contract file saved successfully to grain.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while saving contract file to grain.");
        }
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
}