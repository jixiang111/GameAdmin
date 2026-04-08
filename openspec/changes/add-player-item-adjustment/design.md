# 设计：新增玩家道具调整功能

## 概述

当前系统已经具备：

- `GameServerAddressService` 以及对应的前端服务器选择页面
- `GameServerItemAuditService` 以及对应的前端道具流水页面
- `ICenterGmService.QueryItemChangeLogs` 与 `CenterServerRpcClient.QueryItemChangeLogsAsync`

本次变更是在现有“只读查询链路”之外，补齐“GM 调整道具写入链路”，同时继续以现有道具流水页面作为核验入口。

## 架构设计

### 后端

在现有 GM RPC 集成基础上补充道具调整能力：

1. 在 `Admin.NET.Application/GmRpc/CenterServerContracts.cs` 中扩展 `ICenterGmService`
2. 在 `Admin.NET.Application/GmRpc/CenterServerRpcClient.cs` 中扩展 `ICenterServerRpcClient` 与 `CenterServerRpcClient`
3. 在 `Admin.NET.Application/Service/GameServer` 下新增动态 API 服务
4. 新增请求校验和返回映射 DTO

### 前端

在现有 `itemAudit.vue` 旁边新增一个独立操作页面：

1. 在 `Web/src/api/gameServerItemAdjust` 下新增 API 模块
2. 在 `Web/src/views/workbench/server/itemAdjust.vue` 下新增页面
3. 在 `Web/src/router/route.ts` 中注册路由和菜单
4. 在 `Web/src/router/backEnd.ts` 中加入基础菜单集合
5. 在调整成功后支持跳转到现有审计页面

## RPC 合同变更

### 新增请求与响应 DTO

在 `CenterServerContracts.cs` 中新增以下 MessagePack DTO：

- `CenterGmAdjustPlayerItemReq`
- `CenterGmAdjustPlayerItemRes`
- `GmAdjustPlayerItemReq`
- `GmAdjustPlayerItemRes`

请求字段遵循 `GmItemAuditApi.md`：

- `ServerId`
- `Request.RoleId`
- `Request.ItemId`
- `Request.Delta`
- `Request.ReasonCode`
- `Request.Remark`
- `Request.OperatorId`
- `Request.OperatorName`
- `Request.TraceId`

响应字段包括：

- `Success`
- `Message`
- `ServerId`
- `Data.BeforeNum`
- `Data.AfterNum`
- `Data.AppliedDelta`
- `Data.ChangeTimeTicks`
- `Data.RoleId`
- `Data.ItemId`

### 新增 RPC Client 方法

在 `ICenterServerRpcClient` 中新增 `AdjustPlayerItemAsync`，并在 `CenterServerRpcClient` 中实现。

行为约束：

- 拒绝 `null` 请求或 `null` 的嵌套请求体
- 调用 `client.AdjustPlayerItem(request)`
- 复用 `EnsureRpcSuccess(...)`
- 当服务端未返回 `Data` 时，返回一个安全的默认对象，并尽量保留 `ServerId`、`RoleId`、`ItemId`、`Delta` 等上下文信息

## 后端动态 API 设计

### 新增服务

在 `Admin.NET.Application/Service/GameServer` 下新增服务，建议命名为 `GameServerItemAdjustService`。

根据现有动态 API 命名规则，建议接口路由为：

- `POST /api/gameServerItemAdjust/submit`

与现有服务风格保持一致：

- 服务名对应路由前缀
- 方法名对应 API 后缀

### 依赖注入

注入以下依赖：

- `ICenterServerRpcClient`
- `UserManager`

`UserManager` 已经是项目里记录操作人信息的现有模式，例如 `ClientVersionService` 中已采用该方式，因此这里也应作为 `OperatorId` 和 `OperatorName` 的来源。

### 输入 DTO

新增 `GameServerItemAdjustInput`，校验规则如下：

- `ServerId > 0`
- `RoleId > 0`
- `ItemId > 0`
- `Delta != 0`
- `ReasonCode` 必填，需 `Trim`，最大长度 128
- `Remark` 可选，最大长度按现有风格取 256 或 512
- `TraceId` 可选，最大长度 128

### 输出 DTO

新增 `GameServerItemAdjustOutput`，输出字段包括：

- `ServerId`
- `RoleId`
- `ItemId`
- `AppliedDelta`
- `BeforeNum`
- `AfterNum`
- `ChangeTimeTicks`
- `TraceId`
- `ReasonCode`
- `Remark`
- `OperatorId`
- `OperatorName`

### 提交流程

`Submit(...)` 应按以下顺序执行：

1. 清理并标准化输入文本
2. 校验 `Delta != 0`
3. 从 `UserManager` 获取操作人信息
4. 如果 `TraceId` 为空，则自动生成可读的值，例如 `gm-item-{timestamp}-{guid-segment}`
5. 调用 `_centerServerRpcClient.AdjustPlayerItemAsync(...)`
6. 将 RPC 返回结果映射为前端输出模型

### 错误处理

延续现有 GM RPC 错误处理方式：

- 当 `Success=false` 时，优先直接透出服务端 `Message`
- 不对业务校验失败做自动重试
- 对本地输入错误在发起 RPC 前直接返回 Furion 校验错误

## 前端设计

### 新增 API 模块

新增 `Web/src/api/gameServerItemAdjust/index.ts`。

内容包括：

- 枚举 `GameServerItemAdjustApi`
- 请求类型 `GameServerItemAdjustInput`
- 返回类型 `GameServerItemAdjustOutput`
- 提交函数 `submitGameServerItemAdjust(data)`

实现方式沿用现有 game server 相关模块的 `request` 封装模式。

### 新增页面

新增 `Web/src/views/workbench/server/itemAdjust.vue`。

核心界面元素：

- 复用 `getGameServerAddressList` 的服务器选择器
- `RoleId`、`ItemId`、`Delta` 输入框
- `ReasonCode`、`Remark`、`TraceId` 输入框
- 只读展示当前登录操作人信息，数据来源 `useUserInfo().userInfos`
- 提交按钮，并在提交前进行确认
- 成功结果卡片，展示调整前后数量、实际调整值和变更时间
- 跳转到 `/dashboard/server-item-audit` 的按钮，并带上预填查询参数

### 交互规则

- 提交前必须填写服务器、玩家、道具、调整值和原因码
- `Delta` 必须清晰表达正负，避免误操作
- 发起请求前弹出确认框
- 成功后如果用户继续修改表单，应清空上次成功结果
- 错误提示优先显示后端返回的明确文本

### 调整结果跳转审计

增强 `itemAudit.vue` 的路由初始化逻辑，使其额外支持读取：

- `traceId`
- 可选的 `reasonCode`

调整页在跳转时至少带上：

- `serverId`
- `roleId`
- `itemId`
- `traceId`

这样可以缩短核验路径，避免再次手动输入。

## 路由与权限

新增一个路由和菜单项：

- 路由名：`gameServerItemAdjust`
- 路径：`/dashboard/server-item-adjust`
- 组件：`/workbench/server/itemAdjust`

权限标识延续现有菜单风格，建议使用：

- `gameServerItemAdjust:submit`

然后将该菜单加入 `BASE_WORKBENCH_MENUS`，使其与服务器管理、道具流水页面一样始终出现在工作台菜单中。

## 风险与决策

### 操作人字段的信任边界

`OperatorId` 和 `OperatorName` 属于审计关键字段，不应信任浏览器提交内容。后端必须从当前登录后台会话中获取这两个字段，前端只能展示，不应拥有最终决定权。

### 可追踪性

虽然服务端文档里 `TraceId` 是可选字段，但它对事后核验非常重要。后端在缺失时自动生成，可以兼顾易用性和追踪能力，同时也保留运营人员手动传入自定义 `TraceId` 的能力。

### 最小改动范围

现有道具流水页已经足够承担核验职责，因此本次只补充“调整成功后跳转并预填查询条件”的能力，不对整个审计页做重构。
