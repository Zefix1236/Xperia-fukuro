# 下载：Sony 固件下载（2026-11-23）

## 这个对话负责什么

新机型（Xperia 1/5/10 III–VIII）的 Sony 固件查询与下载；不负责刷写。

## 已完成

- 确认协议：`match/v2` → `software-service/gen2` → `/api/file/{contentId}` → `/filestorage/..._partN`。
- 协议来源是对 XperiFirm 5.8.1 的逆向分析（运行日志、mitmproxy 抓包、9 份 XML 响应）；此前写成“不需要反编译”是错误结论，现已删除：
  - 设备 ID 来自公开的 `https://app.swup.update.sony.net/ess-distribution/public/api/product/v2`；
  - SPC 来自 XDA 社区表（post-90753534）；
  - match 不带 SPC 会返回 `SPC_UNKNOWN`，带 SPC 正常。
- 生成 `Flasher.Core/Catalog/Data/devices.sony.json`：15 个新机型产品、93 个变体、89 个有 SPC；缺 10 VIII 的 XQ-GH44/SO-52G/SOG18/A602SO。
- 新建 `Flasher.Core`：目录、match/service/manifest 解析、Sony 客户端、分块下载、ETag=MD5 校验、Range 续传、合并、磁盘预检。
- 新建 `Flasher.Core.Test`：16 个用例（目录、解析、客户端、分块、合并、下载管理）。
- 下载页 UI：下载页左栏新增「固件」分组 →「固件下载」；机型/型号选择、查询版本、文件勾选、保存目录、进度、取消。
- 已验证：match 8 台设备全部 200；Gen2 URL ≥21 小时有效；42 个 chunk 全部 206；完整下载 1 个 100 MiB 块且 MD5=ETag；`Flasher.Core` 真实下载 CUSTOMER_ID（462 B）与 E_LABEL（1,877,177 B）成功。

## 未完成 / 未验证

- 下载按钮的 UI 端到端只验证到文件列表；未在 UI 里实际点「开始下载」。core 已用 fake server 与真实 service 验证，UI 只是调用同一组 `SonyApiClient` / `DownloadManager`。
- 完整 4.4 GB APP_SW 下载 + 42 块合并未跑过（磁盘当前只剩 13 G，峰值约 8.8 G）。
- ZIP64 解包（`super` 5,047,777,792 B）未实测；首版只下载 ZIP。
- 10 VIII 的 4 个变体缺 SPC；老机型（III 之前）没有 SPC 表。
- 文件夹选择器未做；保存目录可编辑，默认 `~/XperiaFirmware/<PDX>/<版本>`，可用 `XPERIA_FIRMWARE_DIR` 覆盖。
- 未加独立 CI 工作流；未更新 `docs/DEVICES.md`（其中 1 VII 的 PDX 代号 255 应为 256）。

## 下一步

1. UI 里只勾 CUSTOMER_ID/E_LABEL，点「开始下载」验证小文件端到端。
2. 换到有足够空间的盘，跑一次完整 APP_SW（约 20 分钟 @3.8 MiB/s）。
3. 决定是否首版解包；若解包，先验证 ZIP64 与磁盘峰值。
4. 补 10 VIII 缺失 SPC，或让用户手动输入 SPC。
5. 把 `Flasher.Core.Test` 加进 GitHub Actions（新工作流，不改上游）。
