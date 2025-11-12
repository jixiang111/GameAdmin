# 游戏服地址管理 API（后台前端对接）

## 基础说明
- 当前模块基于 DynamicApi，默认路由格式 `/api/{service}/{action}`，因此接口前缀为 `/api/gameServerAddress`（service 名转换为小驼峰）。
- 所有响应均为 `AdminResult`，前端只需关注 `data` 字段；错误可读取 `code` / `message`。
- GM RPC 的 Token 由后端统一注入，无需前端干预，但必须确保 `Admin.NET.Application/Configuration/App.json` 中的 `GmRpc.CenterServer` 已配置正确。

## 接口列表

| 功能 | Method & Path | 请求参数 | 返回数据 |
| --- | --- | --- | --- |
| 列表 | `GET /api/gameServerAddress/list` | 无 | `GameServerAddressOutput[]` |
| 详情 | `GET /api/gameServerAddress/detail` | Query: `serverId` | `GameServerAddressOutput` |
| 新增 | `POST /api/gameServerAddress/add` | Body: `GameServerAddressCreateInput` | `GameServerAddressOutput` |
| 更新 | `POST /api/gameServerAddress/update` | Body: `GameServerAddressUpdateInput` | `GameServerAddressOutput` |
| 删除 | `POST /api/gameServerAddress/delete` | Body: `{ "serverId": number }` | `void` |

## 数据结构

```jsonc
// GameServerAddressCreateInput / UpdateInput
{
  "serverId": 1001,
  "serverName": "一区",
  "ip": "10.0.0.11",
  "port": 8700,
  "wsPort": 8800,
  "grpcPort": 30001,
  "wsUrl": "ws://10.0.0.11:8800",          // 可选，缺省时后端自动拼 ws://{ip}:{wsPort}
  "grpcUrl": "http://10.0.0.11:30001",    // 可选，默认按 h2c 拼接
  "remark": "双线一区",
  "enabled": true
}

// GameServerAddressOutput（列表/详情/返回值）
{
  "serverId": 1001,
  "serverName": "一区",
  "ip": "10.0.0.11",
  "port": 8700,
  "wsPort": 8800,
  "wsUrl": "ws://10.0.0.11:8800",
  "grpcPort": 30001,
  "grpcUrl": "http://10.0.0.11:30001",
  "remark": "双线一区",
  "enabled": true
}
```

## 示例

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
  "grpcPort": 30001,
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
1. `App.json -> GmRpc.CenterServer` 中 `Address` 需指向真实地址（示例：`http://127.0.0.1:30001`），并设置 `UseTls=false` 以启用 h2c。
2. 游戏中心服需将后台服务器 IP 加入 `GmRpcIpWhitelist`。
3. 若需区分环境，可在前端利用 `enabled` 或在后台扩展字段。***
