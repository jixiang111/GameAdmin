# 后台接口文档：玩家道具变更审计

## 概览

建议后台优先调用 CenterServer 的 GM 接口，通过 `ServerId` 指定目标游戏服。

- Center 接口：`ICenterGmService`
- 游戏服接口：`IGmService`（Center 内部转发目标）

## 认证

调用 CenterServer GM RPC 时：

- Header：`x-gm-token: <token>`
- 调用方 IP 需在白名单中

CenterServer 转发到 GameServer 时会透传 `x-gm-token`。

## 时间字段约定

- 所有时间参数/返回时间统一使用 `UTC ticks`（`DateTime.UtcNow.Ticks`）

## 枚举

### ItemChangeSourceType

- `0` Unknown
- `1` Client
- `2` Gm
- `3` System

### ItemChangeOperationType

- `0` Unknown
- `1` Adjust
- `2` Use
- `3` Expire
- `4` Sell
- `5` GmAdjust

## CenterServer 接口（推荐）

### 1. 按服务器改道具

方法：`ICenterGmService.AdjustPlayerItem`

请求：`CenterGmAdjustPlayerItemReq`

```json
{
  "ServerId": 1001,
  "Request": {
    "RoleId": 100100000000000001,
    "ItemId": 20001,
    "Delta": 50,
    "ReasonCode": "gm_compensate",
    "Remark": "festival compensation",
    "OperatorId": 90001,
    "OperatorName": "gm_admin",
    "TraceId": "op-20260402-0001"
  }
}
```

返回：`CenterGmAdjustPlayerItemRes`

- `Success`
- `Message`
- `ServerId`
- `Data`（`GmAdjustPlayerItemRes`）

`Data` 关键字段：

- `BeforeNum`, `AfterNum`, `AppliedDelta`
- `ChangeTimeTicks`
- `RoleId`, `ItemId`

### 2. 按服务器查询道具变更日志

方法：`ICenterGmService.QueryItemChangeLogs`

请求：`CenterGmQueryItemChangeLogReq`

```json
{
  "ServerId": 1001,
  "Query": {
    "RoleId": 100100000000000001,
    "ItemId": 20001,
    "SourceType": 0,
    "OperationType": 0,
    "OperatorId": 0,
    "TraceId": "",
    "ReasonCode": "",
    "StartTimeTicksUtc": 638791776000000000,
    "EndTimeTicksUtc": 638792640000000000,
    "PageIndex": 1,
    "PageSize": 50
  }
}
```

返回：`CenterGmQueryItemChangeLogRes`

- `Success`
- `Message`
- `ServerId`
- `Data`（`GmQueryItemChangeLogRes`）

`Data` 关键字段：

- `Total`, `PageIndex`, `PageSize`, `HasMore`
- `ServerId`, `ServerName`
- `Items[]`（`GmItemChangeLogDto`）

`GmItemChangeLogDto` 字段：

- `LogId`
- `RoleId`, `ItemId`
- `BeforeNum`, `AfterNum`, `Delta`
- `SourceType`, `OperationType`
- `ReasonCode`, `Remark`
- `OperatorId`, `OperatorName`
- `TraceId`, `RequestUniId`
- `ChangeTimeTicks`

## GameServer 直连接口（可选）

当后台已明确目标服地址时可直连 `IGmService`：

- `AdjustPlayerItem(GmAdjustPlayerItemReq)`
- `QueryItemChangeLogs(GmQueryItemChangeLogReq)`

字段含义与 Center 转发中的 `Request`/`Data` 完全一致。

## 查询建议

- 玩家追溯：`RoleId + 时间范围`
- 道具追溯：`ItemId + 时间范围`
- GM 操作审计：`OperatorId + 时间范围`
- 单次链路追踪：`TraceId`

## 系统来源查询建议

当游戏逻辑通过系统入口发放或扣除道具时，建议后台重点使用以下条件组合：

- 活动奖励：`SourceType=System + ReasonCode=activity_reward`
- 装备升级消耗：`SourceType=System + ReasonCode=equip_upgrade_cost`
- 关卡掉落：`SourceType=System + ReasonCode=stage_drop`

示例查询：

```json
{
  "ServerId": 1001,
  "Query": {
    "RoleId": 100100000000000001,
    "SourceType": 3,
    "ReasonCode": "activity_reward",
    "StartTimeTicksUtc": 638791776000000000,
    "EndTimeTicksUtc": 638792640000000000,
    "PageIndex": 1,
    "PageSize": 50
  }
}
```

后台展示建议：

- `ReasonCode` 直接作为“系统来源编码”
- `Remark` 作为“业务上下文”
- `TraceId` 作为“链路号”

常见 `ReasonCode`：

- `activity_reward`
- `equip_upgrade_cost`
- `stage_drop`
- `task_reward`
- `mail_reward`

## 错误处理建议

- `Success=false` 时读取 `Message`。
- 后台重试建议只针对网络类错误，业务校验失败（如 `Delta=0`）不重试。
