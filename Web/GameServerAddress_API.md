# 游戏服地址管理 API（后台前端对接）

## 基础说明
- 服务端使用 DynamicApi，默认路由格式：`/api/{service}/{action}`，因此该模块的前缀为 `/api/gameServerAddress`（首字母小写）。
- 所有响应统一包裹在 `AdminResult` 中（`{ code, type, message, data }`），前端只需关注 `data` 字段。
- 后端会自动携带 GM RPC 所需的 `x-gm-token`，前端无需关注，确保 `App.json` 中的 `GmRpc.CenterServer` 已配置即可。

## 接口列表

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 列表 | `GET /api/gameServerAddress/list` | 无 | `GameServerAddressOutput[]` |
| 详情 | `GET /api/gameServerAddress/detail` | Query: `serverId` | `GameServerAddressOutput` |
| 新增 | `POST /api/gameServerAddress/add` | Body: `GameServerAddressCreateInput` | `GameServerAddressOutput` |
| 更新 | `POST /api/gameServerAddress/update` | Body: `GameServerAddressUpdateInput` | `GameServerAddressOutput` |
| 删除 | `POST /api/gameServerAddress/delete` | Body: `{ serverId }` | `void` |

### 数据结构

```jsonc
// GameServerAddressCreateInput & GameServerAddressUpdateInput
{
  "serverId": 1001,            // int, 必填，CenterServer 中的唯一编号
  "serverName": "一区",        // string, 必填，<=64
  "ip": "10.0.0.11",          // string, 必填
  "port": 8700,               // int, >0
  "wsPort": 8800,             // int，可选（传 0 表示无）
  "grpcPort": 30000,          // int，可选
  "wsUrl": "ws://10.0.0.11:8800",   // string，可选，不填则由后端拼接 `ws://{ip}:{wsPort}`
  "grpcUrl": "https://10.0.0.11:30000", // string，可选，不填则由后端拼接 `http://{ip}:{grpcPort}`
  "remark": "双线一区",        // string，可选，<=256
  "enabled": true             // bool，可选，默认为 true
}

// GameServerAddressOutput（列表、详情、返回值）
{
  "serverId": 1001,
  "serverName": "一区",
  "ip": "10.0.0.11",
  "port": 8700,
  "wsPort": 8800,
  "wsUrl": "ws://10.0.0.11:8800",
  "grpcPort": 30000,
  "grpcUrl": "https://10.0.0.11:30000",
  "remark": "双线一区",
  "enabled": true
}
```

### 示例

**新增**
```http
POST /api/gameServerAddress/add
Content-Type: application/json

{
  "serverId": 1001,
  "serverName": "一区",
  "ip": "10.0.0.11",
  "port": 8700,
  "wsPort": 8800,
  "grpcPort": 30000,
  "remark": "双线一区",
  "enabled": true
}
```

**删除**
```http
POST /api/gameServerAddress/delete
Content-Type: application/json

{ "serverId": 1001 }
```

> 返回值示例：`{ "code": 200, "type": "success", "message": "OK", "data": null }`

## 前置要求
1. `Admin.NET.Application/Configuration/App.json` 中配置正确的 CenterServer gRPC 地址、Token 以及证书策略。
2. CenterServer 需允许后台所在服务器 IP 通过白名单（`GmRpcIpWhitelist`）。
3. 如需区分租户或环境，可在前端利用 `enabled` 字段控制展示或在后台完善更多元数据。

---

# 客户端版本管理 API（后台前端对接）

## 模块说明
- 同样基于 DynamicApi，统一前缀 `/api/clientVersion`。
- 所有响应返回 `AdminResult<T>`，只需要关注 `data`。
- 模块内部会通过 `ICenterServerRpcClient` 调用 GM RPC，所以前端无需自行拼装 gRPC Header。
- 该模块拆分为「OSS 凭证」「测试白名单」「渠道版本规则」三个子域。

## 1. OSS 凭证管理

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 查询 | `GET /api/clientVersion/oss-config` | Query: `provider`（可选，默认 `aliyun`） | `ClientOssConfigOutput` |
| 保存 | `POST /api/clientVersion/oss-config` | Body: `ClientOssConfigInput` | `ClientOssConfigOutput` |

```jsonc
// ClientOssConfigInput
{
  "provider": "aliyun",                 // string, 必填，当前仅支持 aliyun
  "accessKeyId": "LTAIxxx",             // string, 必填
  "accessKeySecret": "xxxxxxxx",        // string, 必填；保存后不再回显明文
  "endpoint": "oss-cn-shanghai.aliyuncs.com", // string, 必填
  "bucketName": "client-resource-prod", // string, 必填
  "rootPath": "/",                      // string, 可选，默认为 /
  "resourceDomain": "https://cdn.xxx.com", // string, 可选
  "isDefault": true                     // bool, 可选，默认为 true
}

// ClientOssConfigOutput
{
  "provider": "aliyun",
  "accessKeyId": "LTAIxxx",
  "accessKeySecretMasked": "******3F8B", // 仅返回掩码
  "endpoint": "oss-cn-shanghai.aliyuncs.com",
  "bucketName": "client-resource-prod",
  "rootPath": "/",
  "resourceDomain": "https://cdn.xxx.com",
  "isDefault": true,
  "updatedAt": "2024-11-11T12:00:00Z",
  "updatedBy": "admin"
}
```

> 提示：保存前需二次确认，提示“更新后会影响所有客户端资源读取与版本列表”。

## 2. 测试白名单

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 分页列表 | `GET /api/clientVersion/whitelist/page` | Query: `pageNo`, `pageSize`, `keyword`, `tag`, `onlyEnabled` | `PagedResult<ClientWhitelistEntryOutput>` |
| 新增/编辑 | `POST /api/clientVersion/whitelist/save` | Body: `ClientWhitelistUpsertInput` | `ClientWhitelistEntryOutput` |
| 删除 | `POST /api/clientVersion/whitelist/delete` | Body: `{ "entryId": "guid" }` | `void` |
| 批量导入 | `POST /api/clientVersion/whitelist/import` | Form: `fileId`（引用通用上传结果） | `ImportPreviewOutput` |
| 导出 | `GET /api/clientVersion/whitelist/export` | Query 同列表；返回 `fileId` | `ExportResult` |

```jsonc
// ClientWhitelistUpsertInput
{
  "entryId": null,               // string, 可选；为空表示新增
  "machineCode": "ABCD-1234-XXXX-9999", // string, 必填，32~64 位，大写字母/数字/-
  "remark": "安卓渠道测试",       // string, 可选
  "tags": ["android", "qa"],     // string[]，可选
  "enabled": true                // bool，默认 true
}

// ClientWhitelistEntryOutput
{
  "entryId": "ba9e1c7a-8945-4f7b-9ce6-09b8d0ef52f5",
  "machineCode": "ABCD-1234-XXXX-9999",
  "remark": "安卓渠道测试",
  "tags": ["android", "qa"],
  "enabled": true,
  "createdBy": "qa_admin",
  "createdAt": "2024-11-11T12:00:00Z",
  "updatedAt": "2024-11-11T12:30:00Z"
}
```

校验提示：
1. `machineCode` 不允许重复，若后台返回 `DuplicateMachineCode`，需在界面提示“机器码已存在”。
2. 导入时后台会先进行格式校验并返回预览页（成功/失败条数），前端需提示用户确认后再执行落表操作。
3. 渠道版本规则弹窗中选择白名单时可通过 `ids` 参数直接调用列表接口复用数据。

## 3. 渠道版本规则

### 3.1 规则 CRUD

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 列表 | `GET /api/clientVersion/rule/list` | Query: `channelId`, `platform`, `ruleType`（可选） | `ClientVersionRuleOutput[]` |
| 详情 | `GET /api/clientVersion/rule/detail` | Query: `ruleId` | `ClientVersionRuleOutput` |
| 新增/更新 | `POST /api/clientVersion/rule/save` | Body: `ClientVersionRuleSaveInput` | `ClientVersionRuleOutput` |
| 删除 | `POST /api/clientVersion/rule/delete` | Body: `{ "ruleId": "..." }` | `void` |
| 白名单发布 | `POST /api/clientVersion/rule/promote` | Body: `PromoteWhitelistVersionInput` | `ClientVersionRuleOutput` |

```jsonc
// ClientVersionRuleSaveInput
{
  "ruleId": null,                     // string, 可选
  "channelId": "oppo",
  "channelName": "OPPO 渠道",
  "platform": "android",              // android | ios | mini
  "ruleType": "general",              // general | whitelist（默认 general）
  "whitelistTesterEntryIds": ["ba9e1c7a-..."], // 仅当 ruleType = whitelist 时必填
  "generalVersionInfo": null,         // ruleType = general 时必填
  "whitelistVersionInfo": {
    "appVersion": "1.0.2",
    "resVersion": "versions_11105165652.json",
    "resVersionTimestamp": 11105165652,
    "updateUrl": "https://cdn.xxx.com/apk/oppo_1.0.2.apk",
    "updateNotice": "修复若干已知问题",
    "loginUrl": "https://login.xxx.com",
    "description": "OPPO 白名单测试包",
    "forceUpdate": false
  }
}

// ClientVersionRuleOutput（核心字段）
{
  "ruleId": "rule-oppo-android-general",
  "channelId": "oppo",
  "channelName": "OPPO 渠道",
  "platform": "android",
  "ruleType": "general",
  "generalVersionInfo": { ... },
  "whitelistVersionInfo": { ... },         // 当 ruleType = whitelist 时返回
  "whitelistTesterEntryIds": ["ba9e1c7a-..."],
  "whitelistMachineCodes": ["ABCD-1234-XXXX-9999"],
  "channelBucket": {
    "bucketName": "client-resource-prod",
    "rootPath": "/",
    "platformFolder": "Android"
  },
  "latestPublish": {
    "operator": "ops_admin",
    "publishedAt": "2024-11-11T13:00:00Z",
    "resVersion": "versions_11105165652.json"
  }
}
```

> 规则约束：同一 `channelId + platform` 只能存在 1 条默认版本（general）与 1 条白名单版本（whitelist）。删除默认版本前需先删除对应白名单。

### 3.2 OSS 版本列表

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 列出 AppVersion | `GET /api/clientVersion/rule/appVersions` | Query: `channelId`, `platform` | `ChannelAppVersionItem[]` |
| 列出资源版本 | `GET /api/clientVersion/rule/resVersions` | Query: `channelId`, `platform`, `appVersion` | `ChannelResVersionItem[]` |

```jsonc
// ChannelAppVersionItem
{
  "appVersion": "1.0.2",
  "folder": "Android/1.0.2",
  "lastModified": "2024-11-11T11:00:00Z"
}

// ChannelResVersionItem
{
  "fileName": "versions_11105165652.json",
  "timestamp": 11105165652,
  "fileSize": 24567,
  "downloadUrl": "https://cdn.xxx.com/Android/1.0.2/versions_11105165652.json",
  "isNewerThanOnline": true    // 后台根据当前 general 版本计算
}
```

选择规则：
1. AppVersion 必须来自可用目录，不允许手动输入。
2. 正式用户（general）仅能选择 `isNewerThanOnline = true` 的资源版本；白名单无限制。
3. 页面需显示 `timestamp` 转换后的本地时间，方便运营判断最新打包。

### 3.3 发布流程
1. 前端点击“白名单发布为正式”时先调用 `GET /api/clientVersion/rule/detail` 获取最新数据，展示对比弹窗（旧正式 VS 白名单）。
2. 用户勾选确认后调用 `POST /api/clientVersion/rule/promote`，Body：

```json
{
  "ruleId": "rule-oppo-android-general",
  "targetResVersion": "versions_11105165652.json",   // 可选，默认取当前白名单 resVersion
  "confirm": true
}
```

3. 后端会校验资源版本递增、白名单信息是否存在；若校验失败会返回业务码（`ResVersionNotAhead`、`WhitelistMissing` 等）。
4. 发布成功后刷新列表并提示“发布成功，本次资源版本：versions_xxx”。

---
