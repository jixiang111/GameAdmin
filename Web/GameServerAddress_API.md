# 游戏服地址管理 API（后台前端对接）

## 基础说明
- 服务端使用 DynamicApi，默认路由格式：`/api/{service}/{action}`，因此该模块的前缀为 `/api/gameServerAddress`（首字母小写）。
- 所有响应统一包在 `AdminResult` 中（`{ code, type, message, data }`），前端只需关注 `data` 字段。
- 后端会自动携带 GM RPC 所需的 `x-gm-token`，前端无需关心，保证 `App.json` 中的 `GmRpc.CenterServer` 配置正确即可。

## 接口列表

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 列表 | `GET /api/gameServerAddress/list` | 无 | `GameServerAddressOutput[]` |
| 详情 | `GET /api/gameServerAddress/detail` | Query: `serverId` | `GameServerAddressOutput` |
| 新增 | `POST /api/gameServerAddress/add` | Body: `GameServerAddressCreateInput` | `GameServerAddressOutput` |
| 更新 | `POST /api/gameServerAddress/update` | Body: `GameServerAddressUpdateInput` | `GameServerAddressOutput` |
| 删除 | `POST /api/gameServerAddress/delete` | Body: `{ "serverId": number }` | `void` |

### 数据结构

```jsonc
// GameServerAddressCreateInput / UpdateInput
{
  "serverId": 1001,            // int，必填，CenterServer 中的唯一编号
  "serverName": "一区",         // string，必填，<=64
  "ip": "10.0.0.11",           // string，必填
  "port": 8700,                // int，>0
  "wsPort": 8800,              // int，可选（=0 表示无）
  "grpcPort": 30000,           // int，可选（=0 表示无）
  "wsUrl": "ws://10.0.0.11:8800",   // string，可选，不填则后端拼接 ws://{ip}:{wsPort}
  "grpcUrl": "http://10.0.0.11:30000", // string，可选，不填则后端拼接 http://{ip}:{grpcPort}
  "remark": "双线一区",          // string，可选，<=256
  "enabled": true              // bool，可选，默认 true
}

// GameServerAddressOutput（列表/详情/返回值）
{
  "serverId": 1001,
  "serverName": "一区",
  "ip": "10.0.0.11",
  "port": 8700,
  "wsPort": 8800,
  "wsUrl": "ws://10.0.0.11:8800",
  "grpcPort": 30000,
  "grpcUrl": "http://10.0.0.11:30000",
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
1. `Admin.NET.Application/Configuration/App.json` 中配置正确的 CenterServer 地址、Token 以及证书策略。
2. CenterServer 需允许后台所在服务器 IP 通过白名单（`GmRpcIpWhitelist`）。
3. 如需区分租户/环境，可在前端利用 `enabled` 字段控制展示或在后台扩展更多字段。

---

# 客户端版本管理 API（后台前端对接）

## 模块说明
- 基于 DynamicApi，统一前缀 `/api/clientVersion`，动作为小驼峰。
- 所有响应返回 `AdminResult<T>`，只需关注 `data`。
- 模块内部通过 `ICenterServerRpcClient` 调 GM RPC，前端无需处理 gRPC Header。
- 领域拆分：`OSS 凭证`、`测试白名单`、`渠道版本规则`。

## 1. OSS 凭证管理

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 查询 | `GET /api/clientVersion/ossConfig` | Query: `provider`（可选，默认 `aliyun`） | `ClientOssConfigOutput` |
| 保存 | `POST /api/clientVersion/saveOssConfig` | Body: `ClientOssConfigInput` | `ClientOssConfigOutput` |

```jsonc
// ClientOssConfigInput
{
  "provider": "aliyun",                 // string，必填，当前仅支持 aliyun
  "accessKeyId": "LTAIxxx",             // string，必填
  "accessKeySecret": "xxxxxxxx",        // string，可为空（空则沿用旧值）
  "endpoint": "oss-cn-shanghai.aliyuncs.com", // string，必填，需带协议或在前端拼 https://
  "bucketName": "client-resource-prod", // string，必填
  "rootPath": "/",                      // string，可选，默认 "/"
  "resourceDomain": "https://cdn.xxx.com", // string，可选
  "region": "cn-shanghai",              // string，可选
  "isDefault": true,                    // bool，可选，默认 true
  "remark": "生产 OSS"                   // string，可选
}

// ClientOssConfigOutput（敏感字段仅返回脱敏值）
{
  "id": 1,
  "provider": "aliyun",
  "accessKeyId": "LTAIxxx",
  "accessKeySecretMasked": "******3F8B",
  "endpoint": "oss-cn-shanghai.aliyuncs.com",
  "bucketName": "client-resource-prod",
  "rootPath": "/",
  "resourceDomain": "https://cdn.xxx.com",
  "region": "cn-shanghai",
  "isDefault": true,
  "remark": "生产 OSS",
  "createTime": "2024-11-11T12:00:00Z",
  "updateTime": "2024-11-11T12:30:00Z",
  "updatedBy": "admin"
}
```

## 2. 测试白名单

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 分页列表 | `GET /api/clientVersion/whitelistPage` | Query: `page`, `pageSize`, `keyword`（模糊匹配机器码/备注/标签），`onlyEnabled` | `SqlSugarPagedList<ClientWhitelistOutput>` |
| 列表（下拉） | `GET /api/clientVersion/whitelistList` | Query: `entryIds`（long[]，可选），`onlyEnabled` | `ClientWhitelistOutput[]` |
| 新增/编辑 | `POST /api/clientVersion/saveWhitelist` | Body: `ClientWhitelistUpsertInput` | `ClientWhitelistOutput` |
| 删除 | `POST /api/clientVersion/deleteWhitelist` | Body: `{ "entryId": number }` | `void` |

```jsonc
// ClientWhitelistUpsertInput
{
  "entryId": null,                     // long，可选；为空表示新增
  "machineCode": "ABCD-1234-XXXX-9999",// string，必填，2~128 位，大写字母/数字/-
  "remark": "安卓渠道测试",               // string，可选，<=512
  "tags": ["android", "qa"],           // string[]，可选
  "enabled": true                      // bool，默认 true
}

// ClientWhitelistOutput
{
  "entryId": 1001,
  "machineCode": "ABCD-1234-XXXX-9999",
  "remark": "安卓渠道测试",
  "tags": ["android", "qa"],
  "enabled": true,
  "createdBy": "qa_admin",
  "createTime": "2024-11-11T12:00:00Z",
  "updateTime": "2024-11-11T12:30:00Z"
}
```

校验提示：`machineCode` 不允许重复；如后端抛业务异常需在前端提示“机器码已存在”。

## 3. 渠道版本规则

### 3.1 规则 CRUD（同时维护正式版与白名单版）

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 列表 | `GET /api/clientVersion/ruleList` | Query: `channelId`（可选），`platform`（可选） | `ClientVersionRuleOutput[]` |
| 详情 | `GET /api/clientVersion/ruleDetail` | Query: `ruleId` | `ClientVersionRuleOutput` |
| 新增/更新 | `POST /api/clientVersion/saveRule` | Body: `ClientVersionRuleSaveInput` | `ClientVersionRuleOutput` |
| 删除 | `POST /api/clientVersion/deleteRule` | Body: `{ "ruleId": number }` | `void` |
| 白名单发布为正式 | `POST /api/clientVersion/promoteWhitelist` | Body: `PromoteWhitelistVersionInput` | `ClientVersionRuleOutput` |

```jsonc
// ClientVersionRuleSaveInput（generalVersionInfo 必填，whitelistVersionInfo 可选）
{
  "ruleId": null,                    // long，可选；为空则按 channel+platform 新建/覆盖
  "channelId": "oppo",               // string，必填
  "channelName": "OPPO 渠道",         // string，必填
  "platform": "Android",             // enum: Android | Ios | Mini
  "bucketName": "client-resource-prod", // string，必填
  "rootPath": "/",                   // string，可选，默认 "/"
  "resourceDomain": "https://cdn.xxx.com", // string，可选
  "generalVersionInfo": {            // 正式版，必填
    "appVersion": "1.0.2",
    "resVersion": "versions_17165123456.json",
    "resVersionTimestamp": 17165123456,
    "resVersionFileSize": 24567,
    "updateUrl": "https://cdn.xxx.com/apk/oppo_1.0.2.apk",
    "updateNotice": "修复若干已知问题",
    "loginUrl": "https://login.xxx.com",
    "description": "OPPO 正式包",
    "forceUpdate": false
  },
  "whitelistVersionInfo": {          // 白名单版，可选
    "appVersion": "1.0.3-rc1",
    "resVersion": "versions_17165199999.json",
    "resVersionTimestamp": 17165199999,
    "updateUrl": "https://cdn.xxx.com/apk/oppo_1.0.3-rc1.apk",
    "updateNotice": "白名单灰度",
    "forceUpdate": false
  },
  "whitelistTesterEntryIds": [1001, 1002] // long[]，关联的白名单 EntryId，可选
}

// ClientVersionRuleOutput
{
  "ruleId": 10,
  "channelId": "oppo",
  "channelName": "OPPO 渠道",
  "platform": "Android",
  "bucketName": "client-resource-prod",
  "rootPath": "/",
  "resourceDomain": "https://cdn.xxx.com",
  "generalVersionInfo": { ... },
  "whitelistVersionInfo": { ... },
  "whitelistTesterEntryIds": [1001],
  "whitelistMachineCodes": ["ABCD-1234-XXXX-9999"],
  "latestPublishResVersion": "versions_17165123456.json",
  "latestPublishTime": "2024-11-11T13:00:00Z",
  "latestPublishUserName": "ops_admin",
  "createTime": "2024-11-10T08:00:00Z",
  "updateTime": "2024-11-11T13:00:00Z"
}
```

规则约束：同一 `channelId + platform` 只有一条规则，`SaveRule` 无 `ruleType`，一次性维护正式版与白名单版；删除前若要保留数据，请先导出。白名单发布时要求 `confirm=true`。

### 3.2 OSS 版本列表

| 功能 | Method & Path | 请求参数 | 响应 data |
| --- | --- | --- | --- |
| 列出 AppVersion | `GET /api/clientVersion/appVersions` | Query: `channelId`, `platform`, `bucketName`（可选），`rootPath`（可选） | `ChannelAppVersionItem[]` |
| 列出资源版本 | `GET /api/clientVersion/resVersions` | Query: `channelId`, `platform`, `appVersion`, `bucketName`（可选），`rootPath`（可选） | `ChannelResVersionItem[]` |

```jsonc
// ChannelAppVersionItem
{
  "appVersion": "1.0.2",
  "folder": "Android/1.0.2",
  "lastModified": null              // OSS 未返回则为空
}

// ChannelResVersionItem
{
  "fileName": "versions_17165123456.json",
  "timestamp": 17165123456,
  "fileSize": 24567,
  "lastModified": "2024-11-11T11:00:00Z",
  "downloadUrl": "https://cdn.xxx.com/Android/1.0.2/versions_17165123456.json",
  "isNewerThanOnline": true        // 后端根据当前 general 版本计算
}
```

选择规则：`appVersions` 与 `resVersions` 需从接口下拉选择，不允许手输；正式版仅可选择 `isNewerThanOnline = true` 的资源版本，白名单不限制。

### 3.3 发布流程
1. 前端点击“白名单发布为正式”前先调用 `GET /api/clientVersion/ruleDetail` 刷新数据并展示对比（旧正式 vs 白名单）。
2. 用户确认后调用 `POST /api/clientVersion/promoteWhitelist`，Body：
```json
{
  "ruleId": 10,
  "targetResVersion": "versions_17165123456.json", // 可选，默认取当前 whitelist resVersion
  "confirm": true
}
```
3. 后端校验资源版本是否比当前正式版新、白名单信息是否存在，失败会返回业务码；成功后返回最新规则并同步 CenterServer。
