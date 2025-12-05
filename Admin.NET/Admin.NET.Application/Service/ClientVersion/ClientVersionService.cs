// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Aliyun.OSS;
using Aliyun.OSS.Common;
using Admin.NET.Application.GmRpc;
using Admin.NET.Application.Service.ClientVersion.Dto;
using COSXML;
using COSXML.Auth;
using COSXML.CosException;
using COSXML.Model.Bucket;
using COSXML.Model.Object;
using Furion.FriendlyException;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Admin.NET.Application.Service.ClientVersion;

/// <summary>
/// 客户端版本管理（对象存储凭证、白名单、版本规则）
/// </summary>
[ApiDescriptionSettings(Order = 625, Description = "客户端版本管理")]
public class ClientVersionService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<ClientResourceOssConfig> _ossConfigRep;
    private readonly SqlSugarRepository<ClientVersionWhitelist> _whitelistRep;
    private readonly SqlSugarRepository<ClientVersionRule> _ruleRep;
    private readonly SqlSugarRepository<ClientVersionRuleWhitelist> _ruleWhitelistRep;
    private readonly UserManager _userManager;
    private readonly ILogger<ClientVersionService> _logger;
    private readonly ICenterServerRpcClient _rpcClient;

    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "aliyun",
        "tencent-cos"
    };
    private static readonly Regex ResVersionRegex = new(@"versions_(\d+)\.json", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClientVersionService(SqlSugarRepository<ClientResourceOssConfig> ossConfigRep,
        SqlSugarRepository<ClientVersionWhitelist> whitelistRep,
        SqlSugarRepository<ClientVersionRule> ruleRep,
        SqlSugarRepository<ClientVersionRuleWhitelist> ruleWhitelistRep,
        UserManager userManager,
        ILogger<ClientVersionService> logger,
        ICenterServerRpcClient rpcClient)
    {
        _ossConfigRep = ossConfigRep;
        _whitelistRep = whitelistRep;
        _ruleRep = ruleRep;
        _ruleWhitelistRep = ruleWhitelistRep;
        _userManager = userManager;
        _logger = logger;
        _rpcClient = rpcClient;
    }

    #region 对象存储配置

    /// <summary>
    /// 获取当前对象存储配置
    /// </summary>
    [DisplayName("获取客户端资源对象存储配置")]
    public async Task<ClientOssConfigOutput> GetOssConfig([FromQuery] ClientOssConfigQueryInput input)
    {
        ClientResourceOssConfig config;
        if (string.IsNullOrWhiteSpace(input?.Provider))
        {
            config = await GetActiveOssConfigAsync();
        }
        else
        {
            var provider = NormalizeProvider(input.Provider);
            EnsureProviderSupported(provider);
            config = await FindOssConfigAsync(provider) ?? throw Oops.Oh($"未找到 {provider} 的对象存储凭证，请先配置");
        }

        return BuildOssOutput(config);
    }

    /// <summary>
    /// 新增 / 更新对象存储配置
    /// </summary>
    [ApiDescriptionSettings(Name = "SaveOssConfig"), HttpPost]
    [DisplayName("保存客户端资源对象存储配置")]
    public async Task<ClientOssConfigOutput> SaveOssConfig(ClientOssConfigInput input)
    {
        var provider = NormalizeProvider(input.Provider);
        EnsureProviderSupported(provider);

        var entity = await FindOssConfigAsync(provider);
        var now = DateTime.Now;

        var normalizedRoot = NormalizeRootPathForStorage(input.RootPath);
        var resourceDomain = NormalizeResourceDomain(input.ResourceDomain);
        var endpoint = input.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
            throw Oops.Oh("Endpoint 不能为空");

        var bucket = input.BucketName?.Trim();
        if (string.IsNullOrWhiteSpace(bucket))
            throw Oops.Oh("BucketName 不能为空");

        var secretCipher = string.IsNullOrWhiteSpace(input.AccessKeySecret)
            ? entity?.AccessKeySecretCipher
            : CryptogramUtil.Encrypt(input.AccessKeySecret.Trim());

        if (string.IsNullOrWhiteSpace(secretCipher))
            throw Oops.Oh("AccessKeySecret 不能为空");

        if (entity == null)
        {
            entity = new ClientResourceOssConfig
            {
                Provider = provider,
                AccessKeyId = input.AccessKeyId.Trim(),
                AccessKeySecretCipher = secretCipher,
                Endpoint = endpoint,
                BucketName = bucket,
                RootPath = normalizedRoot,
                ResourceDomain = resourceDomain,
                Region = input.Region?.Trim(),
                Remark = input.Remark?.Trim(),
                IsDefault = input.IsDefault,
                CreateTime = now,
                CreateUserId = _userManager.UserId,
                CreateUserName = _userManager.RealName ?? _userManager.Account
            };

            await ValidateOssConnectivityAsync(entity);
            await _ossConfigRep.InsertAsync(entity);
        }
        else
        {
            entity.AccessKeyId = input.AccessKeyId.Trim();
            entity.AccessKeySecretCipher = secretCipher;
            entity.Endpoint = endpoint;
            entity.BucketName = bucket;
            entity.RootPath = normalizedRoot;
            entity.ResourceDomain = resourceDomain;
            entity.Region = input.Region?.Trim();
            entity.Remark = input.Remark?.Trim();
            entity.IsDefault = input.IsDefault;
            entity.UpdateTime = now;
            entity.UpdateUserId = _userManager.UserId;
            entity.UpdateUserName = _userManager.RealName ?? _userManager.Account;

            await ValidateOssConnectivityAsync(entity);
            await _ossConfigRep.UpdateAsync(entity);
        }

        if (input.IsDefault)
        {
            await _ossConfigRep.AsUpdateable()
                .SetColumns(u => new ClientResourceOssConfig { IsDefault = false })
                .Where(u => u.Id != entity.Id)
                .ExecuteCommandAsync();
        }

        return BuildOssOutput(entity);
    }

    /// <summary>
    /// 列出对象存储配置
    /// </summary>
    [DisplayName("列出客户端对象存储配置")]
    public async Task<List<ClientOssConfigSummaryOutput>> GetOssConfigList()
    {
        var list = await _ossConfigRep.AsQueryable()
            .OrderBy(u => new { Order = u.IsDefault ? 0 : 1, u.Provider })
            .ToListAsync();

        return list.Select(u => new ClientOssConfigSummaryOutput
        {
            Id = u.Id,
            Provider = u.Provider,
            BucketName = u.BucketName,
            RootPath = u.RootPath,
            ResourceDomain = u.ResourceDomain,
            IsDefault = u.IsDefault,
            Remark = u.Remark
        }).ToList();
    }

    #endregion

    #region 白名单

    /// <summary>
    /// 白名单分页
    /// </summary>
    [DisplayName("客户端白名单分页")]
    public async Task<SqlSugarPagedList<ClientWhitelistOutput>> GetWhitelistPage([FromQuery] ClientWhitelistPageInput input)
    {
        var keyword = input.Keyword?.Trim();
        var query = _whitelistRep.AsQueryable()
            .WhereIF(input.OnlyEnabled == true, u => u.Enabled)
            .WhereIF(!string.IsNullOrWhiteSpace(keyword),
                u => u.MachineCode.Contains(keyword!) || u.Remark.Contains(keyword!) || u.TagsJson.Contains(keyword!))
            .OrderBy(u => u.CreateTime, OrderByType.Desc);

        var page = await query.ToPagedListAsync(input.Page, input.PageSize);
        return ConvertPagedList(page, MapWhitelistOutput);
    }

    /// <summary>
    /// 白名单列表（用于下拉/复选）
    /// </summary>
    [DisplayName("客户端白名单列表")]
    public async Task<List<ClientWhitelistOutput>> GetWhitelistList([FromQuery] ClientWhitelistQueryInput input)
    {
        var ids = input.EntryIds?.Where(id => id > 0).Distinct().ToList();
        var query = _whitelistRep.AsQueryable()
            .WhereIF(ids != null && ids.Count > 0, u => ids.Contains(u.Id))
            .WhereIF(input.OnlyEnabled == true, u => u.Enabled)
            .OrderBy(u => u.CreateTime, OrderByType.Desc);
        var list = await query.ToListAsync();
        return list.Select(MapWhitelistOutput).ToList();
    }

    /// <summary>
    /// 新增/编辑白名单
    /// </summary>
    [ApiDescriptionSettings(Name = "SaveWhitelist"), HttpPost]
    [DisplayName("保存客户端白名单")]
    public async Task<ClientWhitelistOutput> SaveWhitelist(ClientWhitelistUpsertInput input)
    {
        var machineCode = NormalizeMachineCode(input.MachineCode);
        var machineCodeUpper = machineCode.ToUpperInvariant();
        var tagsJson = SerializeTags(input.Tags);
        var now = DateTime.Now;

        ClientVersionWhitelist entity;
        if (input.EntryId.HasValue)
        {
            entity = await _whitelistRep.GetFirstAsync(u => u.Id == input.EntryId.Value)
                ?? throw Oops.Oh("白名单不存在或已删除");

            if (!machineCode.Equals(entity.MachineCode, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _whitelistRep.IsAnyAsync(u =>
                    SqlFunc.ToUpper(u.MachineCode) == machineCodeUpper && u.Id != entity.Id);
                if (exists) throw Oops.Oh("机器码已存在，无法重复添加");
            }

            entity.MachineCode = machineCode;
            entity.Remark = input.Remark?.Trim();
            entity.TagsJson = tagsJson;
            entity.Enabled = input.Enabled;
            entity.UpdateTime = now;
            entity.UpdateUserId = _userManager.UserId;
            entity.UpdateUserName = _userManager.RealName ?? _userManager.Account;

            await _whitelistRep.UpdateAsync(entity);

            // 若被禁用，则需要从所有渠道规则中移除并同步 CenterServer
            if (!entity.Enabled)
            {
                await RemoveWhitelistFromLinkedRulesAsync(entity.Id);
            }
        }
        else
        {
            var exists = await _whitelistRep.IsAnyAsync(u => SqlFunc.ToUpper(u.MachineCode) == machineCodeUpper);
            if (exists) throw Oops.Oh("机器码已存在，无法重复添加");

            entity = new ClientVersionWhitelist
            {
                MachineCode = machineCode,
                Remark = input.Remark?.Trim(),
                TagsJson = tagsJson,
                Enabled = input.Enabled,
                CreateTime = now,
                CreateUserId = _userManager.UserId,
                CreateUserName = _userManager.RealName ?? _userManager.Account
            };

            await _whitelistRep.InsertAsync(entity);
        }

        return MapWhitelistOutput(entity);
    }

    /// <summary>
    /// 删除白名单
    /// </summary>
    [ApiDescriptionSettings(Name = "DeleteWhitelist"), HttpPost]
    [DisplayName("删除客户端白名单")]
    public async Task DeleteWhitelist(ClientWhitelistDeleteInput input)
    {
        var entry = await _whitelistRep.GetFirstAsync(u => u.Id == input.EntryId)
            ?? throw Oops.Oh("白名单不存在或已删除");

        await RemoveWhitelistFromLinkedRulesAsync(entry.Id);
        await _whitelistRep.DeleteAsync(entry);
    }

    #endregion

    #region 版本规则

    /// <summary>
    /// 规则列表
    /// </summary>
    [DisplayName("客户端版本规则列表")]
    public async Task<List<ClientVersionRuleOutput>> GetRuleList([FromQuery] ClientVersionRuleQueryInput input)
    {
        var query = _ruleRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.ChannelId), u => u.ChannelId == input.ChannelId)
            .WhereIF(input.Platform.HasValue, u => u.Platform == input.Platform.Value)
            .OrderBy(u => new { u.ChannelId, u.Platform });

        var rules = await query.ToListAsync();
        return await BuildRuleOutputsAsync(rules);
    }

    /// <summary>
    /// 规则详情
    /// </summary>
    [DisplayName("客户端版本规则详情")]
    public async Task<ClientVersionRuleOutput> GetRuleDetail([FromQuery] long ruleId)
    {
        var rule = await _ruleRep.GetFirstAsync(u => u.Id == ruleId) ?? throw Oops.Oh("规则不存在");
        var list = await BuildRuleOutputsAsync(new[] { rule });
        return list.First();
    }

    /// <summary>
    /// 新增 / 更新规则
    /// </summary>
    [ApiDescriptionSettings(Name = "SaveRule"), HttpPost]
    [DisplayName("保存客户端版本规则")]
    public async Task<ClientVersionRuleOutput> SaveRule(ClientVersionRuleSaveInput input)
    {
        var channelId = input.ChannelId.Trim();
        var platform = input.Platform;
        var now = DateTime.Now;

        var isNew = !input.RuleId.HasValue;
        string? oldChannelId = null;
        string? oldChannelName = null;
        string? oldBucket = null;
        string? oldRoot = null;
        string? oldResourceDomain = null;
        string? oldGeneralJson = null;
        string? oldWhitelistJson = null;
        List<string> oldWhitelistCodes = new();
        string? oldProvider = null;
        long? oldOssConfigId = null;

        ClientVersionRule entity;
        if (input.RuleId.HasValue)
        {
            entity = await _ruleRep.GetFirstAsync(u => u.Id == input.RuleId.Value)
                ?? throw Oops.Oh("规则不存在或已删除");

            oldChannelId = entity.ChannelId;
            oldChannelName = entity.ChannelName;
            oldBucket = entity.BucketName;
            oldRoot = entity.RootPath;
            oldResourceDomain = entity.ResourceDomain;
            oldGeneralJson = entity.GeneralVersionInfoJson;
            oldWhitelistJson = entity.WhitelistVersionInfoJson;
            if (string.IsNullOrWhiteSpace(entity.Provider))
            {
                entity.Provider = "aliyun";
            }
            oldProvider = entity.Provider;
            oldOssConfigId = entity.OssConfigId;

            var oldOutputs = await BuildRuleOutputsAsync(new[] { entity });
            var oldOutput = oldOutputs.FirstOrDefault();
            if (oldOutput != null)
            {
                oldWhitelistCodes = oldOutput.WhitelistMachineCodes ?? new List<string>();
            }
        }
        else
        {
            entity = await _ruleRep.GetFirstAsync(u => u.ChannelId == channelId && u.Platform == platform)
                ?? new ClientVersionRule
                {
                    ChannelId = channelId,
                    Platform = platform,
                    CreateTime = now,
                    CreateUserId = _userManager.UserId,
                    CreateUserName = _userManager.RealName ?? _userManager.Account
                };
        }

        var duplicate = await _ruleRep.IsAnyAsync(u => u.ChannelId == channelId && u.Platform == platform && u.Id != entity.Id);
        if (duplicate) throw Oops.Oh("同一渠道+平台仅允许一条规则");

        if (!input.OssConfigId.HasValue)
            throw Oops.Oh("请选择对象存储参数配置");

        var ossConfig = await GetOssConfigByIdAsync(input.OssConfigId.Value);
        var provider = NormalizeProvider(ossConfig.Provider);
        EnsureProviderSupported(provider);

        if (!string.IsNullOrWhiteSpace(input.Provider))
        {
            var inputProvider = NormalizeProvider(input.Provider);
            if (!string.Equals(inputProvider, provider, StringComparison.OrdinalIgnoreCase))
                throw Oops.Oh("对象存储配置与所选存储类型不一致");
        }

        var bucket = string.IsNullOrWhiteSpace(input.BucketName) ? ossConfig.BucketName : input.BucketName.Trim();
        if (string.IsNullOrWhiteSpace(bucket))
            throw Oops.Oh("BucketName 不能为空");

        var rootSource = string.IsNullOrWhiteSpace(input.RootPath) ? ossConfig.RootPath : input.RootPath;
        var rootPath = NormalizeRootPathForStorage(rootSource);
        var domainSource = string.IsNullOrWhiteSpace(input.ResourceDomain) ? ossConfig.ResourceDomain : input.ResourceDomain;
        var resourceDomain = NormalizeResourceDomain(domainSource);

        entity.ChannelId = channelId;
        entity.ChannelName = input.ChannelName?.Trim() ?? channelId;
        entity.Platform = platform;
        entity.Provider = provider;
        entity.OssConfigId = ossConfig.Id;
        entity.BucketName = bucket;
        entity.RootPath = rootPath;
        entity.ResourceDomain = resourceDomain;
        entity.UpdateTime = now;
        entity.UpdateUserId = _userManager.UserId;
        entity.UpdateUserName = _userManager.RealName ?? _userManager.Account;

        using var client = CreateStorageClient(ossConfig);
        var normalizedRoot = NormalizeStoredRoot(entity.RootPath);

        var generalInfo = await NormalizeVersionInfoAsync(client, ossConfig, entity, input.GeneralVersionInfo, normalizedRoot, false);
        var whitelistInfo = await NormalizeVersionInfoAsync(client, ossConfig, entity, input.WhitelistVersionInfo, normalizedRoot, true);

        entity.GeneralVersionInfoJson = SerializeVersionInfo(generalInfo);
        entity.WhitelistVersionInfoJson = SerializeVersionInfo(whitelistInfo);

        if (entity.Id > 0)
            await _ruleRep.UpdateAsync(entity);
        else
            await _ruleRep.InsertAsync(entity);

        await PersistRuleWhitelistAsync(entity.Id, input.WhitelistTesterEntryIds);

        var output = await BuildRuleOutputsAsync(new[] { entity });
        var result = output.First();

        // 同步到 CenterServer（仅在有变化或新建时）
        var needSync = isNew || RuleChanged(
            oldChannelId,
            oldChannelName,
            oldBucket,
            oldRoot,
            oldResourceDomain,
            oldGeneralJson,
            oldWhitelistJson,
            oldProvider,
            oldOssConfigId,
            oldWhitelistCodes,
            entity,
            result.WhitelistMachineCodes);

        if (needSync)
        {
            await SyncVersionRuleToCenterAsync(entity, result.WhitelistMachineCodes);
        }

        return result;
    }

    /// <summary>
    /// 删除规则
    /// </summary>
    [ApiDescriptionSettings(Name = "DeleteRule"), HttpPost]
    [DisplayName("删除客户端版本规则")]
    public async Task DeleteRule(ClientVersionRuleDeleteInput input)
    {
        var entity = await _ruleRep.GetFirstAsync(u => u.Id == input.RuleId) ?? throw Oops.Oh("规则不存在或已删除");

        await _ruleWhitelistRep.DeleteAsync(u => u.RuleId == entity.Id);
        await _ruleRep.DeleteAsync(entity);

        // 从 CenterServer 删除规则
        await DeleteVersionRuleFromCenterAsync(entity.ChannelId, entity.Platform);
    }

    /// <summary>
    /// 列出可选 AppVersion
    /// </summary>
    [DisplayName("列出客户端 AppVersion")]
    public async Task<List<ChannelAppVersionItem>> GetAppVersions([FromQuery] ChannelAppVersionQuery input)
    {
        var context = await ResolveOssContextAsync(input.ChannelId, input.Platform, input.BucketName, input.RootPath, input.Provider, input.OssConfigId);
        using var client = CreateStorageClient(context.Config);
        return await ListAppVersionsAsync(client, context, input.Platform);
    }

    /// <summary>
    /// 列出资源版本
    /// </summary>
    [DisplayName("列出客户端资源版本")]
    public async Task<List<ChannelResVersionItem>> GetResVersions([FromQuery] ChannelResVersionQuery input)
    {
        var context = await ResolveOssContextAsync(input.ChannelId, input.Platform, input.BucketName, input.RootPath, input.Provider, input.OssConfigId);
        using var client = CreateStorageClient(context.Config);
        return await ListResVersionsAsync(client, context, input.Platform, input.AppVersion);
    }

    /// <summary>
    /// 白名单版本一键发布
    /// </summary>
    [ApiDescriptionSettings(Name = "PromoteWhitelist"), HttpPost]
    [DisplayName("白名单版本发布为正式")]
    public async Task<ClientVersionRuleOutput> PromoteWhitelist(PromoteWhitelistVersionInput input)
    {
        if (!input.Confirm) throw Oops.Oh("请先确认发布提示");

        var rule = await _ruleRep.GetFirstAsync(u => u.Id == input.RuleId) ?? throw Oops.Oh("规则不存在");
        var whitelistInfo = DeserializeVersionInfo(rule.WhitelistVersionInfoJson)
            ?? throw Oops.Oh("尚未配置白名单版本，无法发布");

        var generalInfo = DeserializeVersionInfo(rule.GeneralVersionInfoJson);
        var previousGeneralJson = rule.GeneralVersionInfoJson;
        var ossConfig = await GetOssConfigForRuleAsync(rule);
        using var client = CreateStorageClient(ossConfig);
        var normalizedRoot = NormalizeStoredRoot(rule.RootPath);

        ClientAppVersionInfoDto publishInfo = CloneVersionInfo(whitelistInfo);

        if (!string.IsNullOrWhiteSpace(input.TargetResVersion) &&
            !input.TargetResVersion.Equals(whitelistInfo.ResVersion, StringComparison.OrdinalIgnoreCase))
        {
            publishInfo.ResVersion = input.TargetResVersion.Trim();
            await PopulateResVersionMetadataAsync(client, ossConfig, rule, publishInfo, normalizedRoot);
        }
        else
        {
            await PopulateResVersionMetadataAsync(client, ossConfig, rule, publishInfo, normalizedRoot);
        }

        var previousTimestamp = generalInfo?.ResVersionTimestamp ?? 0;
        if (publishInfo.ResVersionTimestamp.HasValue && publishInfo.ResVersionTimestamp.Value < previousTimestamp)
            throw Oops.Oh("资源版本不得早于当前线上版本");

        rule.GeneralVersionInfoJson = SerializeVersionInfo(publishInfo);
        rule.LatestPublishResVersion = publishInfo.ResVersion;
        rule.LatestPublishTime = DateTime.Now;
        rule.LatestPublishUserId = _userManager.UserId;
        rule.LatestPublishUserName = _userManager.RealName ?? _userManager.Account;
        rule.UpdateTime = rule.LatestPublishTime;
        rule.UpdateUserId = _userManager.UserId;
        rule.UpdateUserName = _userManager.RealName ?? _userManager.Account;

        await _ruleRep.UpdateAsync(rule);

        var output = await BuildRuleOutputsAsync(new[] { rule });
        var result = output.First();

        // 发布后同步到 CenterServer（仅当版本实际变更）
        if (!string.Equals(previousGeneralJson, rule.GeneralVersionInfoJson, StringComparison.Ordinal))
        {
            await SyncVersionRuleToCenterAsync(rule, result.WhitelistMachineCodes);
        }

        return result;
    }

    #endregion

    #region 私有方法 - 对象存储配置

    private async Task<ClientResourceOssConfig?> FindOssConfigAsync(string provider)
    {
        return await _ossConfigRep.GetFirstAsync(u => u.Provider == provider && u.IsDefault)
            ?? await _ossConfigRep.GetFirstAsync(u => u.Provider == provider);
    }

    private async Task<ClientResourceOssConfig> GetActiveOssConfigAsync(string? provider = null)
    {
        if (!string.IsNullOrWhiteSpace(provider))
        {
            var normalized = NormalizeProvider(provider);
            EnsureProviderSupported(normalized);
            return await FindOssConfigAsync(normalized) ?? throw Oops.Oh($"尚未配置 {normalized} 对象存储凭证");
        }

        var defaultConfig = await _ossConfigRep.GetFirstAsync(u => u.IsDefault);
        if (defaultConfig != null)
            return defaultConfig;

        var fallback = await _ossConfigRep.GetFirstAsync(u => true);
        return fallback ?? throw Oops.Oh("尚未配置任何对象存储凭证");
    }

    private async Task<ClientResourceOssConfig> GetOssConfigByIdAsync(long configId)
    {
        var config = await _ossConfigRep.GetFirstAsync(u => u.Id == configId);
        return config ?? throw Oops.Oh("对象存储配置不存在或已删除");
    }

    private async Task<ClientResourceOssConfig> GetOssConfigForRuleAsync(ClientVersionRule rule)
    {
        if (rule.OssConfigId.HasValue)
        {
            return await GetOssConfigByIdAsync(rule.OssConfigId.Value);
        }

        return await GetActiveOssConfigAsync(rule.Provider);
    }

    private async Task ValidateOssConnectivityAsync(ClientResourceOssConfig config)
    {
        try
        {
            using var client = CreateStorageClient(config);
            var existed = await client.DoesBucketExistAsync(config.BucketName);
            if (!existed)
            {
                throw Oops.Oh($"Bucket {config.BucketName} 不存在或无权访问，请确认后重试");
            }
        }
        catch (ObjectStorageException ex)
        {
            _logger.LogError(ex, "验证对象存储凭证失败");
            throw Oops.Oh($"验证对象存储凭证失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证对象存储凭证失败");
            throw Oops.Oh($"验证对象存储凭证失败：{ex.Message}");
        }
    }

    private ClientOssConfigOutput BuildOssOutput(ClientResourceOssConfig entity)
    {
        var secret = CryptogramUtil.Decrypt(entity.AccessKeySecretCipher);
        return new ClientOssConfigOutput
        {
            Id = entity.Id,
            Provider = entity.Provider,
            AccessKeyId = entity.AccessKeyId,
            AccessKeySecretMasked = MaskSecret(secret),
            Endpoint = entity.Endpoint,
            BucketName = entity.BucketName,
            RootPath = entity.RootPath,
            ResourceDomain = entity.ResourceDomain,
            Region = entity.Region,
            IsDefault = entity.IsDefault,
            Remark = entity.Remark,
            CreateTime = entity.CreateTime,
            UpdateTime = entity.UpdateTime,
            UpdatedBy = entity.UpdateUserName
        };
    }

    private IObjectStorageClient CreateStorageClient(ClientResourceOssConfig config)
    {
        var provider = NormalizeProvider(config.Provider);
        EnsureProviderSupported(provider);
        var secret = CryptogramUtil.Decrypt(config.AccessKeySecretCipher);
        return provider switch
        {
            "aliyun" => new AliyunObjectStorageClient(config, secret),
            "tencent-cos" => new TencentCosStorageClient(config, secret, _logger),
            _ => throw Oops.Oh($"暂不支持 {provider} 对象存储类型")
        };
    }

    #endregion

    #region 私有方法 - 白名单

    private static string NormalizeMachineCode(string machineCode)
    {
        if (string.IsNullOrWhiteSpace(machineCode))
            throw Oops.Oh("机器码不能为空");

        var normalized = machineCode.Trim().Replace(" ", "");
        if (normalized.Length < 16 || normalized.Length > 128)
            throw Oops.Oh("机器码长度需在 16~128 之间");

        if (!Regex.IsMatch(normalized, "^[A-Za-z0-9\\-_]+$"))
            throw Oops.Oh("机器码仅允许字母、数字、-、_");

        return normalized;
    }

    private static string? SerializeTags(IEnumerable<string>? tags)
    {
        if (tags == null) return null;
        var filtered = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
        return filtered.Count == 0 ? null : JsonSerializer.Serialize(filtered, JsonOptions);
    }

    private static bool RuleChanged(
        string? oldChannelId,
        string? oldChannelName,
        string? oldBucket,
        string? oldRoot,
        string? oldResourceDomain,
        string? oldGeneralJson,
        string? oldWhitelistJson,
        string? oldProvider,
        long? oldOssConfigId,
        List<string> oldWhitelistCodes,
        ClientVersionRule newRule,
        List<string>? newWhitelistCodes)
    {
        if (oldChannelId == null) return true; // 新建

        if (!string.Equals(oldChannelId, newRule.ChannelId, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(oldChannelName, newRule.ChannelName, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(oldBucket, newRule.BucketName, StringComparison.Ordinal)) return true;
        if (!string.Equals(oldRoot, newRule.RootPath, StringComparison.Ordinal)) return true;
        if (!string.Equals(oldResourceDomain, newRule.ResourceDomain, StringComparison.Ordinal)) return true;
        if (!string.Equals(oldGeneralJson ?? string.Empty, newRule.GeneralVersionInfoJson ?? string.Empty, StringComparison.Ordinal)) return true;
        if (!string.Equals(oldWhitelistJson ?? string.Empty, newRule.WhitelistVersionInfoJson ?? string.Empty, StringComparison.Ordinal)) return true;
        if (!string.Equals(oldProvider ?? "aliyun", newRule.Provider, StringComparison.OrdinalIgnoreCase)) return true;
        if (oldOssConfigId != newRule.OssConfigId) return true;

        var newCodes = newWhitelistCodes ?? new List<string>();
        if (oldWhitelistCodes.Count != newCodes.Count) return true;

        var oldSet = new HashSet<string>(oldWhitelistCodes, StringComparer.OrdinalIgnoreCase);
        var newSet = new HashSet<string>(newCodes, StringComparer.OrdinalIgnoreCase);
        return oldSet.Count != newSet.Count || oldSet.Except(newSet).Any();
    }

    /// <summary>
    /// 将指定白名单从所有关联规则中移除并同步 CenterServer
    /// </summary>
    private async Task RemoveWhitelistFromLinkedRulesAsync(long whitelistEntryId)
    {
        var affectedRuleIds = await _ruleWhitelistRep.AsQueryable()
            .Where(u => u.WhitelistEntryId == whitelistEntryId)
            .Select(u => u.RuleId)
            .Distinct()
            .ToListAsync();

        await _ruleWhitelistRep.DeleteAsync(u => u.WhitelistEntryId == whitelistEntryId);

        if (affectedRuleIds.Count == 0) return;

        var affectedRules = await _ruleRep.AsQueryable()
            .Where(r => affectedRuleIds.Contains(r.Id))
            .ToListAsync();

        if (affectedRules.Count == 0) return;

        var outputs = await BuildRuleOutputsAsync(affectedRules);
        var codeDict = outputs.ToDictionary(o => o.RuleId, o => o.WhitelistMachineCodes ?? new List<string>());

        foreach (var rule in affectedRules)
        {
            var codes = codeDict.TryGetValue(rule.Id, out var list) ? list : new List<string>();
            await SyncVersionRuleToCenterAsync(rule, codes);
        }
    }

    private static List<string>? DeserializeTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(tagsJson, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private ClientWhitelistOutput MapWhitelistOutput(ClientVersionWhitelist entity)
    {
        return new ClientWhitelistOutput
        {
            EntryId = entity.Id,
            MachineCode = entity.MachineCode,
            Remark = entity.Remark,
            Enabled = entity.Enabled,
            Tags = DeserializeTags(entity.TagsJson),
            CreatedBy = entity.CreateUserName,
            CreateTime = entity.CreateTime,
            UpdateTime = entity.UpdateTime
        };
    }

    #endregion

    #region 私有方法 - 规则与对象存储

    private sealed class OssContext
    {
        public ClientResourceOssConfig Config { get; init; }
        public ClientVersionRule? Rule { get; init; }
        public string NormalizedRoot { get; init; }
        public string BucketName { get; init; }
        public string Provider { get; init; }
    }

    private async Task<OssContext> ResolveOssContextAsync(string channelId, ClientPlatform platform, string? bucketOverride, string? rootOverride, string? providerOverride = null, long? ossConfigIdOverride = null)
    {
        ClientVersionRule? rule = null;
        if (!string.IsNullOrWhiteSpace(channelId))
        {
            rule = await _ruleRep.GetFirstAsync(u => u.ChannelId == channelId && u.Platform == platform);
        }

        var provider = string.IsNullOrWhiteSpace(providerOverride)
            ? rule?.Provider
            : NormalizeProvider(providerOverride);

        ClientResourceOssConfig config;
        if (ossConfigIdOverride.HasValue)
        {
            config = await GetOssConfigByIdAsync(ossConfigIdOverride.Value);
            if (!string.IsNullOrWhiteSpace(provider))
            {
                var normalized = NormalizeProvider(provider);
                var configProvider = NormalizeProvider(config.Provider);
                if (!string.Equals(normalized, configProvider, StringComparison.OrdinalIgnoreCase))
                    throw Oops.Oh("对象存储配置与所选存储类型不一致");
            }
        }
        else if (rule?.OssConfigId.HasValue == true)
        {
            config = await GetOssConfigByIdAsync(rule.OssConfigId.Value);
        }
        else
        {
            config = await GetActiveOssConfigAsync(provider);
        }
        provider = config.Provider;

        string bucketName;
        string rootPath;
        if (rule == null)
        {
            // 优先使用 bucketOverride，否则使用 ossConfig 中的 BucketName
            if (string.IsNullOrWhiteSpace(bucketOverride))
            {
                if (string.IsNullOrWhiteSpace(config.BucketName))
                    throw Oops.Oh("请先配置对应渠道的规则，或在对象存储配置中设置 Bucket");
                bucketName = config.BucketName;
            }
            else
            {
                bucketName = bucketOverride.Trim();
            }
            rootPath = NormalizeRootPathForStorage(string.IsNullOrWhiteSpace(rootOverride) ? config.RootPath : rootOverride);
        }
        else
        {
            bucketName = string.IsNullOrWhiteSpace(bucketOverride) ? rule.BucketName : bucketOverride.Trim();
            rootPath = NormalizeRootPathForStorage(string.IsNullOrWhiteSpace(rootOverride) ? rule.RootPath : rootOverride);
        }

        return new OssContext
        {
            Config = config,
            Rule = rule,
            NormalizedRoot = NormalizeStoredRoot(rootPath),
            BucketName = bucketName,
            Provider = config.Provider
        };
    }

    private async Task<List<ClientVersionRuleOutput>> BuildRuleOutputsAsync(IEnumerable<ClientVersionRule> rules)
    {
        var ruleList = rules.ToList();
        var ruleIds = ruleList.Select(r => r.Id).ToList();
        var relation = await _ruleWhitelistRep.AsQueryable()
            .Where(u => ruleIds.Contains(u.RuleId))
            .ToListAsync();

        var whitelistIds = relation.Select(r => r.WhitelistEntryId).Distinct().ToList();
        var whitelistDict = whitelistIds.Count == 0
            ? new Dictionary<long, ClientVersionWhitelist>()
            : (await _whitelistRep.AsQueryable().Where(u => whitelistIds.Contains(u.Id)).ToListAsync())
                .ToDictionary(u => u.Id, u => u);

        var outputs = new List<ClientVersionRuleOutput>();
        foreach (var rule in ruleList)
        {
            var linkedIds = relation.Where(r => r.RuleId == rule.Id).Select(r => r.WhitelistEntryId).ToList();
            var linkedCodes = linkedIds
                .Where(whitelistDict.ContainsKey)
                .Select(id => whitelistDict[id].MachineCode)
                .ToList();

            outputs.Add(new ClientVersionRuleOutput
            {
                RuleId = rule.Id,
                ChannelId = rule.ChannelId,
                ChannelName = rule.ChannelName,
                Platform = rule.Platform,
                Provider = string.IsNullOrWhiteSpace(rule.Provider) ? "aliyun" : rule.Provider,
                OssConfigId = rule.OssConfigId,
                BucketName = rule.BucketName,
                RootPath = rule.RootPath,
                ResourceDomain = rule.ResourceDomain,
                GeneralVersionInfo = DeserializeVersionInfo(rule.GeneralVersionInfoJson),
                WhitelistVersionInfo = DeserializeVersionInfo(rule.WhitelistVersionInfoJson),
                WhitelistTesterEntryIds = linkedIds,
                WhitelistMachineCodes = linkedCodes,
                LatestPublishResVersion = rule.LatestPublishResVersion,
                LatestPublishTime = rule.LatestPublishTime,
                LatestPublishUserName = rule.LatestPublishUserName,
                CreateTime = rule.CreateTime,
                UpdateTime = rule.UpdateTime
            });
        }

        return outputs;
    }

    private async Task PersistRuleWhitelistAsync(long ruleId, List<long>? entryIds)
    {
        await _ruleWhitelistRep.DeleteAsync(u => u.RuleId == ruleId);
        if (entryIds == null || entryIds.Count == 0) return;

        var uniqueIds = entryIds.Distinct().Where(id => id > 0).ToList();
        if (uniqueIds.Count == 0) return;

        var validEntries = await _whitelistRep.AsQueryable()
            .Where(u => uniqueIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        if (validEntries.Count == 0) return;

        var data = validEntries.Select(id => new ClientVersionRuleWhitelist
        {
            RuleId = ruleId,
            WhitelistEntryId = id,
            CreateTime = DateTime.Now,
            CreateUserId = _userManager.UserId,
            CreateUserName = _userManager.RealName ?? _userManager.Account
        }).ToList();

        await _ruleWhitelistRep.InsertRangeAsync(data);
    }

    private async Task<ClientAppVersionInfoDto?> NormalizeVersionInfoAsync(IObjectStorageClient client,
        ClientResourceOssConfig config,
        ClientVersionRule rule,
        ClientAppVersionInfoDto? info,
        string normalizedRoot,
        bool allowEmpty)
    {
        if (info == null)
        {
            return allowEmpty ? null : throw Oops.Oh("请完善版本信息");
        }

        info.AppVersion = info.AppVersion?.Trim();
        info.ResVersion = info.ResVersion?.Trim();
        if (string.IsNullOrWhiteSpace(info.AppVersion) || string.IsNullOrWhiteSpace(info.ResVersion))
            throw Oops.Oh("AppVersion / ResVersion 不能为空");

        info.UpdateResUrls = NormalizeUpdateResUrls(info.UpdateResUrls);
        await EnsureAppVersionExistsAsync(client, rule.BucketName, normalizedRoot, rule.Platform, info.AppVersion);
        await PopulateResVersionMetadataAsync(client, config, rule, info, normalizedRoot);
        return info;
    }

    private static List<string>? NormalizeUpdateResUrls(IEnumerable<string>? urls)
    {
        if (urls == null) return null;

        var normalized = urls
            .Select(url => url?.Trim())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized.Count > 0 ? normalized : null;
    }

    private async Task EnsureAppVersionExistsAsync(IObjectStorageClient client, string bucket, string normalizedRoot, ClientPlatform platform, string appVersion)
    {
        var prefix = BuildPrefix(normalizedRoot, platform, appVersion);
        try
        {
            var result = await client.ListObjectsAsync(bucket, new StorageListRequest
            {
                Prefix = prefix,
                MaxKeys = 1
            });

            if (result.ObjectSummaries.Count == 0)
                throw Oops.Oh($"AppVersion {appVersion} 尚未上传任何资源，请检查对象存储目录");
        }
        catch (ObjectStorageException ex)
        {
            throw Oops.Oh($"校验 AppVersion 失败：{ex.Message}");
        }
    }

    private async Task PopulateResVersionMetadataAsync(IObjectStorageClient client,
        ClientResourceOssConfig config,
        ClientVersionRule rule,
        ClientAppVersionInfoDto info,
        string normalizedRoot)
    {
        var key = CombineSegments(normalizedRoot, GetPlatformFolder(rule.Platform), info.AppVersion, info.ResVersion);
        try
        {
            var metadata = await client.GetObjectMetadataAsync(rule.BucketName, key);
            info.ResVersionFileSize = metadata.ContentLength;
            info.ResVersionTimestamp = info.ResVersionTimestamp ?? ExtractTimestamp(info.ResVersion) ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        catch (ObjectStorageException ex) when (ex.ErrorCode == ObjectStorageErrorCodes.NotFound)
        {
            throw Oops.Oh($"资源文件 {info.ResVersion} 不存在，请重新选择");
        }
        catch (ObjectStorageException ex)
        {
            throw Oops.Oh($"读取资源文件失败：{ex.Message}");
        }
    }

    private async Task<List<ChannelAppVersionItem>> ListAppVersionsAsync(IObjectStorageClient client, OssContext context, ClientPlatform platform)
    {
        var prefix = CombineSegments(context.NormalizedRoot, GetPlatformFolder(platform));
        var seen = new Dictionary<string, ChannelAppVersionItem>(StringComparer.OrdinalIgnoreCase);
        string? marker = null;

        do
        {
            var result = await client.ListObjectsAsync(context.BucketName, new StorageListRequest
            {
                Prefix = string.IsNullOrEmpty(prefix) ? null : $"{prefix}/",
                Delimiter = "/",
                Marker = marker,
                MaxKeys = 1000
            });

            foreach (var commonPrefix in result.CommonPrefixes)
            {
                var folderName = ExtractFolderName(commonPrefix, string.IsNullOrEmpty(prefix) ? null : $"{prefix}/");
                if (string.IsNullOrWhiteSpace(folderName)) continue;
                if (!seen.ContainsKey(folderName))
                {
                    seen[folderName] = new ChannelAppVersionItem
                    {
                        AppVersion = folderName,
                        Folder = commonPrefix
                    };
                }
            }

            marker = result.IsTruncated ? result.NextMarker : null;
        } while (!string.IsNullOrWhiteSpace(marker));

        return seen.Values
            .OrderByDescending(v => ParseVersion(v.AppVersion))
            .ThenByDescending(v => v.AppVersion, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<ChannelResVersionItem>> ListResVersionsAsync(IObjectStorageClient client, OssContext context, ClientPlatform platform, string appVersion)
    {
        if (string.IsNullOrWhiteSpace(appVersion))
            throw Oops.Oh("AppVersion 不能为空");

        var prefix = CombineSegments(context.NormalizedRoot, GetPlatformFolder(platform), appVersion);
        var bucket = context.BucketName;
        var list = new List<ChannelResVersionItem>();
        string? marker = null;

        do
        {
            var result = await client.ListObjectsAsync(bucket, new StorageListRequest
            {
                Prefix = $"{prefix}/",
                Marker = marker,
                MaxKeys = 1000
            });
            foreach (var obj in result.ObjectSummaries)
            {
                var fileName = obj.Key.Substring(obj.Key.LastIndexOf('/') + 1);
                if (!ResVersionRegex.IsMatch(fileName)) continue;

                list.Add(new ChannelResVersionItem
                {
                    FileName = fileName,
                    FileSize = obj.Size,
                    LastModified = obj.LastModified,
                    Timestamp = ExtractTimestamp(fileName),
                    DownloadUrl = BuildDownloadUrl(context.Rule, context.Config, bucket, obj.Key),
                    IsNewerThanOnline = IsNewerThanCurrent(fileName, context.Rule)
                });
            }

            marker = result.IsTruncated ? result.NextMarker : null;
        } while (!string.IsNullOrWhiteSpace(marker));

        return list
            .OrderByDescending(item => item.Timestamp ?? 0)
            .ThenByDescending(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool IsNewerThanCurrent(string fileName, ClientVersionRule? rule)
    {
        if (rule == null) return true;
        var timestamp = ExtractTimestamp(fileName) ?? 0;
        var current = DeserializeVersionInfo(rule.GeneralVersionInfoJson)?.ResVersionTimestamp ?? 0;
        return timestamp > current;
    }

    private static string BuildDownloadUrl(ClientVersionRule? rule, ClientResourceOssConfig config, string bucketName, string key)
    {
        var customDomain = rule?.ResourceDomain ?? config.ResourceDomain;
        if (!string.IsNullOrWhiteSpace(customDomain))
        {
            return $"{customDomain.TrimEnd('/')}/{key}";
        }

        var endpoint = config.Endpoint?.Trim();
        if (!string.IsNullOrWhiteSpace(endpoint) && endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return $"{endpoint.TrimEnd('/')}/{key}";
        }

        return $"https://{bucketName}.{endpoint}/{key}";
    }

    private static long? ExtractTimestamp(string fileName)
    {
        var match = ResVersionRegex.Match(fileName);
        if (!match.Success) return null;
        return long.TryParse(match.Groups[1].Value, out var ts) ? ts : null;
    }

    private static Version ParseVersion(string value)
    {
        return Version.TryParse(value, out var ver) ? ver : new Version(0, 0, 0, 0);
    }

    private static string ExtractFolderName(string commonPrefix, string? basePrefix)
    {
        var trimmed = commonPrefix.Trim('/');
        if (!string.IsNullOrWhiteSpace(basePrefix))
        {
            var baseTrim = basePrefix.Trim('/');
            if (trimmed.StartsWith(baseTrim, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(baseTrim.Length).Trim('/');
            }
        }
        return trimmed;
    }

    private static string GetPlatformFolder(ClientPlatform platform)
    {
        return platform switch
        {
            ClientPlatform.Android => "Android",
            ClientPlatform.Ios => "iOS",
            ClientPlatform.Mini => "WebGL",
            _ => "Android"
        };
    }

    private static string CombineSegments(params string?[] segments)
    {
        return string.Join('/', segments.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim('/')));
    }

    private static string NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return "aliyun";

        var normalized = provider.Trim().ToLowerInvariant();
        return normalized switch
        {
            "cos" or "tencent" or "tencentcloud" or "tencent-cos" => "tencent-cos",
            _ => normalized
        };
    }

    private static void EnsureProviderSupported(string provider)
    {
        if (!SupportedProviders.Contains(provider))
        {
            throw Oops.Oh($"暂不支持 {provider} 对象存储类型");
        }
    }

    private static string NormalizeRootPathForStorage(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || rootPath.Trim() == "/")
            return "/";

        var trimmed = rootPath.Trim().Trim('/');
        return $"/{trimmed}";
    }

    private static string NormalizeStoredRoot(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored) || stored.Trim() == "/")
            return string.Empty;

        return stored.Trim().Trim('/');
    }

    private static string NormalizeResourceDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return null;
        var trimmed = domain.Trim();
        return trimmed.EndsWith("/") ? trimmed.TrimEnd('/') : trimmed;
    }

    private static string BuildPrefix(string normalizedRoot, ClientPlatform platform, string appVersion)
    {
        return $"{CombineSegments(normalizedRoot, GetPlatformFolder(platform), appVersion)}/";
    }

    private static string MaskSecret(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret)) return string.Empty;
        if (secret.Length <= 4) return new string('*', secret.Length);
        return $"{new string('*', secret.Length - 4)}{secret[^4..]}";
    }

    private static ClientAppVersionInfoDto? DeserializeVersionInfo(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<ClientAppVersionInfoDto>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeVersionInfo(ClientAppVersionInfoDto? info)
    {
        return info == null ? null : JsonSerializer.Serialize(info, JsonOptions);
    }

    private static ClientAppVersionInfoDto CloneVersionInfo(ClientAppVersionInfoDto source)
    {
        return new ClientAppVersionInfoDto
        {
            AppVersion = source.AppVersion,
            ResVersion = source.ResVersion,
            ResVersionTimestamp = source.ResVersionTimestamp,
            ResVersionFileSize = source.ResVersionFileSize,
            UpdateUrl = source.UpdateUrl,
            UpdateResUrls = source.UpdateResUrls?.ToList(),
            UpdateNotice = source.UpdateNotice,
            LoginUrl = source.LoginUrl,
            Description = source.Description,
            ForceUpdate = source.ForceUpdate
        };
    }

    private static SqlSugarPagedList<TResult> ConvertPagedList<TSource, TResult>(
        SqlSugarPagedList<TSource> source,
        Func<TSource, TResult> selector)
    {
        return new SqlSugarPagedList<TResult>
        {
            Page = source.Page,
            PageSize = source.PageSize,
            Total = source.Total,
            TotalPages = source.TotalPages,
            HasNextPage = source.HasNextPage,
            HasPrevPage = source.HasPrevPage,
            Items = source.Items.Select(selector).ToList()
        };
    }

    #endregion

    #region 私有类型 - 对象存储

    private interface IObjectStorageClient : IDisposable
    {
        Task<bool> DoesBucketExistAsync(string bucket);

        Task<StorageListResult> ListObjectsAsync(string bucket, StorageListRequest request);

        Task<StorageObjectMetadata> GetObjectMetadataAsync(string bucket, string key);
    }

    private sealed class StorageListRequest
    {
        public string? Prefix { get; init; }
        public string? Delimiter { get; init; }
        public string? Marker { get; init; }
        public int? MaxKeys { get; init; }
    }

    private sealed class StorageListResult
    {
        public List<StorageObjectSummary> ObjectSummaries { get; init; } = new();
        public List<string> CommonPrefixes { get; init; } = new();
        public bool IsTruncated { get; set; }
        public string? NextMarker { get; set; }
    }

    private sealed class StorageObjectSummary
    {
        public string Key { get; init; }
        public long Size { get; init; }
        public DateTime? LastModified { get; init; }
    }

    private sealed class StorageObjectMetadata
    {
        public long ContentLength { get; init; }
        public DateTime? LastModified { get; init; }
    }

    private static class ObjectStorageErrorCodes
    {
        public const string NotFound = "NotFound";
    }

    private sealed class ObjectStorageException : Exception
    {
        public ObjectStorageException(string message, string? errorCode = null, Exception? inner = null)
            : base(message, inner)
        {
            ErrorCode = errorCode ?? string.Empty;
        }

        public string ErrorCode { get; }
    }

    private sealed class AliyunObjectStorageClient : IObjectStorageClient
    {
        private readonly OssClient _client;

        public AliyunObjectStorageClient(ClientResourceOssConfig config, string secret)
        {
            _client = new OssClient(config.Endpoint, config.AccessKeyId, secret);
        }

        public async Task<bool> DoesBucketExistAsync(string bucket)
        {
            try
            {
                return await Task.Run(() => _client.DoesBucketExist(bucket));
            }
            catch (OssException ex)
            {
                throw new ObjectStorageException(ex.Message, ex.ErrorCode, ex);
            }
        }

        public async Task<StorageListResult> ListObjectsAsync(string bucket, StorageListRequest request)
        {
            try
            {
                var ossRequest = new ListObjectsRequest(bucket)
                {
                    Prefix = request.Prefix,
                    Delimiter = request.Delimiter,
                    Marker = request.Marker,
                    MaxKeys = request.MaxKeys ?? 1000
                };

                var result = await Task.Run(() => _client.ListObjects(ossRequest));
                return new StorageListResult
                {
                    ObjectSummaries = result.ObjectSummaries.Select(obj => new StorageObjectSummary
                    {
                        Key = obj.Key,
                        Size = obj.Size,
                        LastModified = obj.LastModified
                    }).ToList(),
                    CommonPrefixes = result.CommonPrefixes?.ToList() ?? new List<string>(),
                    IsTruncated = result.IsTruncated,
                    NextMarker = result.IsTruncated ? result.NextMarker : null
                };
            }
            catch (OssException ex)
            {
                throw new ObjectStorageException(ex.Message, ex.ErrorCode, ex);
            }
        }

        public async Task<StorageObjectMetadata> GetObjectMetadataAsync(string bucket, string key)
        {
            try
            {
                var metadata = await Task.Run(() => _client.GetObjectMetadata(bucket, key));
                return new StorageObjectMetadata
                {
                    ContentLength = metadata.ContentLength,
                    LastModified = metadata.LastModified
                };
            }
            catch (OssException ex)
            {
                throw new ObjectStorageException(ex.Message, ex.ErrorCode, ex);
            }
        }

        public void Dispose()
        {
           
        }
    }

    private sealed class TencentCosStorageClient : IObjectStorageClient
    {
        private readonly CosXmlServer _cos;
        private readonly ILogger _logger;

        public TencentCosStorageClient(ClientResourceOssConfig config, string secret, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(config.AccessKeyId) || string.IsNullOrWhiteSpace(secret))
                throw new ObjectStorageException("请配置腾讯云 COS 的 SecretId 与 SecretKey");

            var region = ResolveCosRegion(config) ?? throw Oops.Oh("请配置腾讯云 COS Region");

            var builder = new CosXmlConfig.Builder()
                .IsHttps(true)
                .SetRegion(region)
                .SetConnectionTimeoutMs(60000)
                .SetReadWriteTimeoutMs(60000);

            var appId = ExtractCosAppId(config.BucketName);
            if (!string.IsNullOrWhiteSpace(appId))
            {
                builder.SetAppid(appId);
            }

            var cosConfig = builder.Build();
            var credentialProvider = new DefaultQCloudCredentialProvider(config.AccessKeyId, secret, 600);
            _cos = new CosXmlServer(cosConfig, credentialProvider);
            _logger = logger;
        }

        public void Dispose()
        {
            
        }

        public async Task<bool> DoesBucketExistAsync(string bucket)
        {
            var request = new HeadBucketRequest(bucket);
            try
            {
                await ExecuteAsync(() => _cos.HeadBucket(request));
                return true;
            }
            catch (CosServerException ex) when (ex.statusCode == 404)
            {
                return false;
            }
            catch (CosServerException ex)
            {
                throw new ObjectStorageException(ex.Message, ex.errorCode, ex);
            }
            catch (CosClientException ex)
            {
                throw new ObjectStorageException(ex.Message, null, ex);
            }
        }

        public async Task<StorageListResult> ListObjectsAsync(string bucket, StorageListRequest request)
        {
            var cosRequest = new GetBucketRequest(bucket);
            if (!string.IsNullOrEmpty(request.Prefix))
                cosRequest.SetPrefix(request.Prefix);
            if (!string.IsNullOrEmpty(request.Delimiter))
                cosRequest.SetDelimiter(request.Delimiter);
            if (!string.IsNullOrEmpty(request.Marker))
                cosRequest.SetMarker(request.Marker);
            if (request.MaxKeys.HasValue)
                cosRequest.SetMaxKeys(request.MaxKeys.Value.ToString(CultureInfo.InvariantCulture));

            try
            {
                var result = await ExecuteAsync(() => _cos.GetBucket(cosRequest));
                var listBucket = result.listBucket;
                return new StorageListResult
                {
                    IsTruncated = listBucket.isTruncated,
                    NextMarker = listBucket.nextMarker,
                    CommonPrefixes = listBucket.commonPrefixesList?.Select(p => p.prefix).Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>(),
                    ObjectSummaries = listBucket.contentsList?.Select(content => new StorageObjectSummary
                    {
                        Key = content.key,
                        Size = content.size,
                        LastModified = TryParseCosTime(content.lastModified)
                    }).ToList() ?? new List<StorageObjectSummary>()
                };
            }
            catch (CosServerException ex)
            {
                throw new ObjectStorageException(ex.Message, ex.errorCode, ex);
            }
            catch (CosClientException ex)
            {
                throw new ObjectStorageException(ex.Message, null, ex);
            }
        }

        public async Task<StorageObjectMetadata> GetObjectMetadataAsync(string bucket, string key)
        {
            var request = new HeadObjectRequest(bucket, key);
            try
            {
                var result = await ExecuteAsync(() => _cos.HeadObject(request));
                return new StorageObjectMetadata
                {
                    ContentLength = result.size,
                    LastModified = TryParseCosTime(result.lastModified)
                };
            }
            catch (CosServerException ex) when (ex.statusCode == 404)
            {
                throw new ObjectStorageException($"对象 {key} 不存在", ObjectStorageErrorCodes.NotFound, ex);
            }
            catch (CosServerException ex)
            {
                throw new ObjectStorageException(ex.Message, ex.errorCode, ex);
            }
            catch (CosClientException ex)
            {
                throw new ObjectStorageException(ex.Message, null, ex);
            }
        }

        private static Task<T> ExecuteAsync<T>(Func<T> action)
        {
            return Task.Run(action);
        }
    }

    private static string? ResolveCosRegion(ClientResourceOssConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Region))
            return config.Region.Trim();

        var endpoint = config.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
            return null;

        if (!endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            endpoint = $"https://{endpoint}";

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            var match = Regex.Match(host, @"cos\.([^.]+)\.myqcloud\.com", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;
            if (!host.Contains('.'))
                return host;
        }

        return endpoint.Trim('/');
    }

    private static string? ExtractCosAppId(string? bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) return null;
        var parts = bucketName.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var last = parts.LastOrDefault();
        return !string.IsNullOrEmpty(last) && last.All(char.IsDigit) ? last : null;
    }

    private static DateTime? TryParseCosTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result))
            return result;
        return null;
    }


    #endregion
    #region 私有方法 - RPC 同步

    /// <summary>
    /// 同步版本规则到 CenterServer
    /// </summary>
    private async Task SyncVersionRuleToCenterAsync(ClientVersionRule rule, List<string> whitelistMachineCodes)
    {
        try
        {
            // 同步正式版本规则
            var generalInfo = DeserializeVersionInfo(rule.GeneralVersionInfoJson);
            if (generalInfo != null)
            {
                var generalRequest = new CenterVersionRuleReq
                {
                    RuleId = $"{rule.ChannelId}_{rule.Platform}_General",
                    ChannelId = rule.ChannelId,
                    ChannelName = rule.ChannelName,
                    Platform = GetPlatformString(rule.Platform),
                    RuleType = ClientVersionRuleType.General,
                    WhitelistMachineCodes = Array.Empty<string>(),
                    VersionInfo = new AppVersionInfo
                    {
                        AppVersion = generalInfo.AppVersion ?? string.Empty,
                        ResVersion = NormalizeResVersionForServer(generalInfo.ResVersion),
                        UpdateUrl = generalInfo.UpdateUrl ?? string.Empty,
                        UpdateResUrls = generalInfo.UpdateResUrls?.ToList() ?? new List<string>(),
                        ChannelName = rule.ChannelName,
                        UpdateNotice = generalInfo.UpdateNotice ?? string.Empty,
                        LoginUrl = generalInfo.LoginUrl ?? string.Empty
                    }
                };
                await _rpcClient.UpsertAppVersionRuleAsync(generalRequest);
            }

            // 同步白名单测试版本规则
            var whitelistInfo = DeserializeVersionInfo(rule.WhitelistVersionInfoJson);
            if (whitelistInfo != null)
            {
                var whitelistRequest = new CenterVersionRuleReq
                {
                    RuleId = $"{rule.ChannelId}_{rule.Platform}_Whitelist",
                    ChannelId = rule.ChannelId,
                    ChannelName = rule.ChannelName,
                    Platform = GetPlatformString(rule.Platform),
                    RuleType = ClientVersionRuleType.Whitelist,
                    WhitelistMachineCodes = whitelistMachineCodes?.ToArray() ?? Array.Empty<string>(),
                    VersionInfo = new AppVersionInfo
                    {
                        AppVersion = whitelistInfo.AppVersion ?? string.Empty,
                        ResVersion = NormalizeResVersionForServer(whitelistInfo.ResVersion),
                        UpdateUrl = whitelistInfo.UpdateUrl ?? string.Empty,
                        UpdateResUrls = whitelistInfo.UpdateResUrls?.ToList() ?? new List<string>(),
                        ChannelName = rule.ChannelName,
                        UpdateNotice = whitelistInfo.UpdateNotice ?? string.Empty,
                        LoginUrl = whitelistInfo.LoginUrl ?? string.Empty
                    }
                };
                await _rpcClient.UpsertAppVersionRuleAsync(whitelistRequest);
            }

            _logger.LogInformation("成功同步版本规则到 CenterServer: ChannelId={ChannelId}, Platform={Platform}", rule.ChannelId, rule.Platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步版本规则到 CenterServer 失败: ChannelId={ChannelId}, Platform={Platform}", rule.ChannelId, rule.Platform);
            // 不抛出异常，避免影响主流程
        }
    }

    /// <summary>
    /// 从 CenterServer 删除版本规则
    /// </summary>
    private async Task DeleteVersionRuleFromCenterAsync(string channelId, ClientPlatform platform)
    {
        try
        {
            await _rpcClient.DeleteAppVersionRuleAsync($"{channelId}_{platform}_General");
            await _rpcClient.DeleteAppVersionRuleAsync($"{channelId}_{platform}_Whitelist");
            _logger.LogInformation("成功从 CenterServer 删除版本规则: ChannelId={ChannelId}, Platform={Platform}", channelId, platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 CenterServer 删除版本规则失败: ChannelId={ChannelId}, Platform={Platform}", channelId, platform);
            // 不抛出异常，避免影响主流程
        }
    }

    private static string GetPlatformString(ClientPlatform platform)
    {
        return platform switch
        {
            ClientPlatform.Android => "Android",
            ClientPlatform.Ios => "iOS",
            ClientPlatform.Mini => "Mini",
            _ => "Android"
        };
    }

    private static string NormalizeResVersionForServer(string? resVersion)
    {
        if (string.IsNullOrWhiteSpace(resVersion)) return string.Empty;

        var match = ResVersionRegex.Match(resVersion.Trim());
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return resVersion.Trim();
    }

    #endregion
}
