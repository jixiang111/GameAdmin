// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵守 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 与 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。

using Furion.ConfigurableOptions;

namespace Admin.NET.Application;

/// <summary>
/// GM RPC 配置
/// </summary>
public sealed class GmRpcOptions : IConfigurableOptions
{
    /// <summary>
    /// CenterServer 站点配置
    /// </summary>
    public GmRpcEndpointOptions CenterServer { get; set; } = new();

    /// <summary>
    /// 游戏服站点配置（保留扩展）
    /// </summary>
    public GmRpcEndpointOptions GameServer { get; set; } = new();
}

/// <summary>
/// RPC 端点配置
/// </summary>
public sealed class GmRpcEndpointOptions
{
    /// <summary>
    /// 完整 gRPC 地址（http://host:port，走 h2c）
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// GM Token（写入 x-gm-token）
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// 是否允许忽略 TLS 证书（仅测试环境使用）
    /// </summary>
    public bool AllowUntrustedCertificate { get; set; }

    /// <summary>
    /// 是否启用 TLS（若中心服未开启 HTTPS，可设为 false）
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// 配置是否启用
    /// </summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(Address);
}
