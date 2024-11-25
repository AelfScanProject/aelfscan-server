using AElfScanServer.HttpApi.Service;
using Xunit.Abstractions;

namespace AElfScanServer.Service;

using System;
using System.Collections.Generic;
using System.Linq;
using AElfScanServer.Common.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using Xunit;

public class ContractFileComparerTests : AElfScanServerApplicationTestBase
{
    public ContractFileComparerTests(ITestOutputHelper output) : base(output)
    {
        
    }


    [Fact]
    public void CompareGetContractFilesResponseDto_ShouldDetectDifferences()
    {
        // Arrange
        var originalFiles = new GetContractFilesResponseDto
        {
            Data = new List<DecompilerContractFileDto>
            {
                new DecompilerContractFileDto
                {
                    Name = "TestContract",
                    Files = new List<DecompilerContractFileDto>
                    {
                        new DecompilerContractFileDto
                        {
                            Name = "File1.cs",
                            Content = EncodeBase64("public class File1 { }")
                        },
                        new DecompilerContractFileDto
                        {
                            Name = "File2.cs",
                            Content = EncodeBase64("public class File2 { }")
                        }
                    }
                }
            }
        };

        var k8sFiles = new GetContractFilesResponseDto
        {
            Data = new List<DecompilerContractFileDto>
            {
                new DecompilerContractFileDto
                {
                    Name = "TestContract",
                    Files = new List<DecompilerContractFileDto>
                    {
                        new DecompilerContractFileDto
                        {
                            Name = "File1.cs",
                            Content = EncodeBase64("public class File1 { }")
                        },
                        new DecompilerContractFileDto
                        {
                            Name = "File2.cs",
                            Content = EncodeBase64("public class File2Modified { }")
                        },
                        new DecompilerContractFileDto
                        {
                            Name = "File3.cs",
                            Content = EncodeBase64("public class File3 { }")
                        }
                    }
                }
            }
        };

        // Act
        var (diffFileNames, isDiff) = ContractFileComparer.CompareGetContractFilesResponseDto(
            originalFiles, k8sFiles, "TestContract");

        // Assert
        Assert.True(isDiff);
        Assert.Contains("File2.cs", diffFileNames);
        Assert.Contains("File3.cs", diffFileNames);
        Assert.Equal(2, diffFileNames.Count);
    }

    [Fact]
    public void CompareGetContractFilesResponseDto_ShouldReturnNoDifferences()
    {
        // Arrange
        var originalFiles = new GetContractFilesResponseDto
        {
            Data = new List<DecompilerContractFileDto>
            {
                new DecompilerContractFileDto
                {
                    Name = "TestContract",
                    Files = new List<DecompilerContractFileDto>
                    {
                        new DecompilerContractFileDto
                        {
                            Name = "File1.cs",
                            Content = EncodeBase64("public class File1 { }")
                        },
                        new DecompilerContractFileDto
                        {
                            Name = "File2.cs",
                            Content = EncodeBase64("public class File2 { }")
                        }
                    }
                }
            }
        };

        var k8sFiles = new GetContractFilesResponseDto
        {
            Data = new List<DecompilerContractFileDto>
            {
                new DecompilerContractFileDto
                {
                    Name = "TestContract",
                    Files = new List<DecompilerContractFileDto>
                    {
                        new DecompilerContractFileDto
                        {
                            Name = "File1.cs",
                            Content = EncodeBase64("public class File1 { }")
                        },
                        new DecompilerContractFileDto
                        {
                            Name = "File2.cs",
                            Content = EncodeBase64("public class File2 { }")
                        }
                    }
                }
            }
        };

        // Act
        var (diffFileNames, isDiff) = ContractFileComparer.CompareGetContractFilesResponseDto(
            originalFiles, k8sFiles, "TestContract");

        // Assert
        Assert.False(isDiff);
        Assert.Empty(diffFileNames);
    }

    private string EncodeBase64(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return Convert.ToBase64String(bytes);
    }
}