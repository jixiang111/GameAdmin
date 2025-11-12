// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵守 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 与 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Admin.NET.Application.GmRpc;
using Furion.DependencyInjection;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Mvc;

namespace Admin.NET.Application.Service;

[ApiDescriptionSettings(Order = 620, Description = "GM 游戏服地址管理")]
public class GameServerAddressService : IDynamicApiController, ITransient
{
    private readonly ICenterServerRpcClient _centerServerRpcClient;

    public GameServerAddressService(ICenterServerRpcClient centerServerRpcClient)
    {
        _centerServerRpcClient = centerServerRpcClient;
    }

    [DisplayName("获取游戏服地址列表")]
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    public async Task<List<GameServerAddressOutput>> List()
    {
        var servers = await _centerServerRpcClient.GetGameServerListAsync() ?? Array.Empty<CenterServerInfoDto>();
        return servers
            .Select(MapToOutput)
            .OrderBy(u => u.ServerId)
            .ToList();
    }

    [DisplayName("获取游戏服地址详情")]
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    public async Task<GameServerAddressOutput> Detail([FromQuery] GameServerAddressDetailInput input)
    {
        var servers = await _centerServerRpcClient.GetGameServerListAsync() ?? Array.Empty<CenterServerInfoDto>();
        var target = servers.FirstOrDefault(u => u.ServerId == input.ServerId);
        if (target == null) throw Oops.Oh($"未找到 ServerId={input.ServerId} 的游戏服");
        return MapToOutput(target);
    }

    [DisplayName("新增游戏服地址")]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    public async Task<GameServerAddressOutput> Add(GameServerAddressCreateInput input)
    {
        return await UpsertInternalAsync(input);
    }

    [DisplayName("更新游戏服地址")]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    public async Task<GameServerAddressOutput> Update(GameServerAddressUpdateInput input)
    {
        return await UpsertInternalAsync(input);
    }

    [DisplayName("删除游戏服地址")]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    public async Task Delete(GameServerAddressDeleteInput input)
    {
        await _centerServerRpcClient.UpsertGameServerAsync(new CenterServerUpdateReq
        {
            ServerId = input.ServerId
        });
    }

    private async Task<GameServerAddressOutput> UpsertInternalAsync(GameServerAddressMutationInput input)
    {
        var ip = input.Ip?.Trim();
        var wsUrl = BuildWebSocketUrl(input, ip);

        var request = new CenterServerUpdateReq
        {
            ServerId = input.ServerId,
            Name = input.ServerName?.Trim(),
            Ip = ip,
            TcpPort = input.Port > 0 ? input.Port : null,
            WebSocketUrl = string.IsNullOrWhiteSpace(wsUrl) ? null : wsUrl,
            GrpcPort = input.GrpcPort > 0 ? input.GrpcPort : null
        };

        var response = await _centerServerRpcClient.UpsertGameServerAsync(request);
        if (response == null) throw Oops.Oh("CenterServer 未返回游戏服详情，请检查 GM RPC");

        return MapToOutput(response);
    }

    private static GameServerAddressOutput MapToOutput(CenterServerInfoDto dto)
    {
        var ip = dto.Ip ?? string.Empty;
        var wsUrl = dto.WebSocketUrl ?? string.Empty;

        return new GameServerAddressOutput
        {
            ServerId = dto.ServerId,
            ServerName = dto.Name ?? string.Empty,
            Ip = ip,
            Port = dto.TcpPort,
            WsUrl = wsUrl,
            WsPort = ExtractPort(wsUrl),
            GrpcPort = dto.GrpcPort,
            GrpcUrl = BuildGrpcUrl(ip, dto.GrpcPort),
            Remark = string.Empty,
            Enabled = true
        };
    }

    private static string BuildWebSocketUrl(GameServerAddressMutationInput input, string ip)
    {
        var wsUrl = input.WsUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(wsUrl))
            return wsUrl;

        if (input.WsPort is > 0 && !string.IsNullOrWhiteSpace(ip))
            return $"ws://{ip}:{input.WsPort}";

        return string.Empty;
    }

    private static string BuildGrpcUrl(string ip, int grpcPort)
    {
        if (grpcPort <= 0 || string.IsNullOrWhiteSpace(ip)) return string.Empty;
        return $"http://{ip}:{grpcPort}";
    }

    private static int ExtractPort(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return 0;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsDefaultPort ? uri.Port : 0;
    }
}
