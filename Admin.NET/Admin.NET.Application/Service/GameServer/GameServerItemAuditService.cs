using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Admin.NET.Application.GmRpc;
using Furion.DependencyInjection;
using Furion.FriendlyException;

namespace Admin.NET.Application.Service;

[ApiDescriptionSettings(Order = 621, Description = "GM 道具流水查询")]
public class GameServerItemAuditService : IDynamicApiController, ITransient
{
    private readonly ICenterServerRpcClient _centerServerRpcClient;

    public GameServerItemAuditService(ICenterServerRpcClient centerServerRpcClient)
    {
        _centerServerRpcClient = centerServerRpcClient;
    }

    [DisplayName("分页查询指定玩家的道具变化流水")]
    [ApiDescriptionSettings(Name = "Page"), HttpGet]
    public async Task<GameServerItemAuditPageOutput> Page([FromQuery] GameServerItemAuditQueryInput input)
    {
        if (input.StartTimeTicksUtc.HasValue && input.EndTimeTicksUtc.HasValue &&
            input.StartTimeTicksUtc.Value > input.EndTimeTicksUtc.Value)
        {
            throw Oops.Oh("开始时间不能大于结束时间");
        }

        var page = Math.Max(input.Page, 1);
        var pageSize = Math.Clamp(input.PageSize, 1, 100);

        var response = await _centerServerRpcClient.QueryItemChangeLogsAsync(new CenterGmQueryItemChangeLogReq
        {
            ServerId = input.ServerId,
            Query = new GmQueryItemChangeLogReq
            {
                RoleId = input.RoleId,
                ItemId = input.ItemId ?? 0,
                SourceType = input.SourceType ?? ItemChangeSourceType.Unknown,
                OperationType = input.OperationType ?? ItemChangeOperationType.Unknown,
                OperatorId = input.OperatorId ?? 0,
                TraceId = input.TraceId?.Trim() ?? string.Empty,
                ReasonCode = input.ReasonCode?.Trim() ?? string.Empty,
                StartTimeTicksUtc = input.StartTimeTicksUtc ?? 0,
                EndTimeTicksUtc = input.EndTimeTicksUtc ?? 0,
                PageIndex = page,
                PageSize = pageSize
            }
        });

        var currentPage = response.PageIndex > 0 ? response.PageIndex : page;
        var currentPageSize = response.PageSize > 0 ? response.PageSize : pageSize;
        var totalPages = currentPageSize > 0 ? (int)Math.Ceiling(response.Total / (double)currentPageSize) : 0;

        return new GameServerItemAuditPageOutput
        {
            ServerId = response.ServerId > 0 ? response.ServerId : input.ServerId,
            ServerName = response.ServerName ?? string.Empty,
            Page = currentPage,
            PageSize = currentPageSize,
            Total = response.Total,
            TotalPages = totalPages,
            HasMore = response.HasMore,
            HasNextPage = response.HasMore || (totalPages > 0 && currentPage < totalPages),
            HasPrevPage = currentPage > 1,
            Items = (response.Items ?? Array.Empty<GmItemChangeLogDto>()).Select(MapItem).ToList()
        };
    }

    private static GameServerItemAuditOutput MapItem(GmItemChangeLogDto item)
    {
        return new GameServerItemAuditOutput
        {
            LogId = item.LogId,
            RoleId = item.RoleId,
            ItemId = item.ItemId,
            BeforeNum = item.BeforeNum,
            AfterNum = item.AfterNum,
            Delta = item.Delta,
            SourceType = item.SourceType,
            OperationType = item.OperationType,
            ReasonCode = item.ReasonCode ?? string.Empty,
            Remark = item.Remark ?? string.Empty,
            OperatorId = item.OperatorId,
            OperatorName = item.OperatorName ?? string.Empty,
            TraceId = item.TraceId ?? string.Empty,
            RequestUniId = item.RequestUniId ?? string.Empty,
            ChangeTimeTicks = item.ChangeTimeTicks
        };
    }
}
