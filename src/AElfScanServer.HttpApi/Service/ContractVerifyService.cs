using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElfScanServer.Common.Commons;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Helper;
using AElfScanServer.Common.Options;
using AElfScanServer.Common.Provider;
using AElfScanServer.Grains;
using AElfScanServer.Grains.Grain.Contract;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Provider;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AElfScanServer.HttpApi.Service;

public interface IContractVerifyService
{
    Task<UploadContractFileResponseDto> UploadContractFileAsync(IFormFile file, string chainId, string contractAddress,
        string csprojPath, string dotnetVersion);


    Task<string> UploadContractStatesAsync(string chainId, string contractAddress);
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
    private readonly IDecompilerProvider _decompilerProvider;
    private const string ContractVersionFileName = "AssemblyInfo.cs";
    private IDistributedCache<ContractVerifyResult> _contractVerifyCache;

    public ContractVerifyService(IClusterClient clusterClient, IAwsS3Provider awsS3ClientService,
        IOptionsMonitor<GlobalOptions> globalOptions, IIndexerGenesisProvider indexerGenesisProvider,
        IK8sProvider k8sProvider, ILogger<ContractVerifyService> logger, IDecompilerProvider decompilerProvider,
        IDistributedCache<ContractVerifyResult> contractVerifyCache)
    {
        _clusterClient = clusterClient;
        _awsS3ClientService = awsS3ClientService;
        _globalOptions = globalOptions;
        _indexerGenesisProvider = indexerGenesisProvider;
        _k8sProvider = k8sProvider;
        _logger = logger;
        _decompilerProvider = decompilerProvider;
        _contractVerifyCache = contractVerifyCache;
    }


    public async Task<string> UploadContractStatesAsync(string chainId, string contractAddress)
    {
        var list = await _indexerGenesisProvider.GetContractListAsync(chainId, 0, 1, "BlockTime", "asc",
            contractAddress);
        var contractInfoDto = list.ContractList.Items.First();
        var version = contractInfoDto.Version;

        var versrinonStr = contractInfoDto.ContractVersion.IsNullOrEmpty()
            ? version.ToString()
            : contractInfoDto.ContractVersion;
        var contractRegistration =
            await _indexerGenesisProvider.GetContractRegistrationAsync(chainId, contractInfoDto.CodeHash);
        var getFilesResult = await _decompilerProvider.GetFilesAsync(contractRegistration[0].Code);

        await _clusterClient
            .GetGrain<IContractFileCodeGrain>(GrainIdHelper.GenerateContractFileKey(chainId, contractAddress))
            .SaveAndUpdateAsync(
                new ContractFileResultDto
                {
                    ChainId = chainId,
                    Address = contractAddress,
                    LastBlockHeight = contractInfoDto.Metadata.Block.BlockHeight,
                    ContractName = GetContractName(chainId, contractAddress),
                    ContractVersion = contractInfoDto.ContractVersion == ""
                        ? contractInfoDto.Version.ToString()
                        : contractInfoDto.ContractVersion,
                    ContractSourceCode = getFilesResult.Data,
                    IsVerify = false
                });

        return "success";
    }

    public string GetContractName(string chainId, string address)
    {
        _globalOptions.CurrentValue.ContractNames.TryGetValue(chainId, out var contractNames);
        if (contractNames == null)
        {
            return "";
        }

        contractNames.TryGetValue(address, out var contractName);

        return contractName;
    }


    public async Task<UploadContractFileResponseDto> UploadContractFileAsync(IFormFile file, string chainId,
        string contractAddress, string csprojPath, string dotnetVersion)
    {
        
        var startNew = Stopwatch.StartNew();
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


        var isValidNetVersion = await IsValidNetVersion(file, csprojPath, dotnetVersion);
        if (!isValidNetVersion)
        {
            return UploadContractFileResponseDto.Fail("The.NET version is inconsistent with the source code");
        }

        string contractName = Path.GetFileNameWithoutExtension(csprojPath);

        try
        {
            var getContractCodeStart = Stopwatch.StartNew();
            var contractInfo = await GetContractCode(chainId, contractAddress);
            getContractCodeStart.Stop();
            _logger.LogInformation(
                $"Statistical time Get contract code from indexer: {getContractCodeStart.Elapsed.TotalSeconds}");
            var contractVersion = contractInfo.Item1;
            var contractCode = contractInfo.Item2;
            _logger.LogInformation("Uploading contract file to S3...");

            var upLoadJsonFileStart = Stopwatch.StartNew();
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                await _awsS3ClientService.UpLoadJsonFileAsync(memoryStream,
                    _globalOptions.CurrentValue.S3ContractFileDirectory,
                    GrainIdHelper.GenerateContractFile(chainId, contractAddress, contractName, contractVersion));
                upLoadJsonFileStart.Stop();
                _logger.LogInformation(
                    $"Statistical time upLoadJsonFileStart: {upLoadJsonFileStart.Elapsed.TotalSeconds}");
            }

            if (await ValidContractFile(chainId, contractAddress, contractName, dotnetVersion, contractVersion,
                    contractCode))

            {
                var saveContractFileToGrainStart = Stopwatch.StartNew();
                await SaveContractFileToGrain(file, chainId, contractAddress, csprojPath);
                saveContractFileToGrainStart.Stop();
                _logger.LogInformation(
                    $"Statistical time saveContractFileToGrainStart: {saveContractFileToGrainStart.Elapsed.TotalSeconds}");
               
                _logger.LogInformation($"{contractAddress} Contract file validated and saved successfully.");

                startNew.Stop();
                _logger.LogInformation(
                    $"Statistical time TOTAL: {startNew.Elapsed.TotalSeconds}");
                return UploadContractFileResponseDto.Success("Upload success", true);
            }
            else
            {
                _logger.LogWarning($"{contractAddress} Contract file validation failed");
                return UploadContractFileResponseDto.Fail("Contract code mismatch. Please re-upload.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"{contractAddress} An error occurred during contract file upload and validation process.");
            return UploadContractFileResponseDto.Fail("Verification failed");
        }
    }


    private async Task<string> GetContractVersion(string contractCode)
    {
        var fileList = new List<DecompilerContractFileDto>();

        var getContractFilesResponseDto = await _decompilerProvider.GetFilesAsync(contractCode);
        if (!getContractFilesResponseDto.Data.IsNullOrEmpty())
        {
            fileList = getContractFilesResponseDto.Data;
        }

        var fileContent = "";

        foreach (var file in fileList)
        {
            fileContent = await GetVersionContent(file);
            if (!fileContent.IsNullOrEmpty())
            {
                break;
            }
        }

        if (!fileContent.IsNullOrEmpty())
        {
            return GetAssemblyInformationalVersion(fileContent);
        }

        return "";
    }


    public string GetAssemblyInformationalVersion(string base64EncodedContent)
    {
        byte[] decodedBytes = Convert.FromBase64String(base64EncodedContent);
        string decodedContent = Encoding.UTF8.GetString(decodedBytes);

        var match = Regex.Match(decodedContent, @"AssemblyInformationalVersion\(""([^""]*)""\)");

        if (match.Success)
        {
            string contentInsideBrackets = match.Groups[1].Value;
            return contentInsideBrackets;
        }
        else
        {
            _logger.LogInformation("Not finf AssemblyInformationalVersion version");
        }

        return "";
    }

    private async Task<string> GetVersionContent(DecompilerContractFileDto file)
    {
        if (file.Name == ContractVersionFileName)
        {
            return file.Content;
        }

        if (file.Files.IsNullOrEmpty())
        {
            return "";
        }

        foreach (var decompilerContractFileDto in file.Files)
        {
            var versionContent = await GetVersionContent(decompilerContractFileDto);

            if (!versionContent.IsNullOrEmpty())
            {
                return versionContent;
            }
        }

        return "";
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

            var versrinonStr = contractInfoDto.ContractVersion.IsNullOrEmpty()
                ? version.ToString()
                : contractInfoDto.ContractVersion;
            var contractRegistration =
                await _indexerGenesisProvider.GetContractRegistrationAsync(chainId, contractInfoDto.CodeHash);
            _logger.LogInformation("Contract code fetched successfully.");

            var contractVersion = await GetContractVersion(contractRegistration[0].Code);

            if (!contractVersion.IsNullOrEmpty())
            {
                versrinonStr = contractVersion;
            }

            return (versrinonStr, contractRegistration[0].Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch contract code.");
            throw;
        }
    }


    public async Task<bool> ValidContractFile(string chainId, string contractAddress, string contractName,
        string dotnetVersion, string contractVersion, string originalContractCode)
    {
        _logger.LogInformation("Starting validation for contract: {ContractName}, Version: {ContractVersion}",
            contractName, contractVersion);

        try
        {
            var k8sStart = Stopwatch.StartNew();

            await _k8sProvider.StartJob(_globalOptions.CurrentValue.Images[dotnetVersion], chainId, contractAddress,
                contractName, contractVersion);
            _logger.LogInformation("Kubernetes job started for contract validation.");
            _logger.LogInformation($"Statistical time k8sStart: {k8sStart.Elapsed.TotalSeconds}");

            var s3Dlownload = Stopwatch.StartNew();

            var result = await _awsS3ClientService.GetContractFileAsync(
                _globalOptions.CurrentValue.S3ContractFileDirectory,
                GrainIdHelper.GenerateContractDLL(chainId, contractAddress, contractName, contractVersion));
            s3Dlownload.Stop();
            _logger.LogInformation($"Statistical time s3Dlownload: {s3Dlownload.Elapsed.TotalSeconds}");

            var k8sContractCode = Convert.ToBase64String(result);


            var contractCodeLength = originalContractCode.Length;
            var base64StringLength = k8sContractCode.Length;
            bool isValid = k8sContractCode == originalContractCode;

            if (contractCodeLength == base64StringLength)
            {
                _logger.LogInformation("Contract codes have the same length. Ensure versions match.");
            }
            else
            {
                _logger.LogWarning(
                    "{address} Contract code lengths differ: Original Length: {OriginalLength}, S3 Length: {S3Length}",
                    contractAddress, contractCodeLength, base64StringLength);
            }

            _logger.LogInformation($"{contractAddress} Contract validation result: {isValid}");
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during contract validation. {contractAddress}");
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
            contractInfo.IsVerify = true;
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


    public async Task<bool> IsValidNetVersion(IFormFile file, string csprojPath,
        string netVersion)
    {
        try
        {
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

                        if (entry.Length > 0)
                        {
                            if (entry.FullName.EndsWith(".csproj"))
                            {
                                var netVersionFromCsproj = await GetNetVersion(entry);
                                return netVersionFromCsproj == netVersion;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while compare net version.");
        }

        return false;
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

    private async Task<string> GetNetVersion(ZipArchiveEntry entry)
    {
        using (var entryStream = entry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            var content = await reader.ReadToEndAsync();
            var match = Regex.Match(content, @"<TargetFramework>(.*?)</TargetFramework>");
            var netVersion = match.Success ? match.Groups[1].Value : null;

            return netVersion;
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