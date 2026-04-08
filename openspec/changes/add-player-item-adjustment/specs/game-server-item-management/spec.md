## 新增需求

### 需求：授权操作员可以通过 CenterServer 调整玩家道具

管理后台必须允许具备权限的操作员通过 CenterServer `AdjustPlayerItem` GM RPC，对指定服务器上的玩家道具数量进行调整。

#### 场景：提交正向或负向道具调整

- **假设** 操作员选择了合法的 `ServerId`
- **并且** 输入了合法的 `RoleId`、`ItemId` 和非零的 `Delta`
- **并且** 提供了非空的 `ReasonCode`
- **当** 操作员提交调整请求
- **那么** 后台会向 CenterServer 发送 `CenterGmAdjustPlayerItemReq`
- **并且** 请求中包含 `OperatorId`、`OperatorName` 和 `TraceId`
- **并且** 返回结果中包含该玩家该道具的实际生效调整信息

### 需求：GM 调整使用的操作人身份必须来自后台登录会话

管理后台必须从当前已认证的后台用户上下文中获取 GM 操作人身份，而不能信任浏览器提交的操作人信息。

#### 场景：浏览器尝试伪造操作人身份

- **假设** 浏览器发起了一次调整请求
- **当** 后端构造 GM RPC 请求体
- **那么** `OperatorId` 和 `OperatorName` 必须来自当前已认证后台会话
- **并且** 浏览器提交的操作人身份字段会被忽略或不被接受

### 需求：界面必须支持调整完成后的即时核验

管理后台界面必须展示服务端确认的调整结果，并提供直接进入道具流水核验页面的能力。

#### 场景：从调整结果跳转到审计页面

- **假设** 一次道具调整成功
- **当** 页面展示结果
- **那么** 页面会显示 `BeforeNum`、`AfterNum`、`AppliedDelta` 和 `ChangeTimeTicks`
- **并且** 提供一个可直接跳转到道具流水页的入口，并预填 `ServerId`、`RoleId`、`ItemId`、`TraceId` 等查询上下文
