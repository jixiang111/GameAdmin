// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.ComponentModel.DataAnnotations;

namespace Admin.NET.Application.Service.ClientVersion.Dto;

#region OSS 配置

public sealed class ClientOssConfigQueryInput
{
    public string? Provider { get; set; }
}

public sealed class ClientOssConfigInput
{
    [Required, MaxLength(32)]
    public string Provider { get; set; } = "aliyun";

    [Required, MaxLength(128)]
    public string AccessKeyId { get; set; }

    /// <summary>
    /// AccessKeySecret，可以为空（表示沿用旧值）
    /// </summary>
    [MaxLength(256)]
    public string? AccessKeySecret { get; set; }

    [Required, MaxLength(256)]
    public string Endpoint { get; set; }

    [Required, MaxLength(128)]
    public string BucketName { get; set; }

    [MaxLength(256)]
    public string? RootPath { get; set; }

    [MaxLength(256)]
    public string? ResourceDomain { get; set; }

    [MaxLength(64)]
    public string? Region { get; set; }

    public bool IsDefault { get; set; } = true;

    [MaxLength(512)]
    public string? Remark { get; set; }
}

public sealed class ClientOssConfigOutput
{
    public long Id { get; set; }
    public string Provider { get; set; }
    public string AccessKeyId { get; set; }
    public string AccessKeySecretMasked { get; set; }
    public string Endpoint { get; set; }
    public string BucketName { get; set; }
    public string RootPath { get; set; }
    public string? ResourceDomain { get; set; }
    public string? Region { get; set; }
    public bool IsDefault { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class ClientOssConfigSummaryOutput
{
    public long Id { get; set; }
    public string Provider { get; set; }
    public string BucketName { get; set; }
    public string RootPath { get; set; }
    public string? ResourceDomain { get; set; }
    public bool IsDefault { get; set; }
    public string? Remark { get; set; }
}

#endregion

#region 白名单

public sealed class ClientWhitelistPageInput : BasePageInput
{
    /// <summary>
    /// 仅查看启用
    /// </summary>
    public bool? OnlyEnabled { get; set; }
}

public sealed class ClientWhitelistUpsertInput
{
    public long? EntryId { get; set; }

    [Required, MaxLength(128)]
    public string MachineCode { get; set; }

    [MaxLength(512)]
    public string? Remark { get; set; }

    public List<string>? Tags { get; set; }

    public bool Enabled { get; set; } = true;
}

public sealed class ClientWhitelistDeleteInput
{
    [Required]
    public long EntryId { get; set; }
}

public sealed class ClientWhitelistQueryInput
{
    public List<long>? EntryIds { get; set; }
    public bool? OnlyEnabled { get; set; }
}

public sealed class ClientWhitelistOutput
{
    public long EntryId { get; set; }
    public string MachineCode { get; set; }
    public string? Remark { get; set; }
    public List<string>? Tags { get; set; }
    public bool Enabled { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}

#endregion

#region 版本规则

public sealed class ClientVersionRuleQueryInput
{
    public string? ChannelId { get; set; }
    public ClientPlatform? Platform { get; set; }
}

public sealed class ClientVersionRuleSaveInput
{
    public long? RuleId { get; set; }

    [Required, MaxLength(64)]
    public string ChannelId { get; set; }

    [Required, MaxLength(128)]
    public string ChannelName { get; set; }

    [Required]
    public ClientPlatform Platform { get; set; }

    [MaxLength(32)]
    public string? Provider { get; set; } = "aliyun";

    public long? OssConfigId { get; set; }

    /// <summary>
    /// 可选，留空则从 OssConfigId 对应的配置中读取
    /// </summary>
    [MaxLength(128)]
    public string? BucketName { get; set; }

    [MaxLength(256)]
    public string? RootPath { get; set; }

    [MaxLength(256)]
    public string? ResourceDomain { get; set; }

    public ClientAppVersionInfoDto? GeneralVersionInfo { get; set; }

    public ClientAppVersionInfoDto? WhitelistVersionInfo { get; set; }

    public List<long>? WhitelistTesterEntryIds { get; set; }
}

public sealed class ClientVersionRuleDeleteInput
{
    [Required]
    public long RuleId { get; set; }
}

public sealed class PromoteWhitelistVersionInput
{
    [Required]
    public long RuleId { get; set; }

    /// <summary>
    /// 可指定资源版本，否则使用白名单版本
    /// </summary>
    public string? TargetResVersion { get; set; }

    [Required]
    public bool Confirm { get; set; }
}

public sealed class ClientVersionRuleOutput
{
    public long RuleId { get; set; }
    public string ChannelId { get; set; }
    public string ChannelName { get; set; }
    public ClientPlatform Platform { get; set; }
    public string Provider { get; set; }
    public long? OssConfigId { get; set; }
    public string BucketName { get; set; }
    public string RootPath { get; set; }
    public string? ResourceDomain { get; set; }
    public ClientAppVersionInfoDto? GeneralVersionInfo { get; set; }
    public ClientAppVersionInfoDto? WhitelistVersionInfo { get; set; }
    public List<long> WhitelistTesterEntryIds { get; set; } = new();
    public List<string> WhitelistMachineCodes { get; set; } = new();
    public DateTime? LatestPublishTime { get; set; }
    public string? LatestPublishUserName { get; set; }
    public string? LatestPublishResVersion { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}

public sealed class ClientAppVersionInfoDto
{
    [Required, MaxLength(64)]
    public string AppVersion { get; set; }

    [Required, MaxLength(256)]
    public string ResVersion { get; set; }

    public long? ResVersionTimestamp { get; set; }

    public long? ResVersionFileSize { get; set; }

    [MaxLength(512)]
    public string? UpdateUrl { get; set; }

    public List<string>? UpdateResUrls { get; set; }

    [MaxLength(1024)]
    public string? UpdateNotice { get; set; }

    [MaxLength(512)]
    public string? LoginUrl { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool ForceUpdate { get; set; }
}

public class ChannelAppVersionQuery
{
    [Required]
    public string ChannelId { get; set; }

    [Required]
    public ClientPlatform Platform { get; set; }

    public string? Provider { get; set; }

    public long? OssConfigId { get; set; }

    /// <summary>
    /// 可选：当规则尚未保存时传递临时桶信息
    /// </summary>
    public string? BucketName { get; set; }

    public string? RootPath { get; set; }
}

public sealed class ChannelResVersionQuery : ChannelAppVersionQuery
{
    [Required, MaxLength(64)]
    public string AppVersion { get; set; }
}

public sealed class ChannelAppVersionItem
{
    public string AppVersion { get; set; }
    public string Folder { get; set; }
    public DateTime? LastModified { get; set; }
}

public sealed class ChannelResVersionItem
{
    public string FileName { get; set; }
    public long? Timestamp { get; set; }
    public long? FileSize { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsNewerThanOnline { get; set; }
    public string DownloadUrl { get; set; }
}

#endregion
