// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵守 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 与 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任。

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Furion.DependencyInjection;
using Furion.FriendlyException;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using MagicOnion.Serialization;
using MagicOnion.Serialization.MessagePack;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admin.NET.Application.GmRpc;

public interface ICenterServerRpcClient
{
    Task<IReadOnlyList<CenterServerInfoDto>> GetGameServerListAsync(CancellationToken cancellationToken = default);

    Task<CenterServerInfoDto> UpsertGameServerAsync(CenterServerUpdateReq request, CancellationToken cancellationToken = default);

    Task<GmQueryItemChangeLogRes> QueryItemChangeLogsAsync(CenterGmQueryItemChangeLogReq request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CenterVersionRuleDto>> ListAppVersionRulesAsync(CancellationToken cancellationToken = default);

    Task<CenterVersionRuleDto> UpsertAppVersionRuleAsync(CenterVersionRuleReq request, CancellationToken cancellationToken = default);

    Task DeleteAppVersionRuleAsync(string ruleId, CancellationToken cancellationToken = default);
}

public sealed class CenterServerRpcClient : ICenterServerRpcClient, ISingleton, IDisposable
{
    private readonly ILogger<CenterServerRpcClient> _logger;
    private readonly GmRpcEndpointOptions _endpointOptions;
    private readonly Lazy<GrpcChannel> _channelFactory;
    private readonly Lazy<Uri> _endpointUri;

    private static readonly object Http2SwitchLock = new();
    private static bool _http2CleartextEnabled;
    private bool _disposed;

    public CenterServerRpcClient(IOptions<GmRpcOptions> options, ILogger<CenterServerRpcClient> logger)
    {
        _logger = logger;
        _endpointOptions = options.Value.CenterServer ?? new GmRpcEndpointOptions();

        _endpointUri = new Lazy<Uri>(NormalizeEndpointUri, LazyThreadSafetyMode.ExecutionAndPublication);
        _channelFactory = new Lazy<GrpcChannel>(CreateChannel, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<IReadOnlyList<CenterServerInfoDto>> GetGameServerListAsync(CancellationToken cancellationToken = default)
    {
        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.GetGameServerList();
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "获取游戏服列表失败");
        return result?.Items ?? null;
    }

    public async Task<CenterServerInfoDto> UpsertGameServerAsync(CenterServerUpdateReq request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("请求参数不能为空");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.UpdateGameServer(request);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "更新游戏服失败");
        return result?.Data;
    }

    public async Task<GmQueryItemChangeLogRes> QueryItemChangeLogsAsync(CenterGmQueryItemChangeLogReq request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("请求参数不能为空");
        if (request.Query == null) throw Oops.Oh("查询参数不能为空");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.QueryItemChangeLogs(request);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "查询道具变化流水失败");

        return result?.Data ?? new GmQueryItemChangeLogRes
        {
            ServerId = request.ServerId,
            PageIndex = request.Query.PageIndex,
            PageSize = request.Query.PageSize,
            Items = Array.Empty<GmItemChangeLogDto>()
        };
    }

    public async Task<IReadOnlyList<CenterVersionRuleDto>> ListAppVersionRulesAsync(CancellationToken cancellationToken = default)
    {
        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.ListAppVersionRules();
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "获取版本规则列表失败");
        return result?.Items ?? Array.Empty<CenterVersionRuleDto>();
    }

    public async Task<CenterVersionRuleDto> UpsertAppVersionRuleAsync(CenterVersionRuleReq request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("请求参数不能为空");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.UpsertAppVersionRule(request);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "同步版本规则失败");
        return result?.Data;
    }

    public async Task DeleteAppVersionRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ruleId)) throw Oops.Oh("规则ID不能为空");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.DeleteAppVersionRule(ruleId);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "删除版本规则失败");
    }

    private GrpcChannel CreateChannel()
    {
        EnsureEndpointConfigured();

        var httpHandler = BuildHttpHandler();
        var address = _endpointUri.Value;
        _logger.LogInformation("初始化 CenterServer gRPC 通道：{Address}", address);

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = httpHandler
        });
    }

    private ICenterGmService CreateClient()
    {
        EnsureEndpointConfigured();
        Console.WriteLine("Create Rpc Client910");
        var resolver = CompositeResolver.Create(
            ContractlessStandardResolver.Instance,
            StandardResolver.Instance);

        var serializerProvider = MessagePackMagicOnionSerializerProvider.Default.WithOptions(
            MessagePackSerializerOptions.Standard
                .WithResolver(resolver)
                .WithCompression(MessagePackCompression.Lz4Block));

        return MagicOnionClient.Create<ICenterGmService>(
            _channelFactory.Value,serializerProvider);
    }


    private ICenterGmService DecorateClient(ICenterGmService client, CancellationToken cancellationToken)
    {
        var headers = BuildHeaders();
        if (headers.Count > 0)
        {
            client = client.WithHeaders(headers);
        }

        if (cancellationToken != default)
        {
            client = client.WithCancellationToken(cancellationToken);
        }

        return client;
    }

    private Metadata BuildHeaders()
    {
        var headers = new Metadata();
        if (!string.IsNullOrWhiteSpace(_endpointOptions.Token))
        {
            headers.Add("x-gm-token", _endpointOptions.Token);
        }

        return headers;
    }

    private HttpMessageHandler BuildHttpHandler()
    {
        EnsureEndpointConfigured();
        var uri = _endpointUri.Value;
        var handler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true
        };

        if (uri.Scheme == Uri.UriSchemeHttps && _endpointOptions.AllowUntrustedCertificate)
        {
            handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            };
        }
        else if (uri.Scheme == Uri.UriSchemeHttp)
        {
            handler.EnableMultipleHttp2Connections = true;
            handler.AllowAutoRedirect = false;
            handler.MaxConnectionsPerServer = int.MaxValue;
            EnableHttp2Cleartext();
        }

        return handler;
    }

    private Uri NormalizeEndpointUri()
    {
        EnsureEndpointConfigured();

        var raw = (_endpointOptions.Address ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw Oops.Oh("未配置 CenterServer GM RPC 地址");
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            var scheme = _endpointOptions.UseTls ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            raw = $"{scheme}://{raw}";
            if (!Uri.TryCreate(raw, UriKind.Absolute, out uri))
            {
                throw Oops.Oh($"CenterServer GM RPC 地址格式不正确：{_endpointOptions.Address}");
            }
        }

        if (_endpointOptions.UseTls && uri.Scheme != Uri.UriSchemeHttps)
        {
            uri = new UriBuilder(uri)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = uri.IsDefaultPort ? -1 : uri.Port
            }.Uri;
        }
        else if (!_endpointOptions.UseTls && uri.Scheme != Uri.UriSchemeHttp)
        {
            var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttp };
            if (!uri.IsDefaultPort) builder.Port = uri.Port;
            uri = builder.Uri;
        }

        return uri;
    }

    private static void EnableHttp2Cleartext()
    {
        if (_http2CleartextEnabled) return;
        lock (Http2SwitchLock)
        {
            if (_http2CleartextEnabled) return;
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            _http2CleartextEnabled = true;
        }
    }

    private void EnsureRpcSuccess(bool success, string message, string fallback)
    {
        if (!success)
        {
            throw Oops.Oh(string.IsNullOrWhiteSpace(message) ? fallback : message);
        }
    }

    private void EnsureEndpointConfigured()
    {
        if (!_endpointOptions.Enabled)
        {
            throw Oops.Oh("未配置 CenterServer GM RPC 地址，请检查 GmRpc:CenterServer");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_channelFactory.IsValueCreated)
        {
            _channelFactory.Value.Dispose();
        }

        _disposed = true;
    }
}
