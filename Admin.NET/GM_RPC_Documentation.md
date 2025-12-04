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

AppVersionInfo 字段补充：
- `UpdateUrl`：整包更新地址；
- `UpdateResUrls` (`List<string>` )：资源增量/热更地址列表，可配置多个以便客户端按顺序尝试并在 GM 后台编辑时一并回传。

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
