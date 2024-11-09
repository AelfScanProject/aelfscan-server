using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElfScanServer.Common.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class ContractFileComparer
{
    public static (List<string> diffFileNames, bool isDiff) CompareGetContractFilesResponseDto(
        GetContractFilesResponseDto originalFiles, GetContractFilesResponseDto k8sFiles, string contractName)
    {
        var diffFileNames = new List<string>();

        if (originalFiles == null || k8sFiles == null)
            return (diffFileNames, true);

        var originalContractDirectory = originalFiles.Data.First(c => c.Name == contractName);
        var k8sContractDirectory = k8sFiles.Data.First(c => c.Name == contractName);


        CompareDirectory(originalContractDirectory, k8sContractDirectory, diffFileNames);


        return (diffFileNames, diffFileNames.Count > 0);
    }


    public static void CompareDirectory(DecompilerContractFileDto source,
        DecompilerContractFileDto target, List<string> diffFileNames)
    {
        if (source.Files.IsNullOrEmpty())
        {
            if (!target.Files.IsNullOrEmpty())
            {
                diffFileNames.Add(target.Name);
                return;
            }
        }

        if (target.Files.IsNullOrEmpty())
        {
            if (!source.Files.IsNullOrEmpty())
            {
                diffFileNames.Add(source.Name);
                return;
            }
        }


        var sourceFiles = source.Files.OrderBy(c => c.Name);
        var targetFiles = target.Files.OrderBy(c => c.Name);

        foreach (var file in targetFiles)
        {
            if (file.Name.EndsWith(".csproj"))
                continue;

            var findFile = sourceFiles.First(c => c.Name == file.Name);

            if (file.Content.IsNullOrEmpty() && findFile != null && findFile.Content.IsNullOrEmpty())
            {
                CompareDirectory(file, findFile, diffFileNames);
                continue;
            }

            if (findFile == null)
            {
                diffFileNames.Add(file.Name);
                continue;
            }

            if (CompareContractFileIsDiff(file.Content, findFile.Content, file.Name))
            {
                diffFileNames.Add(file.Name);
            }
        }
    }


    private static bool CompareContractFileIsDiff(string content1, string content2, string fileName)
    {
        string decodedContent1 = DecodeBase64(content1);
        string decodedContent2 = DecodeBase64(content2);

        bool areEquivalent = CompareCodeContent(decodedContent1, decodedContent2);
        if (!areEquivalent)
        {
            return true;
        }

        // SaveContentDifference(fileName, decodedContent1, decodedContent2);
        return false;
    }

    private static string DecodeBase64(string encodedContent)
    {
        if (string.IsNullOrEmpty(encodedContent))
            return string.Empty;

        var bytes = Convert.FromBase64String(encodedContent);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static bool CompareCodeContent(string code1, string code2)
    {
        var tree1 = CSharpSyntaxTree.ParseText(code1);
        var tree2 = CSharpSyntaxTree.ParseText(code2);
        var root1 = tree1.GetRoot();
        var root2 = tree2.GetRoot();

        return root1.IsEquivalentTo(root2);
    }

    private static void SaveContentDifference(string name, string content1, string content2)
    {
        string filePath1 = Path.Combine(Directory.GetCurrentDirectory(), $"Difference_{name}_File1.txt");
        string filePath2 = Path.Combine(Directory.GetCurrentDirectory(), $"Difference_{name}_File2.txt");

        File.WriteAllText(filePath1, content1);
        File.WriteAllText(filePath2, content2);
    }
}