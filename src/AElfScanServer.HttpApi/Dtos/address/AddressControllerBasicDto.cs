using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElfScanServer.Common.Dtos.Input;
using AElfScanServer.Common.Enums;
using Volo.Abp.Application.Dtos;

namespace AElfScanServer.HttpApi.Dtos.address;

public class GetDetailBasicDto
{
    public string ChainId { get; set; } = "";
}

public class GetListInputBasicDto : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }

    public List<OrderInfo> OrderInfos { get; set; }

    public List<string> SearchAfter { get; set; }

    public void OfOrderInfos(params (SortField sortField, SortDirection sortDirection)[] orderInfos)
    {
        OrderInfos = OrderInfo.BuildOrderInfos(orderInfos);
    }
}