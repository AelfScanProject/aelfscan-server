using System.Collections.Generic;
using System.Linq;
using AElfScanServer.Common.Dtos.ChartData;

namespace AElfScanServer.HttpApi.Dtos.ChartData;

public class ChartDataRequest
{
    public long StartDate { get; set; } = 0;
    public long EndDate { get; set; } = 0;
    public string ChainId { get; set; }
}

public class SetRoundRequest
{
    public string ChainId { get; set; }

    public string StartDate { get; set; }
    public string EndDate { get; set; }

    public int SetNumber { get; set; }

    public bool UpdateData { get; set; }
}

public class InitRoundResp
{
    public long MinRound { get; set; }

    public long MaxRound { get; set; }

    public long RoundCount { get; set; }

    public string MinDate { get; set; }

    public List<string> UpdateDate { get; set; }


    public List<string> UpdateDateNodeBlockProduce { get; set; }

    public string MaxDate { get; set; }
    public string FinishDate { get; set; }
}

public class DailyTransactionCountResp
{
    public long Total { get; set; } 
    public List<DailyTransactionCount> List { get; set; }

    public DailyTransactionCount HighestTransactionCount { get; set; }
    public DailyTransactionCount LowesTransactionCount { get; set; }
    public string ChainId { get; set; }
}