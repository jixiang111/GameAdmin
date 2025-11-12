## GM RPC 接口说明

以下接口通过 MagicOnion/gRPC 暴露，供后台管理系统调用。接口分布在 **游戏服** 与 **CenterServer** 两侧，调用前请先配置 IP 白名单与 GM Token。

---

### 1. 运行依赖
- .NET 6+ + Grpc.Net.Client + MagicOnion.Client
- MessagePack（MagicOnion 自带）
- 若跨公网需启用 TLS/HTTP2
- 配置文件：
  - 游戏服：Geek.Server.App/Configs/app_config.json
  - CenterServer：Geek.Server.CenterServer/Configs/app_config.json

---

### 2. 安全要求
1. **固定 IP 白名单**：GmRpcIpWhitelist（默认只允许 127.0.0.1）。将后台服务器出口 IP 写入数组后重启或热加载。
2. **Token 校验**：设置 GmRpcToken（非空即启用），调用时在 gRPC Metadata 中附加 x-gm-token: <值>。
3. 建议后台侧记录调用日志/审批，公网环境配合 VPN、mTLS、限流等手段。

---

### 3. 调用示例
```csharp
var channel = GrpcChannel.ForAddress("https://<host>:<GrpcPort>");
var headers = new Metadata { { "x-gm-token", "<token>" } };
var ctx = CallContext.Default.WithHeaders(headers);

var gmClient     = MagicOnionClient.Create<IGmService>(channel).WithContext(ctx);
var centerClient = MagicOnionClient.Create<ICenterGmService>(channel).WithContext(ctx);
```

---

### 4. 游戏服 GM 接口（Server.Logic.Logic.RPCServices）
| 接口 | 说明 |
| --- | --- |
| ListPlayers(int pageIndex, int pageSize) | 分页列出玩家，pageIndex ≥ 1，pageSize ∈ [1,100]。返回 Total/HasMore/Items(GmRoleSummary) |
| GetPlayerInfo(long roleId) | 查询指定角色详情（含登录/离线时间、ReconnectToken 等） |

> 时间字段均为 UTC ticks。若出现 PermissionDenied，先检查白名单和 Token；如提示未找到角色，说明 roleId 无效或已清档。

---

### 5. CenterServer GM 接口（Geek.Server.CenterServer.Logic.RPCServices）

#### 5.1 游戏服地址管理
| 接口 | 说明 |
| --- | --- |
| GetGameServerList() | 返回所有游戏服地址（CenterServerInfoDto） |
| UpdateGameServer(CenterServerUpdateReq) | 按 ServerId 更新 IP/端口/WebSocket/Grpc，立即写入 Configs/gameserver_config.json |

#### 5.2 客户端版本配置（AppVersionInfo）
| 接口 | 说明 |
| --- | --- |
| ListAppVersionRules() | 列出全部版本规则（包含 ChannelId、Platform、RuleType、WhitelistMachineCodes、AppVersionInfo） |
| UpsertAppVersionRule(CenterVersionRuleReq) | 新增或更新规则，指定渠道/平台/规则类型（默认或白名单）及白名单机器码，落盘到 app_version_config.json |
| DeleteAppVersionRule(string ruleId) | 删除规则；若存在白名单版本必须先删除白名单再删默认版本 |

版本规则约束：
1. 平台仅支持 `android` / `ios` / `mini`；
2. 同一渠道 + 平台**必须**有且仅有 1 个默认版本；
3. 每个渠道 + 平台**最多 1 个**白名单版本，且需配置至少一个机器码；
4. 白名单命中后优先返回白名单版本，否则回退默认版本。详见 ClientVersion_API.md。

后台接入建议：
- 在 Admin.NET 等后台项目中复用 `CenterVersionRuleReq/Dto` 定义，保持 MessagePack 契约一致；
- 交互上可将“默认版本”和“白名单版本”拆分为两个卡片，限制每个平台最多各一条；
- Upsert 前校验渠道 + 平台组合是否已存在默认/白名单，减少无意义调用；
- 白名单机器码建议统一为小写或标准格式，避免大小写导致的无法命中；
- 删除默认版本前需确认对应白名单规则已删除；可在前端做拦截提示；
- GM RPC 调用同样遵循安全要求，注意在 `GrpcChannel` 上加 `x-gm-token` 以及开启 HTTP/2。

#### 5.3 客户端资源 OSS 凭证配置

CenterServer 现在内置多云 OSS 抽象（默认阿里云），所有客户端资源的上传、列举以及版本校验都会通过 GM RPC 统一完成。为了让前端能够配置生效凭证，新增如下接口：

| 接口 | 说明 |
| --- | --- |
| GetClientOssConfig(OssProviderQuery req) | 查询当前 provider 的 AccessKeyId、Endpoint、Bucket 等信息，`Provider` 缺省为 `aliyun`。 |
| UpsertClientOssConfig(ClientOssConfigReq request) | 设置或更新 OSS 凭证，落盘至 `Geek.Server.CenterServer/Configs/client_oss_config.json` 并刷新 OSS Service 缓存。 |

`ClientOssConfigDto/Req` 字段说明：
- `Provider`：字符串，当前仅支持 `aliyun`，后续可扩展到 `cos`、`obs` 等，前端用下拉枚举。
- `AccessKeyId`、`AccessKeySecret`：阿里云账号凭证，`AccessKeySecret` 在查询结果中只返回 `******1234` 形式的掩码，只有重新填写才会更新。
- `Endpoint`：OSS 访问域名，例如 `oss-cn-shanghai.aliyuncs.com`，用于列目录和签名上传。
- `BucketName`：客户端资源所在桶，影响后续版本扫描。
- `RootPath`：桶内的资源根路径，默认 `/`，CenterServer 会在其下按平台创建 `Android/iOS/WebGL` 子目录。
- `ResourceDomain`：可选，前端展示 CDN/下载域名时使用。
- `IsDefault`：布尔值，标识该配置是否作为客户端资源默认访问凭证。

前端交互要求：
1. 在“客户端资源”页签新增“OSS 凭证”卡片，包含上述字段，保存时调用 `UpsertClientOssConfig`，成功后刷新列表。
2. 除 `Provider` 外其他字段均需要必填校验；`AccessKeySecret` 为空时表示沿用旧值，不允许在界面上明文展示历史密钥。
3. 凭证更新后需要提示“会影响所有客户端版本规则与资源列表”，并在调用接口前提示确认。
4. 所有读取 OSS 目录、校验版本文件的 RPC 会自动使用这份配置，前端无需直接调用 OSS SDK。

#### 5.4 客户端测试白名单管理

为了让部分客户端提前拉取测试版本，后台提供独立的白名单资源表，前端按以下接口对接：

| 接口 | 说明 |
| --- | --- |
| ListClientWhitelist(ClientWhitelistQuery req) | 分页查询白名单，支持 machineCode/remark 模糊搜索，支持 `onlyEnabled` 过滤。 |
| UpsertClientWhitelist(ClientWhitelistUpsertReq req) | 新增或编辑白名单条目，`EntryId` 为空表示新增。 |
| DeleteClientWhitelist(string entryId) | 删除指定白名单（逻辑删除），不可批量物理删除。 |

`ClientWhitelistEntryDto` 字段说明：
- `EntryId`：后台生成的 GUID，作为前端多选、批量操作的主键。
- `MachineCode`：必填，32~64 位，仅限大写字母/数字/连字符，用于和客户端上报机器码匹配，必须全局唯一。
- `Remark`：可选，记录申请来源、负责人等信息。
- `Tags`：字符串数组，前端可用多选标签控件，用于后续筛选。
- `Enabled`：布尔值，禁用后不会参与任何渠道规则。
- `CreatedBy`、`CreatedAt`、`UpdatedAt`：审计信息，列表/详情中只读展示。

前端交互要求：
1. 在“客户端版本管理”新增“测试白名单”页签，支持 CRUD、批量导入（可选 CSV/XLSX）、导出当前筛选结果。
2. 列表支持复选，渠道规则弹窗在添加测试用户时直接复用该列表（通过 `ListClientWhitelist` 的 `ids` 参数回填）。
3. 保存前做最小长度与字符集校验，重复机器码由后台返回 `DuplicateMachineCode` 错误码并提示运营人员。
4. 支持一键复制机器码和备注，方便运营与测试沟通。

#### 5.5 渠道客户端版本规则（增强）

在既有 `ListAppVersionRules/UpsertAppVersionRule` 能力的基础上，新增以下字段与流程：

- `CenterVersionRuleDto` 新增：
  - `WhitelistVersionInfo`：白名单测试使用的 `AppVersionInfo`。
  - `GeneralVersionInfo`：正式用户使用的 `AppVersionInfo`（原 `VersionInfo` 功能）。
  - `WhitelistTesterEntryIds`：引用 5.4 白名单的 `EntryId` 列表。
  - `ChannelBucket`：包含 `BucketName`、`RootPath`、`PlatformFolder` 的只读信息。
  - `LatestPublish`：最近一次从白名单发布到正式的时间和操作者。
- `CenterVersionRuleReq` 同步扩展，对应字段在保存时全部必填。
- 新增 `PromoteWhitelistVersion(PromoteWhitelistVersionReq req)`，用于将白名单配置一键复制到正式版本。
- 新增 `ListChannelOssAppVersions(ChannelOssAppVersionQuery req)` 和 `ListChannelOssResVersions(ChannelOssResVersionQuery req)`，分别列出可选的 AppVersion 目录和 `versions_<timestamp>.json` 资源描述文件。

OSS 目录约定：

```text
<bucket>/
├─ Android/
│  └─ 1.0.1/
│     ├─ versions_11105163809.json
│     └─ versions_11105165652.json
├─ iOS/
│  └─ 1.0.1/
└─ WebGL/            # 对应小程序平台（mini）
```

约束与前端处理：
1. `ChannelId + Platform` 仍然是唯一键，`Platform` 取值固定为 `android`、`ios`、`mini`（界面上可显示为 Android / 苹果 / 小程序）。
2. `AppVersionInfo.AppVersion` 只能从 `ListChannelOssAppVersions` 返回的目录中选择；后台会校验存在性，前端不再允许手输。
3. `AppVersionInfo.ResVersion` 填写 `versions_<timestamp>.json` 文件名，`ResVersionTimestamp`（去掉前缀后的数字）由后台返回，用于比较大小。正式用户只能选择 **比当前线上更大的**资源版本，前端需将较小值置灰并提示“需选择更高版本”；白名单版本不受此限制。
4. `AppVersionInfo` 结构扩展：在原有 `AppVersion/ResVersion/UpdateUrl/UpdateNotice/LoginUrl` 的基础上新增 `ForceUpdate`（bool）、`Description`（字符串，用于版本说明）、`ResVersionTimestamp`（long，来源文件名）、`ResVersionFileSize`（long，可选，用于展示）。
5. `WhitelistTesterEntryIds` 来自 5.4 的白名单列表，可复选并支持关键字筛选；保存时后台会把对应机器码写入规则。
6. `ChannelBucket` 信息（Bucket、RootPath、PlatformFolder）由后台下发，前端只读展示，帮助运营确认当前规则指向的 OSS 目录；如需调整桶，需在渠道配置处变更后再刷新此页。
7. “白名单测试版本 -> 正式版本”需要二次确认：点击按钮先弹出对比弹窗（展示旧正式版本与白名单版本的 AppVersion、ResVersion、资源文件、更新说明），用户勾选确认后再调用 `PromoteWhitelistVersion`。后台会校验资源版本递增并更新 `GeneralVersionInfo`，同时返回 `LatestPublish`。
8. 发布后应自动刷新规则列表，并提示“发布成功，本次资源版本：versions_xxx”。

前端补充校验：
- 当 `WhitelistVersionInfo` 或 `GeneralVersionInfo` 的 `AppVersion` 与 OSS 目录不匹配时应立即阻止保存。
- 资源文件列表中的 `versions_<timestamp>.json` 需显示打包时间（时间戳转本地时间）和文件大小，方便运营判断优先级。
- 正常用户的资源版本选择后，如果存在更高版本但未发布，可在列表中显示黄色标签提醒。

---

### 6. 配置示例
```json
{
  "GrpcPort": 30000,
  "GmRpcIpWhitelist": [ "127.0.0.1", "10.0.0.5" ],
  "GmRpcToken": "gm-secret-token"
}
```
上线前确认：
1. 后台 IP 已加入白名单；
2. Header x-gm-token 与配置一致；
3. host:port 指向正确环境；
4. 代理可正确透传客户端 IP 或已限制可信代理。

---

### 7. 补充建议
- 所有 GM 操作请记录日志并增加审批/二次确认。
- 高危操作（封号、强更、发放道具）建议拆分成更细的接口并限制权限。
- 若需开放公网入口，可结合 VPN、mTLS、限流、WAF 等手段进一步保护。
