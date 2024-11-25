using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.HttpApi.Dtos.address;
using AElfScanServer.HttpApi.Provider;


namespace AElfScanServer.Mocks.Provider;

public class MockDecompilerProvider : IDecompilerProvider
{
    public async Task<GetContractFilesResponseDto> GetFilesAsync(string base64String)
    {
        return new GetContractFilesResponseDto
        {
            Code = 200,
            Msg = "",
            Version = "1",
            Data = new List<DecompilerContractFileDto>()
            {
                new DecompilerContractFileDto
                {
                    Name = "Name",
                    Content = "Content",
                    FileType = "File",
                    Files = new List<DecompilerContractFileDto>()
                }
            }
        };
    }
}