using System;
using AElfScanServer.Common.Dtos;

namespace AElfScanServer.Mocks.Provider;

public class MockUtil
{
    public static readonly string MainChainId = "AELF";

    public static MetadataDto CreateDefaultMetaData()
    {
        return new MetadataDto
        {
            ChainId = MainChainId,
            Block = new BlockMetadataDto
            {
                BlockHash = "BlockHash",
                BlockHeight = 100,
                BlockTime = DateTime.UtcNow
            }
        };
    }
}