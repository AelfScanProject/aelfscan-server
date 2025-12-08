using System.Collections.Generic;

namespace AElfScanServer.Common.Dtos.Indexer;

public class IndexerDailyHolderDto
{
    public List<DailyHolderDto> DailyHolder { get; set; }
}

public class DailyHolderDto
{
    public string DateStr { get; set; }
    public long Date { get; set; }
    public long Count { get; set; }
}