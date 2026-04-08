# 任务：新增玩家道具调整功能

- [x] 在 `Admin.NET.Application/GmRpc/CenterServerContracts.cs` 中补充 `AdjustPlayerItem` RPC 定义及 `GmItemAuditApi.md` 对应 DTO
- [x] 在 `Admin.NET.Application/GmRpc/CenterServerRpcClient.cs` 中补充 `AdjustPlayerItemAsync(...)`，并保持与现有查询逻辑一致的请求与错误处理
- [x] 在 `Admin.NET.Application/Service/GameServer/Dto` 下新增玩家道具调整请求和响应 DTO
- [x] 新增 `GameServerItemAdjustService`，提供 `POST /api/gameServerItemAdjust/submit`，包含输入校验、`UserManager` 操作人信息注入和缺省 `TraceId` 生成
- [x] 新增 `Web/src/api/gameServerItemAdjust/index.ts`，补充请求类型、返回类型和提交 API 封装
- [x] 新增 `Web/src/views/workbench/server/itemAdjust.vue`，实现调整表单、确认提交、结果卡片和跳转审计页功能
- [x] 在 `Web/src/router/route.ts` 和 `Web/src/router/backEnd.ts` 中注册新路由与基础菜单项
- [x] 增强 `Web/src/views/workbench/server/itemAudit.vue` 的路由预填逻辑，使其支持从调整结果携带 `traceId` 自动进入核验上下文
- [ ] 通过后端构建和前端功能验证完整链路，覆盖成功、输入校验失败以及 RPC 报错展示场景




> 验证阻塞：前端 npm run build 已通过；后端 dotnet build Admin.NET.Web.Entry/Admin.NET.Web.Entry.csproj -v minimal 因本机缺少 .NET SDK 9.0.0（当前仅 8.0.121）未能完成。
