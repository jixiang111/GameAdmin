# Admin.NET Windows 部署文档（无 IIS，前后端一起）

本文档针对不使用 IIS 的场景。目录结构：
- 后端：`F:\WorkSpace\GameAdmin\Admin.NET`
- 前端：`F:\WorkSpace\GameAdmin\Web`

## 1. 环境要求

- 构建机：.NET SDK 9、Node.js >= 18、pnpm
- 运行机：.NET 9 Runtime（若发布使用 `--self-contained true` 可不装）
- 前端静态服务：任选其一（Caddy/Nginx/Node http-server）

## 2. 后端发布

在 `F:\WorkSpace\GameAdmin\Admin.NET` 执行：
```powershell
dotnet publish Admin.NET.Web.Entry\Admin.NET.Web.Entry.csproj -c Release -f net9.0 -r win-x64 --self-contained false -o D:\Deploy\Admin.NET
```
说明：
- 如果运行机没有 .NET 9 Runtime，把 `--self-contained false` 改成 `true`。
- 发布包会包含 `Configuration\*.json`。

## 3. 后端配置

配置文件在发布目录的 `Configuration\*.json`：
- `Configuration\Database.json`：数据库连接与初始化配置。
- `Configuration\App.json`：端口、跨域、虚拟目录、Swagger 等。
- 其他业务配置：`JWT.json`、`Upload.json`、`Cache.json`、`EventBus.json` 等。

关键点：
- `Configuration\App.json` 的 `Urls` 默认是 `http://*:5005`。
- 跨域在 `Configuration\App.json` 的 `CorsAccessorSettings`。
- 二级目录部署需设置 `Configuration\App.json` 的 `AppSettings.VirtualPath`。

注意：发布包不会包含以下目录（见 `Admin.NET.Web.Entry.csproj`）：
- `wwwroot\upload`
- `wwwroot\avatar`
如果已有上传文件，更新时务必保留。

## 4. 前端构建与配置

在 `F:\WorkSpace\GameAdmin\Web` 执行：
```powershell
pnpm install
pnpm run build
```
默认输出目录为 `dist`（见 `vite.config.ts`）。

前端运行时读取 `dist\config.js`（构建时由 `.env` 与 `.env.production` 生成）。
常用配置：
- `VITE_API_URL`：后端地址。
- `VITE_PUBLIC_PATH`：前端部署基础路径（默认空）。

配置方式：
- 构建前修改 `.env.production`。
- 构建后可直接修改 `dist\config.js`。

## 5. 无 IIS 部署方案

### 5.1 方案 A：后端托管前端（同端口）

1) 前端构建生成 `dist`。  
2) 将 `Web\dist` 内容拷贝到后端发布目录的 `wwwroot` 下（保留 `wwwroot\upload` 和 `wwwroot\avatar`）。  
3) 修改 `dist\config.js`：`VITE_API_URL = ""`（同源相对路径）。  
4) 启动后端服务（见第 6 节）。  

说明：如果前端路由刷新 404，建议改用方案 B 的静态服务器并启用 SPA fallback。

### 5.2 方案 B：独立静态服务器

**同域反代（推荐）**：静态服务器负责前端，并把 `/api`、`/hubs`、`/upload` 反代到后端。  
Caddy 示例（前端目录 `D:\Deploy\Admin.NET.Web`）：
```text
:8080 {
  root * D:/Deploy/Admin.NET.Web
  try_files {path} /index.html
  file_server
  @api path /api* /hubs* /upload*
  reverse_proxy @api 127.0.0.1:5005
}
```

**分域直连**：前端独立端口/域名，设置 `dist\config.js`：
- `VITE_API_URL = http://server:5005`
并在后端 `Configuration\App.json` 的 `CorsAccessorSettings` 中配置允许域名。

## 6. 运行与服务化

### 6.1 后端（NSSM 服务）
```powershell
nssm install AdminNET "C:\Program Files\dotnet\dotnet.exe" "D:\Deploy\Admin.NET\Admin.NET.Web.Entry.dll"
nssm set AdminNET AppDirectory "D:\Deploy\Admin.NET"
nssm set AdminNET AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production" "ASPNETCORE_URLS=http://0.0.0.0:5005"
nssm start AdminNET
```

### 6.2 前端静态服务（示例）
- Caddy 启动：`caddy run --config Caddyfile`
- 也可用 NSSM 把 Caddy 注册为服务（与后端方式一致）。

## 7. 升级与回滚

1) 备份：`Configuration\*.json`、`wwwroot\upload`、`wwwroot\avatar`。  
2) 停止服务。  
3) 覆盖发布包（保留上述目录）。  
4) 启动服务。  
5) 异常时恢复备份回滚。  

## 8. 验证清单

- 前端页面可访问，路由刷新不 404。  
- 后端端口可访问（如 `http://server:5005`）。  
- 登录、菜单、上传、SignalR 正常。  
- 日志可在运行目录的 `logs` 查看。  

## 9. 常见问题

- **前端路由刷新 404**：未启用 SPA fallback。  
- **前后端跨域报错**：检查 `CorsAccessorSettings`。  
- **API 地址错误**：检查 `dist\config.js` 的 `VITE_API_URL`。  
- **上传目录丢失**：发布包不包含 `wwwroot\upload` 和 `wwwroot\avatar`，需要手动保留。  
