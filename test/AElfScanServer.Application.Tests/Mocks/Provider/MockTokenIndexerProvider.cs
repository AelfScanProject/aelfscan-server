using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.Common.Dtos;
using AElfScanServer.Common.Dtos.Indexer;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;
using AElfScanServer.Common.IndexerPluginProvider;
using AElfScanServer.Common.Token.Provider;

namespace AElfScanServer.Mocks.Provider;

public class MockTokenIndexerProvider : ITokenIndexerProvider
{

    public async Task<IndexerTokenInfoListDto> GetTokenListAsync(TokenListInput input)
    {
        return new IndexerTokenInfoListDto()
        {
            TotalCount = 1,
            Items =
            {
                new IndexerTokenInfoDto
                {
                    Symbol = "ELF",
                    CollectionSymbol = null,
                    TokenName = "Token",
                    TotalSupply = 10000000,
                    Supply = 1000000,
                    Issued = 1000000,
                    Issuer = "Test",
                    Owner = "Test",
                    IsPrimaryToken = false,
                    IsBurnable = false,
                    Decimals = 8,
                    Type = SymbolType.Token,
                    ExternalInfo = null,
                    HolderCount = 100,
                    TransferCount = 200,
                    ItemCount = 0,
                    Metadata = MockUtil.CreateDefaultMetaData()
                }
            }
        };
    }

    public async Task<List<IndexerTokenInfoDto>> GetAllTokenInfosAsync(TokenListInput input)
    {
        return new List<IndexerTokenInfoDto>(){
                new IndexerTokenInfoDto
                {
                    Symbol = "ELF",
                    CollectionSymbol = null,
                    TokenName = "Token",
                    TotalSupply = 10000000,
                    Supply = 1000000,
                    Issued = 1000000,
                    Issuer = "Test",
                    Owner = "Test",
                    IsPrimaryToken = false,
                    IsBurnable = false,
                    Decimals = 8,
                    Type = SymbolType.Token,
                    ExternalInfo = null,
                    HolderCount = 100,
                    TransferCount = 200,
                    ItemCount = 0,
                }
        };
    }

    public async Task<List<IndexerTokenInfoDto>> GetTokenDetailAsync(string chainId, string symbol)
    {
        return new List<IndexerTokenInfoDto>(){
            new IndexerTokenInfoDto
            {
                Symbol = "ELF",
                CollectionSymbol = null,
                TokenName = "Token",
                TotalSupply = 10000000,
                Supply = 1000000,
                Issued = 1000000,
                Issuer = "Test",
                Owner = "Test",
                IsPrimaryToken = false,
                IsBurnable = false,
                Decimals = 8,
                Type = SymbolType.Token,
                ExternalInfo = null,
                HolderCount = 100,
                TransferCount = 200,
                ItemCount = 0,
            }
        };
    }

    public async Task<IndexerTokenTransferListDto> GetTokenTransferInfoAsync(TokenTransferInput input)
    {
        return new IndexerTokenTransferListDto
        {
            TotalCount = 1,
            Items = { new IndexerTransferInfoDto
                {
                    Id = "Id",
                    TransactionId = "TransactionId",
                    From = "From",
                    To = "To",
                    Method = "Transfer",
                    Amount = 100000000,
                    FormatAmount = 1,
                    Token = new IndexerTokenBaseDto
                    {
                        Symbol = "ELF",
                        CollectionSymbol = null,
                        Type = SymbolType.Token,
                        Decimals = 8
                    },
                }
            }
        };
    }

    public async Task<string> GetTokenImageAsync(string symbol, string chainId, List<ExternalInfoDto> externalInfo = null)
    {
        return "http://www.test.jpg";
    }

    public async Task<int> GetAccountCountAsync(string chainId)
    {
        return 10000;
    }

    public async Task<IndexerTokenHolderInfoListDto> GetTokenHolderInfoAsync(TokenHolderInput input)
    {
        return new IndexerTokenHolderInfoListDto
        {
            TotalCount = 1,
            Items = new List<IndexerTokenHolderInfoDto>
            {
               new IndexerTokenHolderInfoDto
               {
                   Id = "Id",
                   Address = "Address",
                   Token =  new IndexerTokenBaseDto
                   {
                       Symbol = "SGR-1",
                       CollectionSymbol = "SGR-0",
                       Type = SymbolType.Nft,
                       Decimals = 4
                   },
                   Amount = 10000,
                   FormatAmount = 1,
                   TransferCount = 200,
                   Metadata = new MetadataDto
                   {
                       ChainId = "AELF",
                       Block = new BlockMetadataDto
                       {
                           BlockHash = "BlockHash",
                           BlockHeight = 100,
                           BlockTime = default
                       }
                   }
               }
            }
        };

    }

    public async Task<List<HolderInfo>> GetHolderInfoAsync(string chainId, string address, List<SymbolType> types)
    {
        return new List<HolderInfo>
        {
            new HolderInfo
            {
                Balance = 100,
                Symbol = "ELF",
                ChainId = "AELF"
            }
        };
    }

    public async Task<HolderInfo> GetHolderInfoAsync(string chainId, string symbol, string address)
    {
        return new HolderInfo
        {
            Balance = 100,
            Symbol = "ELF",
            ChainId = "AELF"
        };
    }

    public async Task<Dictionary<string, IndexerTokenInfoDto>> GetTokenDictAsync(string chainId, List<string> symbols)
    {
        return new Dictionary<string, IndexerTokenInfoDto>()
        {
            {
                "ELF", new IndexerTokenInfoDto
                {
                    Symbol = "ELF",
                    TokenName = "Token",
                    TotalSupply = 1000,
                    Supply = 100,
                    Decimals = 2,
                    Type = SymbolType.Token,
                    HolderCount = 20,
                    TransferCount = 20,
                    Metadata = new MetadataDto
                    {
                        ChainId = "AELF",
                        Block = new BlockMetadataDto
                        {
                            BlockHash = "BlockHash",
                            BlockHeight = 100,
                            BlockTime = default
                        }
                    }
                }
            }
        };
    }

    public async Task<TokenTransferInfosDto> GetTokenTransfersAsync(TokenTransferInput input)
    {
        return new TokenTransferInfosDto
        {
            Total = 2,
            List = new List<TokenTransferInfoDto>()
            {
                new TokenTransferInfoDto
                {
                    ChainId = "AELF",
                    TransactionId = "TransactionId",
                    Method = "Transfer",
                    BlockHeight = 100,
                    BlockTime = 0,
                    Symbol = "ELF",
                    SymbolName = "Token",
                    SymbolImageUrl = "test.jpg",
                    From = new CommonAddressDto
                    {
                        Name = "From",
                        Address = "From",
                        AddressType = AddressType.EoaAddress,
                        IsManager = false,
                        IsProducer = false
                    },
                    To = new CommonAddressDto
                    {
                        Name = "To",
                        Address = "To",
                        AddressType = AddressType.ContractAddress,
                        IsManager = false,
                        IsProducer = false
                    },
                    Quantity = 10,
                    Status = TransactionStatus.Mined,
                    TransactionFeeList = new List<TransactionFeeDto>
                    {
                        new TransactionFeeDto
                        {
                            Symbol = "Elf",
                            Amount = 10,
                            AmountOfUsd = 10
                        }
                    }
                }
            },
            IsAddress = false,
            Balance = 0,
            Value = 0
        };
    }

    public async Task<List<BlockBurnFeeDto>> GetBlockBurntFeeListAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        return new List<BlockBurnFeeDto>
        {
            new BlockBurnFeeDto
            {
                Symbol = "ELF",
                Amount = 20,
                BlockHeight = 100
            }
        };
    }

    public Task<IndexerTokenHolderInfoListDto> GetCollectionHolderInfoAsync(TokenHolderInput input)
    {
        throw new System.NotImplementedException();
    }

    public Task<Dictionary<string, IndexerTokenInfoDto>> GetNftDictAsync(string chainId, List<string> symbols)
    {
        throw new System.NotImplementedException();
    }
}