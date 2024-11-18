using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos;

public enum SymbolType
{
    Token,
    Nft,
    Nft_Collection
}

public enum AddressType
{
    EoaAddress,
    ContractAddress
}

public enum TransferStatusType
{
    In,
    Out
}

public class CommonAddressDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public AddressType AddressType { get; set; }
    public bool IsManager { get; set; }
    public bool IsProducer { get; set; }
}

public class AddressAssetDto
{
    public string Address { get; set; }

    public double TotalTokenValueOfElf { get; set; }

    public double TotalNftValueOfElf { get; set; }

    public int Count { get; set; }
    public HashSet<string> SymbolSet = new();
    
    public double GetTotalValueOfElf()
    {
        return TotalTokenValueOfElf + TotalNftValueOfElf;
    }

    public AddressAssetDto()
    {
    }

    public void Accumulate(AddressAssetDto assetDto)
    {
        TotalTokenValueOfElf += assetDto.TotalTokenValueOfElf;
        TotalNftValueOfElf += assetDto.TotalNftValueOfElf;
    }

    public AddressAssetDto(string address, double totalTokenValueOfElf, double totalNftValueOfElf)
    {
        Address = address;
        TotalTokenValueOfElf = totalTokenValueOfElf;
        TotalNftValueOfElf = totalNftValueOfElf;
    }
}