# PROGRESS

## 当前状态

阶段 0「勘察」已完成（2026-11-20），**未修改任何代码**，只写文档。
基线：`upstream/dev` = `5a99917e`（2026-11-21 同步，见文末同步记录）；`main` = 上游 + 文档骨架 + 勘察文档。

## 阶段 0 清单

- [x] 核实许可证现状 → `docs/LICENSE-NOTES.md`
  - 根 `LICENSE` 是 Apache-2.0；`PCL.Launcher/`、`PCL.Launcher.Core/` 各有一份相同内容的 PCL 自定义 `LICENCE`（分发限原样副本、合理使用指南不是法律文件）。
  - `docs/upstream/` 为空、`Plain Craft Launcher 2/` 目录不存在（Avalonia 迁移后已改名），`CLAUDE.md` 里那条核实要求的前提不成立，已记录事实。
  - 名称 vs 指南"PCL 开头 + 第三方修改后缀"的条件不一致，已列为待决事项。
- [x] 读上游文档与源码，弄清仓库结构、MC 业务、PCL 控件/主题位置 → `docs/ARCHITECTURE.md`
- [x] Linux 构建并运行，记录问题和 Windows 专有代码 → `docs/DECISIONS.md`
  - 构建：0 错误 / 205 警告；交叉编译 win-x64 也通过（0 错误）。
  - 测试：487 个用例，480 通过、7 跳过、0 失败。
  - 运行：X11（强制）与原生 Wayland 都能创建窗口并进主界面；首次启动有协议/遥测弹窗。
- [x] 写出构建 / 测试 / 运行 / 发布命令 → `docs/DEV.md`
- [x] 验证 upstream 同步流程：fetch、mirror 对比、临时分支 merge 演练均通过；因上游无新提交，冲突解决尚未实测。
- [x] 向用户汇报（本次对话）。

## 运行期发现（后续阶段要处理）

- 启动会访问上游更新/公告/下载服务器（MirrorChyan、`staticassets.naids.com`、`s3.pysio.online`、GitHub raw 等）。
- 启动会扫 `~/.minecraft`、扫系统 Java，并往 `<实例>/PCL/config.v1.yml` 写配置（本次沙箱只读，写入失败但被容错）。
- 遥测：Sentry；本地无 DSN，实际未上报。`BuildProvenance` 在 release 分支会请求上游 GitHub Release 哈希。
- 已知无关紧要的启动警告：`Dependency injection failed: rpc-property@128`；隔离配置目录会触发"用户密钥已重建"。
- 应用内更新在 Linux 上不可用（`ManualUpdatePolicy`，只给下载链接），属上游设计。

## 未完成 / 未验证

- 上游同步的**冲突解决**流程：等上游 `dev` 有新提交后实测。
- Windows 实机运行（只在 Linux 上交叉编译过 win-x64）。
- macOS 构建/运行。
- 关于页/EULA/遥测/更新这些上游身份相关内容仍原样保留（阶段 2 处理）。
- `PCL.Localization` 项目当前没有被任何项目引用（语言文件由 `PCL.Core.UI` 直接嵌入），是否为上游遗留待确认。

## 待用户决定

- [ ] 我们新增代码（`Flasher.Core`）的许可证。
- [ ] 是否向 PCL 作者确认授权。
- [ ] 产品显示名是否要满足"PCL 开头 + 第三方修改后缀"；仓库名是否保持 `Xperia-fukuro`。
- [ ] 摘除 Minecraft 的顺序/方式：先砍入口（页面模块）还是先排除编译；是否保留 macOS。
- [ ] 是否修订 `CLAUDE.md` 中两处与现状不符的表述（".NET 10" 的 SDK/运行时区分；`Plain Craft Launcher 2/` 目录）。
- [ ] 本机工具目录 `.dotnet/`、`.dotnet-home/`、`.local-nuget/`（约 2 GB）和 `.run/` 证据是否保留。

## 下一步

等用户确认后进入阶段 2「摘除 Minecraft」。建议第一批先做**低风险、可独立验证**的两件事：

1. 启动期上游行为：`StartupEffects` 的协议/遥测弹窗替换、`UpdateManager`/公告/`BuildProvenance` 停用。
2. 页面入口：从 `PageCatalog` 移除 Download/Instance/Launch 模块，保留设置/工具骨架，确认应用仍能启动。

之后再做 `Flasher.Core`（`IFastbootBackend` + mock + 单元测试）。

## 同步记录

| 日期 | upstream/dev | 操作 | 结果 |
| --- | --- | --- | --- |
| 2026-11-20 | `b51179f9` | `git fetch upstream --prune`；`main` 上开 `sync/2026-11-20` 合并 `upstream-mirror` | 上游无新提交，merge 为 "已经是最新的"；分支已删除；`rerere.enabled=true` |
| 2026-11-21 | `5a99917e` | `git fetch upstream --prune`；`upstream-mirror` 快进；`main` 上开 `sync/2026-11-21` 合并 `upstream-mirror` | 无冲突，3 个文件（BOM 修复 + 字体选项修复）；构建 0 错误 / 206 警告；测试 490 个用例（483 通过、7 跳过、0 失败）；已合入 `main` |
