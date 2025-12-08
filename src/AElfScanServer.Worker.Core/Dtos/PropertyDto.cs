using System.Collections.Generic;
using Nest;

namespace AElfScanServer.Worker.Core.Dtos;

public class SearchResult
{
    [PropertyName("aggregations")] public Aggregations Aggregations { get; set; }
}

public class Aggregations
{
    [PropertyName("field1_list")] public Field1List Field1List { get; set; }
}

public class Field1List
{
    [PropertyName("buckets")] public List<Bucket> Buckets { get; set; }
}

public class Bucket
{
    [PropertyName("key")] public int BlockHeight { get; set; }
}