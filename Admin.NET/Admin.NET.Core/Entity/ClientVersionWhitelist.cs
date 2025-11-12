// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 客户端版本测试白名单
/// </summary>
[SugarTable(null, "客户端版本测试白名单")]
[SysTable]
[SugarIndex("idx_client_whitelist_machine", nameof(MachineCode), OrderByType.Asc, IsUnique = true)]
[SugarIndex("idx_client_whitelist_enabled", nameof(Enabled), OrderByType.Desc)]
public sealed class ClientVersionWhitelist : EntityBase
{
    /// <summary>
    /// 机器码（唯一）
    /// </summary>
    [SugarColumn(ColumnDescription = "机器码", Length = 128)]
    [Required, MaxLength(128)]
    public string MachineCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 512, IsNullable = true)]
    [MaxLength(512)]
    public string? Remark { get; set; }

    /// <summary>
    /// 标签 JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "标签 JSON", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public string? TagsJson { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用", DefaultValue = "1")]
    public bool Enabled { get; set; } = true;
}
