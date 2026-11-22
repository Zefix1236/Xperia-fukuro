# FORK-PATCHES

记录对上游文件的改动：文件、改动内容、原因、对应的 `// FORK:` 标记。

## 阶段 0（勘察）

暂无。阶段 0 只新增/修改 `docs/` 下的文档，没有改动上游代码或资源。

（后续每个阶段按提交记录追加，格式：）

| 文件 | 改动 | 原因 | 标记位置 |
| --- | --- | --- | --- |
| 例：`PCL.Launcher/Program.cs` | 去掉启动期更新检查 | 不连上游服务器 | `// FORK: 不保留上游自动更新` |

## 仓库配置

| 文件 | 改动 | 原因 | 标记位置 |
| --- | --- | --- | --- |
| `.gitignore` | 增加 `/EXAMPLES/` | 本地示例/参考资料目录不入库，也不随仓库分发 | 文件末尾注释 |

## 登录页对齐 PCL 原版

| 文件 | 改动 | 原因 | 标记位置 |
| --- | --- | --- | --- |
| `PCL.Launcher/Views/Pages/Launch/PageLaunchLeft.axaml` | 恢复顶部「正版 / 离线」单选、账户类型徽标；底部按钮文案改为「版本选择 / 版本设置」 | 对齐原版 PCL 的登录方式与底部操作 | 文件头 `<!-- FORK: -->` |
| `PCL.Launcher/Views/Pages/Launch/PageLaunchLeft.axaml.cs` | 新增登录方式单选同步、登录子页切换；返回时重置登录方式 | 原版用登录方式而不是档案列表决定表单 | 文件头 `// FORK:` |
| `PCL.Launcher/ViewModels/Launch/PageLaunchLeftViewModel.cs` | 登录方式状态、账户徽标文案、登录子页判定（`Profile` → `ProfileSkin` 改名） | 原版用登录方式而不是档案列表决定表单 | 文件头 `// FORK:` |
| `PCL.Launcher/Views/Pages/Launch/PageLoginMs.axaml(.cs)` | 按原版布局重写：人像 → 账号下拉 + 60×28 登录按钮 → 外链；去掉返回按钮 | 原版有账号下拉、无返回按钮 | 文件头 |
| `PCL.Launcher/Views/Pages/Launch/PageLoginOffline.axaml(.cs)` | 按原版 PageLoginLegacy 重写：红提示 + 皮肤预览 + 可编辑用户名下拉；回车创建档案；默认显示 Steve 头像 | 原版离线页没有 UUID 选项与创建/返回按钮 | 文件头 |
| `PCL.Launcher/Views/Pages/Launch/PageLoginProfileSkin.axaml(.cs)`、`PageLoginProfileSkinViewModel.cs` | 去掉账号类型行、名字 16→17、皮肤行 80→75；tooltip/菜单文案对齐原版 | 账号类型改由顶部徽标展示 | 文件头 |
| `PCL.Launcher/Views/Pages/Launch/PageLoginProfile.axaml(.cs)`、`PageLoginProfileViewModel.cs` | 删除 | 原版没有档案列表与「新建档案」 | — |
| `PCL.Localization/Languages/*.axaml` | 新增/调整登录页文案（`Launch.Home.LoginType.*`、`AccountBadge.*`、`SelectVersion/VersionSettings`、`Account.Offline.NameHint/NameFieldHint`、皮肤卡菜单等） | 对齐原版文案 | `Launch.Home` 前 `<!-- FORK: -->` |

## 下载页进入动画

| 文件 | 改动 | 原因 | 标记位置 |
| --- | --- | --- | --- |
| `PCL.Launcher/Views/Pages/Download/PageDownloadInstall.axaml.cs` | 阶段同步提前到 `PageEnter`；导航期间不播放阶段动画 | 原版进入页面时先回到版本列表，列表才能参与连锁渐显 | 文件头 `// FORK:` |
| `PCL.Launcher/UI/Controls/Minecraft/MinecraftVersionCatalog.cs` | `Activate()` 在版本数据未变化时不再 `Rebuild()` | 重建会替换正在播放入场动画的条目 | 文件头 `// FORK:` |
| `PCL.Launcher/UI/Controls/Containers/MyPageRight.cs` | 入场动画枚举前统一复位后代控件透明度 | 同名动画替换后控件停在中间透明度 | 文件头 `// FORK:` |

已向上游提交 PR `PCL-Community/PCL-CE#3857`；上游合并后同步时按冲突处理，届时可撤掉这三处本地改动。

## 主题与动画时序

| 文件 | 改动 | 原因 | 标记位置 |
| --- | --- | --- | --- |
| `PCL.Launcher/UI/Theming/ThemeConstants.axaml` | 新增 `ColorBrushSidebar`（#F1FFFFFF） | 原版左栏是 94.5% 白，不是现在的 84% 透明 | 文件头 `<!-- FORK: -->` |
| `PCL.Launcher/Views/MainWindow.axaml` | 左栏底色改用该画刷；侧影改为 4px / 4% 黑 | 对齐原版 `ColorBrushBackgroundTransparentSidebar` 与 `RectLeftShadow` | 文件头 |
| `PCL.Launcher/Views/Shell/TitleBar.axaml` | 标题栏底色 `ColorBrush3` → `ColorBrush4` | 原版用 ColorObject4（#4890f5），本仓库 ColorBrush4 实测 #4390ee | 文件头 |
| `PCL.Launcher/Views/Shell/StartupEffects.cs` | 窗口开场动画与启动中切换动画补上原版的延迟（100ms / 50ms） | 对齐原版动画时序 | 文件头 `// FORK:` |

## 说明

- `MyCheckBox` 的 `UseLayoutRounding` 修复已由上游合并，不再作为本地改动。
- 早期开发用的 `DialogService` 弹窗日志已移除，不保留调试输出。
