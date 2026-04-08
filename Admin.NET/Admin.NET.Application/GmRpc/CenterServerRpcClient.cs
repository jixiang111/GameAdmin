// Admin.NET 椤圭洰鐨勭増鏉冦€佸晢鏍囥€佷笓鍒╁拰鍏朵粬鐩稿叧鏉冨埄鍧囧彈鐩稿簲娉曞緥娉曡鐨勪繚鎶ゃ€備娇鐢ㄦ湰椤圭洰搴旈伒瀹堢浉鍏虫硶寰嬫硶瑙勫拰璁稿彲璇佺殑瑕佹眰銆?//
// 鏈」鐩富瑕侀伒瀹?MIT 璁稿彲璇佸拰 Apache 璁稿彲璇侊紙鐗堟湰 2.0锛夎繘琛屽垎鍙戝拰浣跨敤銆傝鍙瘉浣嶄簬婧愪唬鐮佹爲鏍圭洰褰曚腑鐨?LICENSE-MIT 涓?LICENSE-APACHE 鏂囦欢銆?//
// 涓嶅緱鍒╃敤鏈」鐩粠浜嬪嵄瀹冲浗瀹跺畨鍏ㄣ€佹壈涔辩ぞ浼氱З搴忋€佷镜鐘粬浜哄悎娉曟潈鐩婄瓑娉曞緥娉曡绂佹鐨勬椿鍔紒浠讳綍鍩轰簬鏈」鐩簩娆″紑鍙戣€屼骇鐢熺殑涓€鍒囨硶寰嬬籂绾峰拰璐ｄ换锛屾垜浠笉鎵挎媴浠讳綍璐ｄ换銆?
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

    Task<GmAdjustPlayerItemRes> AdjustPlayerItemAsync(CenterGmAdjustPlayerItemReq request, CancellationToken cancellationToken = default);

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
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "鑾峰彇娓告垙鏈嶅垪琛ㄥけ璐?);
        return result?.Items ?? null;
    }

    public async Task<CenterServerInfoDto> UpsertGameServerAsync(CenterServerUpdateReq request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("璇锋眰鍙傛暟涓嶈兘涓虹┖");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.UpdateGameServer(request);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "鏇存柊娓告垙鏈嶅け璐?);
        return result?.Data;
    }


    public async Task<GmAdjustPlayerItemRes> AdjustPlayerItemAsync(CenterGmAdjustPlayerItemReq request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("请求参数不能为空");
        if (request.Request == null) throw Oops.Oh("调整参数不能为空");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.AdjustPlayerItem(request);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "调整玩家道具失败");

        return result?.Data ?? new GmAdjustPlayerItemRes
        {
            RoleId = request.Request.RoleId,
            ItemId = request.Request.ItemId,
            AppliedDelta = request.Request.Delta
        };
    }

    public async Task<GmQueryItemChangeLogRes> QueryItemChangeLogsAsync(CenterGmQueryItemChangeLogReq request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("璇锋眰鍙傛暟涓嶈兘涓虹┖");
        if (request.Query == null) throw Oops.Oh("鏌ヨ鍙傛暟涓嶈兘涓虹┖");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.QueryItemChangeLogs(request);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "鏌ヨ閬撳叿鍙樺寲娴佹按澶辫触");

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
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "鑾峰彇鐗堟湰瑙勫垯鍒楄〃澶辫触");
        return result?.Items ?? Array.Empty<CenterVersionRuleDto>();
    }

    public async Task<CenterVersionRuleDto> UpsertAppVersionRuleAsync(CenterVersionRuleReq request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("璇锋眰鍙傛暟涓嶈兘涓虹┖");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.UpsertAppVersionRule(request);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "鍚屾鐗堟湰瑙勫垯澶辫触");
        return result?.Data;
    }

    public async Task DeleteAppVersionRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ruleId)) throw Oops.Oh("瑙勫垯ID涓嶈兘涓虹┖");

        var client = DecorateClient(CreateClient(), cancellationToken);
        var result = await client.DeleteAppVersionRule(ruleId);
        EnsureRpcSuccess(result?.Success ?? false, result?.Message, "鍒犻櫎鐗堟湰瑙勫垯澶辫触");
    }

    private GrpcChannel CreateChannel()
    {
        EnsureEndpointConfigured();

        var httpHandler = BuildHttpHandler();
        var address = _endpointUri.Value;
        _logger.LogInformation("鍒濆鍖?CenterServer gRPC 閫氶亾锛歿Address}", address);

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
            throw Oops.Oh("鏈厤缃?CenterServer GM RPC 鍦板潃");
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            var scheme = _endpointOptions.UseTls ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            raw = $"{scheme}://{raw}";
            if (!Uri.TryCreate(raw, UriKind.Absolute, out uri))
            {
                throw Oops.Oh($"CenterServer GM RPC 鍦板潃鏍煎紡涓嶆纭細{_endpointOptions.Address}");
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
            throw Oops.Oh("鏈厤缃?CenterServer GM RPC 鍦板潃锛岃妫€鏌?GmRpc:CenterServer");
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


