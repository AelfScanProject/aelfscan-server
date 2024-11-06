using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
    private readonly IDecompilerProvider _decompilerProvider;

    public ContractVerifyService(IClusterClient clusterClient, IAwsS3Provider awsS3ClientService,
        IOptionsMonitor<GlobalOptions> globalOptions, IIndexerGenesisProvider indexerGenesisProvider,
        IK8sProvider k8sProvider, ILogger<ContractVerifyService> logger, IDecompilerProvider decompilerProvider)
    {
        _clusterClient = clusterClient;
        _awsS3ClientService = awsS3ClientService;
        _globalOptions = globalOptions;
        _indexerGenesisProvider = indexerGenesisProvider;
        _k8sProvider = k8sProvider;
        _logger = logger;
        _decompilerProvider = decompilerProvider;
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


    private async Task<string> GetContractVersion(string chainId, string contractAddress, string conrtactName,
        string contractCode)
    {
        var contractInfo = await _clusterClient
            .GetGrain<IContractFileCodeGrain>(GrainIdHelper.GenerateContractFileKey(chainId, contractAddress))
            .GetAsync();
        var fileList = new List<DecompilerContractFileDto>();

        if (contractInfo == null)
        {
            var getContractFilesResponseDto = await _decompilerProvider.GetFilesAsync(contractCode);
            if (!getContractFilesResponseDto.Data.IsNullOrEmpty())
            {
                fileList = getContractFilesResponseDto.Data;
            }
        }
        else
        {
            fileList = contractInfo.ContractSourceCode;
        }

        foreach (var decompilerContractFileDto in fileList)
        {
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
            // 1. 启动 Kubernetes Job 进行合约验证
            await _k8sProvider.StartJob(_globalOptions.CurrentValue.Images[dotnetVersion], chainId, contractAddress,
                contractName, contractVersion);
            _logger.LogInformation("Kubernetes job started for contract validation.");

            // 2. 从 S3 获取已上传的合约文件内容
            var result = await _awsS3ClientService.GetContractFileAsync(
                _globalOptions.CurrentValue.S3ContractFileDirectory,
                GrainIdHelper.GenerateContractDLL(chainId, contractAddress, contractName, contractVersion));

            // 3. 获取本地合约文件并进行 base64 编码
            var localFilePath =
                "/Users/wuhaoxuan/Downloads/build 4/EBridge.Contracts.Bridge-1.5.0.0/EBridge.Contracts.Bridge.dll";
            string localFileBase64;
            if (File.Exists(localFilePath))
            {
                var localFileBytes = await File.ReadAllBytesAsync(localFilePath);
                localFileBase64 = Convert.ToBase64String(localFileBytes);
                _logger.LogInformation("Local contract file read and encoded successfully.");
            }
            else
            {
                _logger.LogError("Local contract file not found at path: {LocalFilePath}", localFilePath);
                return false;
            }

            // 4. 比较合约文件内容的 base64 编码结果
            var k8sContractCode = Convert.ToBase64String(result);


            var contractCodeLength = originalContractCode.Length;
            var base64StringLength = k8sContractCode.Length;
            bool isValid = k8sContractCode == originalContractCode;
            bool isValid2 = k8sContractCode == localFileBase64;

            var k8sFileData = await _decompilerProvider.GetFilesAsync(k8sContractCode);

            var originalFileData = await _decompilerProvider.GetFilesAsync(originalContractCode);

            var k8sHash = ComputeMd5Hash(k8sFileData);
            var originalHash = ComputeMd5Hash(originalFileData);

            var compareGetContractFilesResponseDto = CompareGetContractFilesResponseDto(k8sFileData, originalFileData);


            // 输出合约文件对比结果和长度信息
            if (contractCodeLength == base64StringLength)
            {
                _logger.LogInformation("Contract codes have the same length. Ensure versions match.");
            }
            else
            {
                _logger.LogWarning(
                    "Contract code lengths differ: Original Length: {OriginalLength}, S3 Length: {S3Length}",
                    contractCodeLength, base64StringLength);
            }

            _logger.LogInformation("Contract validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during contract validation.");
            return false;
        }
    }


    private void WriteFile(string s, string fileName)
    {
        // 解码 Base64 字符串
        byte[] fileBytes = Convert.FromBase64String(s);

        // 创建临时文件路径
        string tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), fileName + ".txt");

        // 将字节写入临时文件
        File.WriteAllBytes(tempFilePath, fileBytes);
    }

    private string ComputeMd5Hash<T>(T input)
    {
        using (var md5 = MD5.Create())
        {
            // 将对象序列化为 JSON 字符串
            var json = JsonSerializer.Serialize(input);
            var bytes = Encoding.UTF8.GetBytes(json);

            // 计算哈希值
            var hashBytes = md5.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }

    public bool CompareGetContractFilesResponseDto(GetContractFilesResponseDto obj1,
        GetContractFilesResponseDto obj2)
    {
        if (obj1 == null || obj2 == null)
            return obj1 == obj2;

        var a = 1;
        if (obj1.Code != obj2.Code)
        {
            a = 1;
        }

        if (obj1.Msg != obj2.Msg)
        {
            a = 1;
        }

        if (obj1.Version != obj2.Version)
        {
            a = 1;
        }

        // 比较基本类型字段
        // if (obj1.Code != obj2.Code || obj1.Msg != obj2.Msg || obj1.Version != obj2.Version)
        //     return false;

        // 比较 Data 字段中的每个 DecompilerContractFileDto 对象
        // if (obj1.Data == null || obj2.Data == null)
        //     return obj1.Data == obj2.Data;


        if (obj1.Data.Count != obj2.Data.Count)
            a = 1;

        // 对 Data 列表按 Name 字段排序后再进行比较
        var sortedData1 = obj1.Data.OrderBy(d => d.Name).ToList();
        var sortedData2 = obj2.Data.OrderBy(d => d.Name).ToList();

        for (int i = 0; i < sortedData1.Count; i++)
        {
            if (!CompareDecompilerContractFileDto(sortedData1[i], sortedData2[i]))
                return false;
        }

        return true;
    }

    public bool CompareDecompilerContractFileDto(DecompilerContractFileDto dto1, DecompilerContractFileDto dto2)
    {
        if (dto1 == null || dto2 == null)
            return dto1 == dto2;

        var a = 1;

        if (dto1.Name != dto2.Name)
        {
            a = 1;
        }

        if (dto1.Content != dto2.Content)
        {
            CompareText(dto1.Content, dto2.Content);


            a = 1;
        }

        if (dto1.FileType != dto2.FileType)
        {
            a = 1;
        }


        // 比较 Files 字段：递归前按 Name 字段排序
        // if (dto1.Files == null || dto2.Files == null)
        //     return dto1.Files == dto2.Files;

        if (dto1.Files == null)
        {
            if (dto2.Files == null)
            {
                return true;
            }

            a = 1;
        }

        if (dto1.Files.Count != dto2.Files.Count)
            a = 1;

        var sortedFiles1 = dto1.Files.OrderBy(f => f.Name).ToList();
        var sortedFiles2 = dto2.Files.OrderBy(f => f.Name).ToList();

        for (int i = 0; i < sortedFiles1.Count; i++)
        {
            if (!CompareDecompilerContractFileDto(sortedFiles1[i], sortedFiles2[i]))
                return false;
        }

        return true;
    }


    private bool CompareText(string s1, string s2)
    {
        byte[] data = Convert.FromBase64String(s1);
        string decodedStringS1 = Encoding.UTF8.GetString(data);

        byte[] data2 = Convert.FromBase64String(s2);
        string decodedStringS2 = Encoding.UTF8.GetString(data2);

        var tree1 = CSharpSyntaxTree.ParseText(JsonConvert.SerializeObject(decodedStringS1));
        var tree2 = CSharpSyntaxTree.ParseText(JsonConvert.SerializeObject(decodedStringS2));
        var root1 = tree1.GetRoot();
        var root2 = tree2.GetRoot();
        bool areEquivalent = root1.IsEquivalentTo(root2);


        return areEquivalent;
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