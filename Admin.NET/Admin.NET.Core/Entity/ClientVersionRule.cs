// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 渠道客户端版本规则
/// </summary>
[SugarTable(null, "渠道客户端版本规则")]
[SysTable]
[SugarIndex("idx_client_version_channel", nameof(ChannelId), OrderByType.Asc, nameof(Platform), OrderByType.Asc, IsUnique = true)]
public sealed class ClientVersionRule : EntityBase
{
    /// <summary>
    /// 渠道 ID
    /// </summary>
    [SugarColumn(ColumnDescription = "渠道ID", Length = 64)]
    [Required, MaxLength(64)]
    public string ChannelId { get; set; }

    /// <summary>
    /// 渠道名称
    /// </summary>
    [SugarColumn(ColumnDescription = "渠道名称", Length = 128)]
    [Required, MaxLength(128)]
    public string ChannelName { get; set; }

    /// <summary>
    /// 平台
    /// </summary>
    [SugarColumn(ColumnDescription = "平台", Length = 32)]
    [Required]
    public ClientPlatform Platform { get; set; }

    /// <summary>
    /// 资源桶
    /// </summary>
    [SugarColumn(ColumnDescription = "资源桶", Length = 128)]
    [Required, MaxLength(128)]
    public string BucketName { get; set; }

    /// <summary>
    /// 根目录
    /// </summary>
    [SugarColumn(ColumnDescription = "根目录", Length = 256)]
    [MaxLength(256)]
    public string RootPath { get; set; } = "/";

    /// <summary>
    /// 自定义资源域名
    /// </summary>
    [SugarColumn(ColumnDescription = "资源域名", Length = 256, IsNullable = true)]
    [MaxLength(256)]
    public string? ResourceDomain { get; set; }

    /// <summary>
    /// 正式用户版本信息（JSON）
    /// </summary>
    [SugarColumn(ColumnDescription = "正式用户版本信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public string? GeneralVersionInfoJson { get; set; }

    /// <summary>
    /// 白名单版本信息（JSON）
    /// </summary>
    [SugarColumn(ColumnDescription = "白名单版本信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public string? WhitelistVersionInfoJson { get; set; }

    /// <summary>
    /// 最近一次发布资源版本
    /// </summary>
    [SugarColumn(ColumnDescription = "最近一次发布资源版本", Length = 128, IsNullable = true)]
    [MaxLength(128)]
    public string? LatestPublishResVersion { get; set; }

    /// <summary>
    /// 最近发布时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最近发布时间", IsNullable = true)]
    public DateTime? LatestPublishTime { get; set; }

    /// <summary>
    /// 最近发布人 Id
    /// </summary>
    [SugarColumn(ColumnDescription = "最近发布人Id", IsNullable = true)]
    public long? LatestPublishUserId { get; set; }

    /// <summary>
    /// 最近发布人名称
    /// </summary>
    [SugarColumn(ColumnDescription = "最近发布人名称", Length = 64, IsNullable = true)]
    [MaxLength(64)]
    public string? LatestPublishUserName { get; set; }
}
