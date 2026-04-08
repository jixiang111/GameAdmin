using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Admin.NET.Application.GmRpc;
using Furion.DependencyInjection;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Mvc;

namespace Admin.NET.Application.Service;

[ApiDescriptionSettings(Order = 622, Description = "GM 玩家道具调整")]
public class GameServerItemAdjustService : IDynamicApiController, ITransient
{
    private readonly ICenterServerRpcClient _centerServerRpcClient;
    private readonly UserManager _userManager;

    public GameServerItemAdjustService(ICenterServerRpcClient centerServerRpcClient, UserManager userManager)
    {
        _centerServerRpcClient = centerServerRpcClient;
        _userManager = userManager;
    }

    [DisplayName("提交玩家道具调整")]
    [ApiDescriptionSettings(Name = "Submit"), HttpPost]
    public async Task<GameServerItemAdjustOutput> Submit(GameServerItemAdjustInput input)
    {
        if (input.Delta == 0)
            throw Oops.Oh("Delta 不能为 0");

        if (_userManager.UserId <= 0)
            throw Oops.Oh("未获取到当前操作人信息");

        var reasonCode = input.ReasonCode?.Trim();
        if (string.IsNullOrWhiteSpace(reasonCode))
            throw Oops.Oh("ReasonCode 不能为空");

        var operatorName = string.IsNullOrWhiteSpace(_userManager.RealName) ? _userManager.Account : _userManager.RealName;
        if (string.IsNullOrWhiteSpace(operatorName))
            operatorName = _userManager.UserId.ToString();

        var traceId = NormalizeTraceId(input.TraceId);
        var remark = input.Remark?.Trim() ?? string.Empty;

        var response = await _centerServerRpcClient.AdjustPlayerItemAsync(new CenterGmAdjustPlayerItemReq
        {
            ServerId = input.ServerId,
            Request = new GmAdjustPlayerItemReq
            {
                RoleId = input.RoleId,
                ItemId = input.ItemId,
                Delta = input.Delta,
                ReasonCode = reasonCode,
                Remark = remark,
                OperatorId = _userManager.UserId,
                OperatorName = operatorName,
                TraceId = traceId
            }
        });

        return new GameServerItemAdjustOutput
        {
            ServerId = input.ServerId,
            RoleId = response.RoleId,
            ItemId = response.ItemId,
            AppliedDelta = response.AppliedDelta,
            BeforeNum = response.BeforeNum,
            AfterNum = response.AfterNum,
            ChangeTimeTicks = response.ChangeTimeTicks,
            TraceId = traceId,
            ReasonCode = reasonCode,
            Remark = remark,
            OperatorId = _userManager.UserId,
            OperatorName = operatorName
        };
    }

    private static string NormalizeTraceId(string? traceId)
    {
        var normalized = traceId?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
            return normalized;

        return $"gm-item-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..31];
    }
}
