using System.Collections.Generic;
using System.Linq;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.Common.Dtos.Input;

public class BaseInput : OrderInfo
{
    public string ChainId { get; set; } = "";
    public long SkipCount { get; set; }
    public long MaxResultCount { get; set; } = 10;
    public List<OrderInfo> OrderInfos { get; set; }
    public List<string> SearchAfter { get; set; }

    public void OfOrderInfos(params (SortField sortField, SortDirection sortDirection)[] orderInfos)
    {
        OrderInfos = BuildOrderInfos(orderInfos);
    }
}

public class OrderInfo
{
    public string OrderBy { get; set; }

    public string Sort { get; set; }

    public static List<OrderInfo> BuildOrderInfos(
        params (SortField sortField, SortDirection sortDirection)[] orderInfos)
    {
        return orderInfos.Select(info => new OrderInfo
        {
            OrderBy = info.sortField.ToString(),
            Sort = info.sortDirection.ToString()
        }).ToList();
    }
}