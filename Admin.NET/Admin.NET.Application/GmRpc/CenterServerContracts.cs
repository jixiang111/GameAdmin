// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵守 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 与 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。

using System.Collections.Generic;
using MagicOnion;
using MessagePack;

namespace Admin.NET.Application.GmRpc;
public interface ICenterGmService : IService<ICenterGmService>
{
    UnaryResult<CenterServerListRes> GetGameServerList();
    UnaryResult<CenterServerOpRes> UpdateGameServer(CenterServerUpdateReq request);
    UnaryResult<CenterGmQueryItemChangeLogRes> QueryItemChangeLogs(CenterGmQueryItemChangeLogReq request);

    UnaryResult<CenterVersionRuleListRes> ListAppVersionRules();
    UnaryResult<CenterVersionRuleOpRes> UpsertAppVersionRule(CenterVersionRuleReq request);
    UnaryResult<CenterVersionRuleOpRes> DeleteAppVersionRule(string ruleId);
}

[MessagePackObject(true)]
public class CenterServerListRes
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public IReadOnlyList<CenterServerInfoDto> Items { get; set; } = Array.Empty<CenterServerInfoDto>();
}

[MessagePackObject(true)]
public class CenterServerOpRes
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public CenterServerInfoDto Data { get; set; }
}

[MessagePackObject(true)]
public class CenterServerInfoDto
{
    public int ServerId { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }
    public int TcpPort { get; set; }
    public string WebSocketUrl { get; set; }
    public int GrpcPort { get; set; }
}

[MessagePackObject(true)]
public class CenterServerUpdateReq
{
    public int ServerId { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }
    public int? TcpPort { get; set; }
    public string WebSocketUrl { get; set; }
    public int? GrpcPort { get; set; }
}

[MessagePackObject(true)]
public class CenterGmQueryItemChangeLogReq
{
    public int ServerId { get; set; }
    public GmQueryItemChangeLogReq Query { get; set; } = new();
}

[MessagePackObject(true)]
public class CenterGmQueryItemChangeLogRes
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int ServerId { get; set; }
    public GmQueryItemChangeLogRes Data { get; set; } = new();
}

[MessagePackObject(true)]
public class GmQueryItemChangeLogReq
{
    public long RoleId { get; set; }
    public int ItemId { get; set; }
    public ItemChangeSourceType SourceType { get; set; } = ItemChangeSourceType.Unknown;
    public ItemChangeOperationType OperationType { get; set; } = ItemChangeOperationType.Unknown;
    public long OperatorId { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public long StartTimeTicksUtc { get; set; }
    public long EndTimeTicksUtc { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

[MessagePackObject(true)]
public class GmQueryItemChangeLogRes
{
    public int Total { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public IReadOnlyList<GmItemChangeLogDto> Items { get; set; } = Array.Empty<GmItemChangeLogDto>();
}

[MessagePackObject(true)]
public class GmItemChangeLogDto
{
    public long LogId { get; set; }
    public long RoleId { get; set; }
    public int ItemId { get; set; }
    public long BeforeNum { get; set; }
    public long AfterNum { get; set; }
    public long Delta { get; set; }
    public ItemChangeSourceType SourceType { get; set; } = ItemChangeSourceType.Unknown;
    public ItemChangeOperationType OperationType { get; set; } = ItemChangeOperationType.Unknown;
    public string ReasonCode { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string RequestUniId { get; set; } = string.Empty;
    public long ChangeTimeTicks { get; set; }
}

public enum ItemChangeSourceType
{
    Unknown = 0,
    Client = 1,
    Gm = 2,
    System = 3
}

public enum ItemChangeOperationType
{
    Unknown = 0,
    Adjust = 1,
    Use = 2,
    Expire = 3,
    Sell = 4,
    GmAdjust = 5
}

[MessagePackObject(true)]
public class CenterVersionRuleListRes
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public IReadOnlyList<CenterVersionRuleDto> Items { get; set; } = Array.Empty<CenterVersionRuleDto>();
}

[MessagePackObject(true)]
public class CenterVersionRuleOpRes
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public CenterVersionRuleDto Data { get; set; }
}

[MessagePackObject(true)]
public class CenterVersionRuleDto
{
    public string RuleId { get; set; }
    public string ChannelId { get; set; }
    public string ChannelName { get; set; }
    public string Platform { get; set; }
    public ClientVersionRuleType RuleType { get; set; } = ClientVersionRuleType.General;
    public IReadOnlyList<string> WhitelistMachineCodes { get; set; } = Array.Empty<string>();
    public AppVersionInfo VersionInfo { get; set; }
}

[MessagePackObject(true)]
public class CenterVersionRuleReq
{
    public string RuleId { get; set; }
    public string ChannelId { get; set; }
    public string ChannelName { get; set; }
    public string Platform { get; set; }
    public ClientVersionRuleType RuleType { get; set; } = ClientVersionRuleType.General;
    public IReadOnlyList<string> WhitelistMachineCodes { get; set; } = Array.Empty<string>();
    public AppVersionInfo VersionInfo { get; set; }
}

[MessagePackObject(true)]
public class AppVersionInfo
{
    public string AppVersion { get; set; } = string.Empty;
    public string ResVersion { get; set; } = string.Empty;
    public string UpdateUrl { get; set; } = string.Empty;
    public List<string> UpdateResUrls { get; set; } = new();
    public string ChannelName { get; set; } = string.Empty;
    public string UpdateNotice { get; set; } = string.Empty;
    public string LoginUrl { get; set; } = string.Empty;
}

public enum ClientVersionRuleType
{
    General = 0,
    Whitelist = 1
}
