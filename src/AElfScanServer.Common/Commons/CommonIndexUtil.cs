using System;

namespace AElfScanServer.Common.Commons;

public class CommonIndexUtil
{
    public static string GetIndexName(string collectionPrefix, string indexName)
    {
        if (collectionPrefix.IsNullOrEmpty())
        {
            return indexName;
        }

        return string.Join(".", collectionPrefix, indexName);
    }
}