# 客户端版本规则前端改动备忘

> 文件位置：`Web/src/views/workbench/clientVersion/channel.vue`  
> 目标：编辑版本规则时，资源版本下拉显示创建时间，并保留已保存的白名单资源版本（不自动跳到最新）。

## 1. 下拉项显示创建时间

- 在白名单、正式版的资源版本 `<el-option>` 内容中添加二行布局：
  - 第一行：文件名 `item.fileName`
  - 第二行：`创建时间：{{ formatResVersionTime(item) || '未知' }}`
  - 保留 “新版本” 标签。

- 新增辅助方法：
  ```ts
  const formatResVersionTime = (item: ChannelResVersionItem): string => {
  	if (item.lastModified) try parse为 Date;
  	if (item.timestamp) 10位当秒，13位当毫秒；
  	若无则从文件名 `versions_YYYYMMDDHHMMSS.json` 提取，按 UTC 构造 Date；
  	最终调用 formatDateTime 输出。
  };

  const formatDateTime = (date: Date): string => {
  	const pad = (n: number) => n.toString().padStart(2, '0');
  	return `${date.getFullYear()}年${pad(date.getMonth() + 1)}月${pad(date.getDate())}日${pad(date.getHours())}时${pad(
  		date.getMinutes()
  	)}分${pad(date.getSeconds())}秒`;
  };
  ```

## 2. 保留白名单资源版本选择

- 调整 `onWhitelistAppVersionChange(preserveSelection = false)`：
  - `preserveSelection=true` 时保留已保存的 `whitelistResVersion`，若列表里没有则插入占位项再设回原值；
  - 仅在 `!preserveSelection` 且未选择时，默认取最新（列表第一个）。

- 在 `openDrawer` 编辑模式加载资源版本时调用 `onWhitelistAppVersionChange(true)`，避免覆盖已保存值。

## 3. 注意事项

- 文件保持 UTF-8 编码，中文直写（勿再出现乱码）。
- 改动尽量局部，不要覆盖其他逻辑（尤其是正式版资源选择保持原流程）。
- 如需重做，可先 `git checkout -- Web/src/views/workbench/clientVersion/channel.vue` 还原，再按本备忘逐步修改。
