// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 客户端资源 OSS 凭证配置
/// </summary>
[SugarTable(null, "客户端资源 OSS 凭证")]
[SysTable]
[SugarIndex("idx_client_oss_provider_default", nameof(Provider), OrderByType.Asc, nameof(IsDefault), OrderByType.Desc)]
public sealed class ClientResourceOssConfig : EntityBase
{
    /// <summary>
    /// 存储提供商（目前仅支持 aliyun）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储提供商", Length = 32)]
    [Required, MaxLength(32)]
    public string Provider { get; set; } = "aliyun";

    /// <summary>
    /// AccessKeyId
    /// </summary>
    [SugarColumn(ColumnDescription = "AccessKeyId", Length = 128)]
    [Required, MaxLength(128)]
    public string AccessKeyId { get; set; }

    /// <summary>
    /// AccessKeySecret（加密存储）
    /// </summary>
    [SugarColumn(ColumnDescription = "AccessKeySecret", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    [Required]
    public string AccessKeySecretCipher { get; set; }

    /// <summary>
    /// Endpoint
    /// </summary>
    [SugarColumn(ColumnDescription = "Endpoint", Length = 256)]
    [Required, MaxLength(256)]
    public string Endpoint { get; set; }

    /// <summary>
    /// 默认 Bucket
    /// </summary>
    [SugarColumn(ColumnDescription = "默认Bucket", Length = 128)]
    [Required, MaxLength(128)]
    public string BucketName { get; set; }

    /// <summary>
    /// 根路径（默认 /）
    /// </summary>
    [SugarColumn(ColumnDescription = "根路径", Length = 256)]
    [MaxLength(256)]
    public string RootPath { get; set; } = "/";

    /// <summary>
    /// 资源域名（可选）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源域名", Length = 256, IsNullable = true)]
    [MaxLength(256)]
    public string? ResourceDomain { get; set; }

    /// <summary>
    /// 区域
    /// </summary>
    [SugarColumn(ColumnDescription = "区域", Length = 64, IsNullable = true)]
    [MaxLength(64)]
    public string? Region { get; set; }

    /// <summary>
    /// 是否默认配置
    /// </summary>
    [SugarColumn(ColumnDescription = "是否默认", DefaultValue = "0")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnDescription = "描述", Length = 512, IsNullable = true)]
    [MaxLength(512)]
    public string? Remark { get; set; }
}
