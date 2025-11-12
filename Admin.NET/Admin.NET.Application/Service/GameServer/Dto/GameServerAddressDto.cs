// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵守 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 与 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Admin.NET.Application.Service;

public sealed class GameServerAddressOutput
{
    public int ServerId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string Ip { get; set; } = string.Empty;

    public int Port { get; set; }

    public int WsPort { get; set; }

    public string WsUrl { get; set; } = string.Empty;

    public int GrpcPort { get; set; }

    public string GrpcUrl { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public bool Enabled { get; set; }
}

public abstract class GameServerAddressMutationInput
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ServerId 必须大于 0")]
    public int ServerId { get; set; }

    [Required]
    [MaxLength(64)]
    public string ServerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Ip { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    [Range(0, 65535)]
    public int? WsPort { get; set; }

    [Range(0, 65535)]
    public int? GrpcPort { get; set; }

    [MaxLength(256)]
    public string WsUrl { get; set; } = string.Empty;

    [MaxLength(256)]
    public string GrpcUrl { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Remark { get; set; } = string.Empty;

    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
}

public sealed class GameServerAddressCreateInput : GameServerAddressMutationInput
{
}

public sealed class GameServerAddressUpdateInput : GameServerAddressMutationInput
{
}

public sealed class GameServerAddressDeleteInput
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ServerId { get; set; }
}

public sealed class GameServerAddressDetailInput
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ServerId { get; set; }
}
