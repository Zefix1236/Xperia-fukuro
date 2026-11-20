# LICENSE-NOTES

阶段 0 许可证勘察记录，整理日期 2026-11-20，基于提交 `ba68b02a`（= `upstream/dev` `b51179f9` + 文档提交）。
只陈述文件里写了什么、代码里怎么用，不替用户下法律结论。

## 1. 仓库里实际有哪些许可证文件

| 文件 | 内容 | 按 README 声明适用的范围 |
| --- | --- | --- |
| `LICENSE`（根目录） | Apache License 2.0 标准原文，208 行，无额外版权头 | 除 `PCL.Launcher/`、`PCL.Launcher.Core/` 外的所有目录 |
| `PCL.Launcher/LICENCE` | PCL 自定义文本，63 行 / 4756 字节 | `PCL.Launcher/` |
| `PCL.Launcher.Core/LICENCE` | 与上一份**逐字节相同**（`diff` 无输出） | `PCL.Launcher.Core/` |
| `PCL.Core/NOTICE` | 列出 5 个第三方组件（SharpZipLib、LiteDB、Roslyn、UWP Notifications、PolySharp） | 仅供 Apache-2.0 部分的 NOTICE 使用 |

- `README.md` 第 78–81 行明确写了这个"两个目录用自定义许可、其余 Apache-2.0"的划分。
- 两份 `LICENCE` 都来自上游提交（`git log`：`PCL.Launcher/LICENCE` ← `009af6f6`，`PCL.Launcher.Core/LICENCE` ← `74c4e2d7`），当前与 `upstream/dev` 无差异（`git diff upstream/dev -- ...` 为空）。
- 根目录 `LICENSE` 是标准 Apache-2.0 文本，不含"Copyright (C) xxx"这类可填写的版权行；Apache-2.0 本身的再分发/署名要求见该文本第 4 节。

## 2. `LICENCE` 文本写了什么（内容转述）

文件由三部分组成：《PCL 分发有限许可》《PCL 存储库合理使用指南》《说人话的备注》。

### 2.1 《PCL 分发有限许可》

- 版权方写的是"成都瓜皮龙科技有限公司"。
- 定义："软件"指 Plain Craft Launcher（PCL）及许可方有权许可的源码、二进制、资源、文档；"原样副本"指未经任何方式改变的副本（传输、存储、镜像、压缩、解压不算改变）；"收费"包括付费下载、会员门槛、强制赞助等变相收费。
- 授予的权利：**在持续不收费的前提下**，可以存储、镜像、备份、上传、下载、向公众提供、传播和再分发**原样副本**，全球、非排他、免版税、不可再许可。
- 文本**没有**写"可以修改后分发"，也**没有**单独授予修改权；它只围绕"原样副本"的再分发展开。（事实描述，不解释法律后果。）

### 2.2 《PCL 存储库合理使用指南》

- 原文开头明确："本指南仅作为说明，不是法律文件或协议。"后面写的是开发者"在遵守本指南，且善意、合理使用的情况下，不会追究责任"的承诺。
- **轻度使用**（只参考极小部分内容，或做出的软件与 PCL 实质功能无重叠）要求：适当署名；不得与 PCL 混淆或暗示与 PCL 有关（反例点名"Potato Craft Launcher""PCL 手机版"）。
- **重度使用**（包括"基于本存储库进行修改"）要求：
  1. 明确表示是第三方基于 PCL 独立二次创作的产物；
  2. 内容名称**必须以 Plain Craft Launcher (PCL) 开头**，并在后面加能表现"由第三方修改"的后缀（允许例：PCL 某服务器魔改版、PCL PR Collection、PCL (XX Bug Fix)；禁止例：PCL 某服务器特供版、PCL 3、PCL 手机版、PCL++、PCL Pro）；
  3. 在**关于页面首位**给出"龙腾猫跃"署名及对应赞助链接，公开源代码，并继续使用该文件作为指南（可参考 CC BY-SA）；
  4. 软件**若有 Minecraft 启动功能**，应实现与 `McLaunchPrecheck` 一致（或更激进）的正版购买劝导与赞助劝导；
  5. 软件在 Windows 上运行时，不应有与 PCL 赞助解锁类似的功能或表现，例如界面主色相必须为蓝色。
- 《说人话的备注》再次说明这些规则主要针对恶意使用、倒卖和"山寨版"，常规善意使用没问题；有疑问可去 PCL 仓库 discussions 提问。

## 3. 与本项目直接相关的事实

### 3.1 项目名与指南的命名条件

- 指南重度使用第 2 条要求名称以 `Plain Craft Launcher (PCL)` 开头并带第三方修改后缀。
- 当前已定项目名是 `Xperia-fukuro`（`CLAUDE.md`），字面上不满足该写法。
- 这里只记录"文本要求"与"当前名称"的差异，是否改名、改显示名还是只改仓库名，由用户决定。

### 3.2 关于页现状（符合与需要改的部分）

- `PCL.Launcher/Views/Pages/Setup/PageSetupAbout.axaml`：第一张卡片的**第一行**就是"龙腾猫跃 / Plain Craft Launcher 的原作者！"（第 44–50 行），右侧有"赞助原作者"按钮（第 64 行）。
- 赞助链接在 `PageSetupAbout.axaml.cs:26`：`https://ifdian.net/a/LTCat`。
- 同一张卡片里还有 PCL Community、PCL CE 自身两行；下方有"上游版权与法律信息"卡片（第 278 行起），链接到 PCL 用户协议（`https://shimo.im/docs/rGrd8pY8xWkt6ryW`）和上游源码（`https://github.com/Meloong-Git/PCL`）。
- 结论性事实：署名和赞助链接的位置已经符合指南第 3 条对"首位"的要求；但页面同时把 PCL 的协议当作本软件的协议展示（见下条），且软件名、源码地址仍指向上游。

### 3.3 首次启动的协议与遥测

- `PCL.Launcher/Views/Shell/StartupEffects.cs:263` 的 `ShowEulaDialogAsync`：首次启动弹"协议授权"，文案是"在使用 PCL CE 前，请同意 PCL 的用户协议与免责声明。"，查看按钮打开 `shimo.im` 的 PCL 协议文档。
- 未同意时点"拒绝"会直接退出（`UiInteraction.Shutdown()`）；同意后写入 `States.System.LauncherEula`。
- 紧接着 `StartupEffects.cs:291` 的 `ShowTelemetryDialogAsync` 会询问是否启用遥测（数据项见文案）。
- 这正是 `CLAUDE.md` 要求替换成"我们自己的使用条款和刷机风险免责声明"的位置；在替换前，界面呈现的是 PCL 的协议，且没有刷机风险内容。

### 3.4 遥测与"上游身份"相关代码（事实）

- `PCL.Core/App/Essentials/TelemetryService.cs`：Sentry SDK 上报；本次本地构建没有 DSN（日志"未找到 Sentry DSN"），实际没有发出遥测数据。
- `PCL.Core/App/Essentials/BuildProvenance.cs`：`BranchCode == 0` 的构建（发布版）会在启动时请求 `https://api.github.com/repos/PCL-Community/PCL-CE/releases/tags/v<版本>` 校验自身哈希；请求失败时放行（fail-open）。源码注释明确说"如果你正在制作 PCL-CE 的 fork……可以放心关闭、忽略甚至移除此功能"。
- 启动时还会访问 PCL CE 的更新/公告/下载源（`UpdateManager` 里硬编码了 MirrorChyan、`s3.pysio.online`、`staticassets.naids.com`、GitHub raw 等）。这些与"不保留指向上游的自动更新、遥测、统计"直接相关，摘除阶段要处理。

### 3.5 捆绑的二进制文件

`PCL.Launcher/Resources/` 下有以下非源码文件。仓库里**没有单独的许可证文件**，但 `PCL.Launcher/Resources/metadata.json` 的 `licenses` 列表（34 项，关于页"许可与版权声明"的数据源）写了部分组件的版权和许可证：

| 文件 | 大小 | 代码里的用途 | 许可证/来源（据 metadata.json） |
| --- | --- | --- | --- |
| `forge-installer.jar` | 3.2 KB | Forge 安装包装器（`PCL.Launcher.Core/Install/LoaderInstallerInstall.cs`） | 未单列 |
| `java-wrapper.jar` | 15.8 KB | Java 启动包装（`Launch/Arguments.cs`） | MIT，`00ll00/java_launch_wrapper` |
| `legacyfix.jar` | 997 KB | 旧版游戏兼容修复（`Launch/ArgumentsGame.cs`、`ArgumentsJvm.cs`） | **GPL-3.0**，`betacraftuk/legacyfix` |
| `lwjgl-unsafe-agent.jar` | 9.8 KB | LWJGL agent（`Launch/ArgumentsGame.cs`） | Apache-2.0，`HMCL-dev/lwjgl-unsafe-agent` |
| `transformer.jar` | 2.2 KB | 库下载转换（`Minecraft/Library/Net.cs`） | 未单列 |
| `linkd.exe` | 11 KB | Windows 联机链接（`Launch/Arguments.cs`） | 未单列 |
| `mcmod.buf` | 687 KB | 内置 mcmod 数据库（`Comp/Database.cs`） | 未单列 |
| `Font.ttf` | 20 KB | 源码中未找到引用；界面字体实际用 `Assets/Fonts` 的内嵌 "PCL English" | 未单列 |

- 这些文件基本都服务于 Minecraft 功能，计划随 MC 功能一起摘除；摘除前不要单独再分发它们。
- `legacyfix.jar` 是 **GPL-3.0**：只要发布包里还带这个文件，就需要按 GPL-3.0 处理该组件的分发义务（例如提供对应源码）。它属于 MC 功能，按计划会被移除；如果决定保留，需要用户单独确认。
- `Iconfont`（阿里巴巴矢量图标库）、`Iconmonstr`、`Lucide` 等图标资源也在 metadata 列表中；`Font.ttf` 如果最终保留，需要补查字体名称与授权。

### 3.6 `docs/upstream/` 为空

`CLAUDE.md` 要求"对照 `docs/upstream/` 里用户保存的原版文本"核实 `Plain Craft Launcher 2/LICENCE`。实际情况：

- `docs/upstream/` 目录为空，没有可对照的原文；
- 仓库里也没有 `Plain Craft Launcher 2/` 目录（Avalonia 迁移后项目改为 `PCL.Launcher/`、`PCL.Launcher.Core/`）；
- 本次只能以仓库内文件和历史提交为准，无法独立确认上游原文是否被改过（只能确认当前两份文件与 `upstream/dev` 相同、来自上游提交）。

### 3.7 第三方依赖

直接 NuGet 依赖 36 个（详见各 `csproj`），主要有：Avalonia 12.1.6、Microsoft.Extensions.* 11.0.0、Sentry 6.6.0、SQLitePCLRaw、Dapper、LiteDB、SharpZipLib、protobuf-net、Polly、YamlDotNet、CommunityToolkit.Mvvm、MSTest 4.5.3 等。

- `metadata.json` 的 `licenses` 列表有 34 项，覆盖 Avalonia、Sentry、Dapper、LiteDB、SharpZipLib 等多数直接依赖和部分捆绑二进制，是界面上实际展示的清单。
- 但它**不是全集**：例如 `System.Management`、`System.Security.Cryptography.ProtectedData`、`Microsoft.CodeAnalysis.*`、`MSTest.*`、`Avalonia.Wayland`、`AvaloniaUI.DiagnosticsSupport` 没有单列；`Avalonia` 条目只写了 Desktop/Themes.Fluent/Fonts.Inter。
- `PCL.Core/NOTICE` 只列 5 个组件，明显落后于现状。发布前需要按实际依赖和随包二进制重新生成第三方声明（路线图第 6 步）。

## 4. 待用户决定（记录，不催）

1. 我们新增的代码（`Flasher.Core` 等）用什么许可证。
2. 是否联系 PCL 作者确认授权（或按《合理使用指南》行事）。
3. 产品对外显示名是否要满足"PCL 开头 + 第三方修改后缀"，还是维持 `Xperia-fukuro`；仓库名与显示名是否分开处理。
4. 是否保留 `PCL.Launcher/Resources` 里的字体和任何非 MC 二进制；其余 MC 二进制是否随功能一起删除。
5. `CLAUDE.md` 中"`Plain Craft Launcher 2/` 目录 + `docs/upstream/` 原文比对"这条已与实际仓库结构不符，是否修订该文件。

## 5. 发布前必须做的事（由上述文件直接推出，非法律意见）

- 随发布附 Apache-2.0 副本；保留 PCL `LICENCE` 原样；公开源码。
- 关于页保留"龙腾猫跃"署名与赞助链接在首位，并写明"本产品基于 PCL 二次创作"。
- 用我们自己的《使用条款 + 刷机风险免责声明》替换 PCL 协议弹窗与关于页里的 PCL 协议链接（变砖、数据丢失、保修失效、DRM 密钥丢失等风险条目由用户审定）。
- 不保留指向上游仓库的自动更新、公告、遥测、统计。
- 重新生成第三方组件声明（含 Avalonia 等），与 `PCL.Core/NOTICE`、`metadata.json` 的 licenses 列表对齐。
- 如果发布包里仍带 `legacyfix.jar`（GPL-3.0）或其他第三方二进制，按各自许可证履行义务（源码提供、版权声明等）。
- 不自行发布构建产物（`CLAUDE.md` 明确由用户决定）。
