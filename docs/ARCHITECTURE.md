# 架构

阶段 0 勘察结果，整理日期 2026-11-20，基线提交 `ba68b02a`（= 上游 `b51179f9` + 文档提交）。
每个结论后尽量给出文件路径；没写路径的是从构建/运行日志直接观察到的。

## 0. 快速结论

- 仓库 = 一个 `.slnx` + 8 个项目，分层清晰：`PCL.Core`（核心库）← `PCL.Core.UI`（Avalonia 适配层）、`PCL.Launcher.Core`（业务层）、`PCL.Platform`（平台实现）← `PCL.Launcher`（界面/入口）。
- **PCL 风格界面不在单独的主题包里**：`PCL.Launcher/UI/Controls`（87 个文件，`MyButton`/`MyCard`/`MyListItem` 等）+ `PCL.Launcher/UI/Styles` + `PCL.Launcher/UI/Theming` + `PCL.Core.UI`（动画、对话框、通知、线程、主题后端）。
- **Minecraft 业务是"可摘除的模块"**：页面通过 `PCL.Launcher/Navigation/Catalog/*PageModule` 注册，MC 相关页面、ViewModel、`PCL.Core/Minecraft`（117 个文件）、`PCL.Core/Link`（60 个文件）、`PCL.Launcher.Core` 下的 Minecraft/Comp/Install/Launch/Modpack 等目录都是相对独立的。
- 平台层已经把 Windows 专有代码隔离在 `PCL.Platform/Windows`（20 个文件）和 `PCL.Core` 的几个工具类里；Linux 实现有 13 个文件，上游已经能原生跑 Wayland 和 X11。
- 上游启动时会访问 PCL CE 自己的更新/公告/下载服务器，并带有 Sentry 遥测与"官方构建哈希校验"；这些要在后续阶段移除。
- 新功能（刷机）可以完全放在新项目 `Flasher.Core`，只通过 `PCL.Core/Platform` 的接口和新的 `IPageModule` 接触上游。

## 1. 解决方案与依赖方向

| 项目 | 目标框架 | 职责 | 直接依赖 |
| --- | --- | --- | --- |
| `PCL.Launcher` | net10.0 | Avalonia 入口（`WinExe`）、主窗口、页面、PCL 控件、样式、组合根 | Core.UI、Launcher.Core、Platform、SourceGenerators |
| `PCL.Launcher.Core` | net10.0 | 启动器业务层：MC 实例/安装/启动/下载/整合包/更新 | PCL.Core |
| `PCL.Core.UI` | net10.0 | Avalonia 适配：动画、对话框、通知、Hosting、本地化后端、主题后端 | PCL.Core |
| `PCL.Platform` | net10.0 | 平台原生实现（Windows/Linux/macOS） | PCL.Core |
| `PCL.Core` | net10.0 | 与 UI 无关的核心库：生命周期/IoC、配置、日志、网络、Minecraft 数据模型、联机 | SourceGenerators |
| `PCL.Core.Test` | net10.0 | MSTest 测试（487 个用例） | Core、Core.UI、Platform、Launcher.Core、**PCL.Launcher** |
| `PCL.Localization` | net10.0 | 语言资源的中立目录（`LocalizationCatalog`），当前**没有被任何项目引用**；语言文件实际通过 `PCL.Core.UI.csproj` 的 `AvaloniaResource Include="..\PCL.Localization\Languages\*.axaml"` 打进 UI 程序集 | 无 |
| `PCL.Core.SourceGenerators` | netstandard2.0 | 编译期生成（配置项、本地化属性等） | 无 |

依赖方向是单向的：`PCL.Core` 不引用任何 UI 项目。`PCL.Launcher.Core` 与 `PCL.Platform` 之间没有直接引用，平台服务通过 `PCL.Core/Platform` 的接口注入。

## 2. 启动流程

入口 `PCL.Launcher/Program.cs`：

1. `PlatformDetector.Detect()` 判断 Windows/Linux/macOS（`PCL.Core/Platform/PlatformDetector.cs`）。
2. `PlatformStartup.Initialize(...)`：Linux 走 `LinuxPlatformStartup`，macOS 走 `MacOsPlatformStartup`。
3. `PlatformRegistration.Register(platform)` 注册平台服务；`LauncherServiceRegistration.Build(...)` 组装 DI 容器（`PCL.Launcher/Composition/LauncherServiceRegistration.cs`）。
4. `StartupCommandHandler.TryHandle(args, services)` 处理无需界面的命令（目前只有 GPU 偏好命令）。
5. 配置 Avalonia：Linux 下按 `PCL_FORCE_X11` / `PCL_USE_WAYLAND` 或会话类型选择 X11/Wayland（`LinuxPlatformStartup.cs`，判定逻辑在 `PCL.Core/Platform/LinuxDisplayProtocol.cs`）。
6. `PageRegistry.Initialize(...)`、`AnimationRegistration.Register()`，然后 `Lifecycle.OnInitialize()` 启动生命周期。
7. 生命周期到达 `WindowCreating` 时创建 `MainWindow`；`WindowCreated` 后 `StartupEffects` 依次弹出提示：特殊版本提示（仅 Debug）、环境警告、**协议授权**、**遥测询问**。

生命周期状态（`PCL.Core/App/IoC/LifecycleState.cs`）：
`Manual → BeforeLoading → Loading → Loaded → WindowCreating → WindowCreated → Running → Closing → Exiting`。

服务用特性声明（`[LifecycleService]`、`[LifecycleStart]`、`[LifecycleScope]` 等，见 `PCL.Core/App/IoC/Attributes.cs`），由 `Lifecycle` 自动实例化、按优先级启动/停止。当前服务列表（`grep LifecycleService`）：

- 基础：`StartupService`、`Lifecycle`、`LogService`、`ConfigService`、`LocalizationService`、`ThemeService`、`NetworkService`、`EventBusService`、`DatabaseService`、`CacheServiceManager`、`SingleInstanceService`、`ApplicationService`、`MainWindowService`、`PromoteService`、`RpcService`、`TelemetryService`、`UpdateService`、`DependencyCheckService`。
- MC 相关：`Minecraft.JavaService`、`Minecraft.Profile.ProfileService`、`Link.Lobby.LobbyService`。

## 3. PCL 风格 UI（要保留的部分）

### 3.1 应用与窗口

- `PCL.Launcher/App.axaml`：`FluentTheme` + 6 个自定义样式文件（`PageStyles`、`Global`、`Controls/Inputs`、`Controls/Feedback`、`Controls/MyCard`、`Controls/ScrollBars`），资源合并 `UI/Theming/ThemeConstants.axaml`。
- `PCL.Launcher/Views/MainWindow.axaml`：850×500、无系统装饰、透明背景、自绘圆角窗口；包含标题栏、左侧页面宿主、右侧页面宿主、背景层、提示/弹窗层。
- `PCL.Launcher/Views/Shell/`：`TitleBar`、`BackgroundHost`、`ExitFlow`、`FileDragHandler`、`KeyboardHandler`（Esc/F11/F12/F5/Enter 快捷键与"隐藏实例/隐藏功能"开关）、`ExtraButtonsPanel`、`StartupEffects`。

### 3.2 控件与样式

`PCL.Launcher/UI/Controls/`（87 个文件）按用途分目录：

- `Buttons/`：`MyButton`、`MyTextButton`、`MyIconButton`、`MyIconTextButton`、`MyExtraButton` 等。
- `Containers/`：`MyCard`、`MyListItem`、`MyPageLeft`/`MyPageRight`、`MyScrollViewer`/`MyScrollBar`、`MyCollapseBar`、`MyResourceSelectBar`、`WaterfallPanel`、`MyVirtualizingElement`。
- `Inputs/`：`MyTextBox`、`MyPasswordBox`、`MyComboBox`、`MyCheckBox`、`MyRadioButton`、`MySlider`、`MySearchBox`。
- `Feedback/`：`MyHint`、`MyToast`、`MyLoading`、`MyImage`、`MyDropShadow`、字体加载（通用）、MOTD 渲染（MC 相关）。
- `Navigation/`、`Menus/`、`SvgIcon/`、`Behaviors/`（剪贴板拦截等）、`Minecraft/`（MC 版本列表/安装卡片，属摘除范围）。
- 样式：`PCL.Launcher/UI/Styles/{Global,PageStyles}.axaml` + `Styles/Controls/*.axaml`。
- 主题：`PCL.Launcher/UI/Theming/ThemeCoordinator.cs` 把 `PCL.Core/UI/Theme/ThemeService` 的颜色主题接到 Avalonia 的 `Application.Resources` 与 `RequestedThemeVariant`（明/暗）。颜色主题枚举 `ColorTheme`（`PCL.Core/App/ConfigEnums.cs:35`）只有 4 种蓝：SkyBlue(235)、CatBlue(255)、DeathBlue(268)、HmclBlue(275)，与 PCL 指南"主色相保持蓝色"一致。

### 3.3 动画、对话框、通知

- 动画：`PCL.Core.UI/Animation`（94 个文件），`PCL.Launcher` 侧注册/驱动（`AnimationRegistration`、`PCL.Launcher/UI/...`）。Wayland 下用定时器代替 RAF（`LinuxPlatformStartup.cs` 的 `AnimationService.PreferTimerClock`）。
- 对话框：`PCL.Core/UI/Dialogs`（抽象）+ `PCL.Launcher/UI/Dialogs`（`MyMsgDialog` 等具体实现）+ `PCL.Launcher/UI/DialogPresenter.cs`。
- 提示/气泡：`MyHint`、`MyToast`、`HintPresenter`（`PCL.Launcher/UI`）。
- 系统通知：`PCL.Core.UI/Notifications/{Windows,Linux,MacOs}NotificationService.cs`；Windows 实现调用 `PCL.Core/UI/WindowsToastNotification.cs`。

### 3.4 本地化

- 语言文件：`PCL.Localization/Languages/{zh-CN,zh-TW,en-US,en-GB,ja-JP,fr-FR,es-ES}.axaml`，每行一个 `x:Key`。
- 运行加载：`PCL.Core.UI/Localization/LocalizationBackend.cs` 从 `avares://PCL.Core.UI/Localization/Languages/{code}.axaml` 读取（文件由 `PCL.Core.UI.csproj` 嵌入）。
- XAML 用法：`{loc:Loc Key}`（`PCL.Core.UI/Localization/LocExtension.cs`）；代码用法：`Lang.Text("Key")`。
- 新增界面文案必须同时改所有语言文件（`PCL.Core.Test/LocalizationTest.cs`、`LocalizationUsageTest.cs` 会校验 key 完整性）。

## 4. 页面与导航（摘除 MC 的主要抓手）

### 4.1 结构

- `PCL.Launcher/Navigation/`：
  - `PageCatalogBuilder` + `PageCatalog`：从 `IPageModule` 聚合页面描述，校验重复 ID、默认子页、主入口。
  - `PageIds`（顶级页面）、`SubPageIds`（子页面）、`MainTab`（Launch/Download/Tools/Setup 四个标题栏入口）。
  - `PageRouter`、`PageHostFactory`、`PageViewFactory`、`PageRegistry`、`PageNavigationStateMachine`：负责挂载与路由。
- `PCL.Launcher/Navigation/Catalog/` 的 6 个模块就是"功能清单"：

| 模块 | 顶级页 | 主要子页 | MC 相关？ |
| --- | --- | --- | --- |
| `LaunchPageModule` | 启动 | 启动页；登录/皮肤页由页面内部切换 | **是**（登录/启动 MC） |
| `DownloadPageModule` | 下载 | 游戏本体、Forge/NeoForge/Fabric 等、Mod/整合包/资源包/光影/世界、收藏夹、客户端 | **是** |
| `InstancePageModule` | 实例 | 概览/设置/导出/世界/截图/Mod/资源包/光影/服务器 | **是** |
| `ToolsPageModule` | 工具 | 联机大厅、测试工具 | 联机**是**；测试工具部分通用 |
| `SetupPageModule` | 设置 | 启动、Java、游戏管理、联机、界面、语言、杂项、关于、更新、反馈、日志 | 混合：界面/语言/关于/日志/杂项通用；启动/Java/游戏管理/联机/更新是 MC 或上游更新专用 |
| `ShellPageModule` | 外壳（不可从标题栏进入） | 社区资源详情、帮助详情、任务管理器、游戏日志 | 混合：任务管理器/帮助通用，资源详情/游戏日志是 MC |

- 隐藏机制：`Config.Preference.Hide.*` 可以在设置里隐藏部分功能，F12 强制显示（`PCL.Launcher/Views/Shell/KeyboardHandler.cs`、`PageDescriptor` 的 `visible` 参数）。这不是真正的移除，只适合过渡。

### 4.2 摘除 MC 的可行切入点（判断，不是决定）

1. **页面层**：从 `PageCatalog` 的模块列表里去掉 `DownloadPageModule`、`InstancePageModule`、`LaunchPageModule`，把 `MainTab` 减到 Tools/Setup（或新增 Flasher 入口），设置页里去掉 Java/启动/游戏管理/联机/更新子页。需要同步修改 `PageIds`、`SubPageIds`、`PageSetupLeft`、`PageSetupHost` 等引用点。
2. **业务层**：`PCL.Launcher.Core` 的 `Minecraft/`、`Comp/`、`Download/`、`Install/`、`Launch/`、`LocalComp/`、`Modpack/`、`CrashAnalysis/` 可以从 csproj 排除编译（`<Compile Remove="..."/>`），而不是物理删除。
3. **核心层**：`PCL.Core/Minecraft/`、`PCL.Core/Link/`、`PCL.Core/App/Essentials/{TelemetryService,UpdateService,BuildProvenance}` 以及 `PCL.Core/Link` 相关服务，同样优先用 csproj 排除。注意 `PCL.Core` 被 `PCL.Platform` 依赖，排除时要处理 `WindowsMinecraftPlatformRules`、`Posix...` 等 MC 平台规则接口。
4. **UI 资源**：`PCL.Launcher/UI/Controls/Minecraft/`、`PCL.Launcher/Resources/*.jar|mcmod.buf|linkd.exe` 一并移除。
5. **启动行为**：`StartupEffects` 的协议/遥测弹窗、`UpdateManager` 的启动检查、`BuildProvenance` 的 GitHub 请求必须改掉。
6. **依赖**：`PCL.Launcher.Core` 的 `Comp`/`Download` 使用 Dapper、LiteDB、protobuf-net 等；`PCL.Core` 使用 `System.Management`（Windows 硬件信息）、`PlainToolkit.CngProtectedData`（Windows 密钥保护）。摘除后可以精简，但要确认没有共用。

## 5. Minecraft 业务清单（摘除目标）

| 位置 | 规模（跟踪文件数） | 内容 |
| --- | --- | --- |
| `PCL.Core/Minecraft/` | 117 | 版本模型/解析、Java、Maven/库、启动参数、Profile、存档、Yggdrasil 登录 |
| `PCL.Core/Link/` | 60 | 联机大厅、EasyTier、中继/NAT 打洞 |
| `PCL.Launcher.Core/Minecraft/` | 55 | 实例、资源、版本对比、JarInJar、Watcher |
| `PCL.Launcher.Core/Comp/` | 14 | Mod/整合包/资源包社区下载（CurseForge/Modrinth 等） |
| `PCL.Launcher.Core/Install`、`Launch`、`LocalComp`、`Modpack`、`Download`、`CrashAnalysis` | 约 50 | 安装管线、启动参数、本地资源、整合包、下载、崩溃分析 |
| `PCL.Launcher.Core/Updates/` | 13 | **启动器自身更新**（MirrorChyan/Minio/GitHub），不是 MC，但属上游身份，需移除 |
| `PCL.Launcher/Views/Pages/{Download,Instance,Launch}`、`ViewModels/{Download,Instance,Launch}` | 109 | MC 页面与 ViewModel |
| `PCL.Launcher/UI/Controls/Minecraft/` | 5 | MC 版本列表/安装卡片 |
| `PCL.Launcher/Resources/` | 多个二进制 | Forge 安装器、Java wrapper、legacyfix、lwjgl agent、linkd.exe、mcmod 数据库等 |

启动期会发生的 MC 行为（本次运行观察）：

- 扫描 `~/.minecraft` 及 `Config` 里记录的实例，读取/写入 `<实例>/PCL/config.v1.yml`（每个实例一份本地配置）。
- 扫描系统 Java（`JavaService`），解析版本。
- 访问 Mojang 版本清单、下载源测速（`DlClientList`、Mojang 官方源加载耗时等）。
- 加载联机大厅服务 `LobbyService`，尝试下载 EasyTier 依赖。

## 6. 平台层与 Windows 专有代码

### 6.1 抽象接口

`PCL.Core/Platform/` 定义约 30 个接口（`IFileDialogService`、`IShellLauncher`、`IShortcutService`、`IWindowInterop`、`IGpuPreferenceService`、`IFatalDialogService`、`IJavaPlatformInfo`、`IMinecraftPlatformRules`、`ISystemProxyProvider`、`IRuntimeEnvironmentService`、`IKeyProtectorFactory` 等），由 `PCL.Platform/PlatformServiceRegistration.cs` + `PlatformModuleFactory` 按 `PlatformKind` 注册。

### 6.2 实现分布

- 通用：`PCL.Platform/Shared`（16 个文件：进程启动、环境变量、回收站基类、窗口互操作基类、Unix 密钥保护工厂等）+ `PCL.Platform/Posix`（3 个）。
- Linux：`PCL.Platform/Linux`（13 个）：路径、硬件信息、Java 信息、启动环境、MC 平台规则、回收站、快捷方式、代理、运行时检查、系统事实、窗口互操作（含 `libX11.so.6` P/Invoke）、致命错误对话框。
- Windows：`PCL.Platform/Windows`（20 个）：上述接口的 Windows 实现 + GPU 偏好、任务栏进度、DPAPI/CNG 密钥保护、注册表 Java 扫描、Toast 通知、AUMID 等。
- macOS：`PCL.Platform/MacOS`（12 个）。

### 6.3 Windows 专有代码/行为清单

- 平台目录：`PCL.Platform/Windows/*`（20 个文件）。
- `PCL.Core` 内按 `OperatingSystem.IsWindows()` 分支的：`Utils/OS/ProcessInterop.cs`、`KernelInterop.cs`、`SystemInfo.cs`、`EnvironmentInterop.cs`、`Utils/Secret/{EncryptHelper,Identify,IdentifyOld,WindowsKeyProtector}.cs`、`Minecraft/Java/Scanner/RegistryJavaScanner.cs`、`IO/PipeComm.cs`、`App/Essentials/PromoteService.cs`。
- Windows 专用包：`System.Management`、`System.Security.Cryptography.ProtectedData`、`PlainToolkit.CngProtectedData`（`PCL.Core.csproj`）。
- 行为差异：Windows 才有的应用内更新（`WindowsUpdatePolicy`），Linux/macOS 是 `ManualUpdatePolicy`（只给下载页链接）；任务栏缩略图裁剪（`TaskbarProgress.SetThumbnailClip`）在 Linux 上是空实现；Windows 提权/单实例管道在 Linux 用 `PipeServer`/flock 等替代。
- `EnableWindowsTargeting=true` 已打开，Linux 上交叉编译 Windows 目标不需要额外开关。

## 7. 数据与配置路径

| 内容 | 路径（Linux） | 说明 |
| --- | --- | --- |
| 启动器数据 | `$PCL_DATA_DIR`，否则可执行文件目录/PCL，打包安装时 `~/PCLCE` | 日志、本地配置、缓存等 |
| 共享配置 | `$XDG_CONFIG_HOME/PCLCE/config.v1.json`（Debug 构建是 `PCLCE_Debug`） | 状态、偏好、账户等 |
| 本地配置 | `<数据目录>/config.v1.yml` | 本机路径等 |
| 实例配置 | `<Minecraft 文件夹>/versions/<版本>/PCL/config.v1.yml` | 每个实例一份 |
| 日志 | `<数据目录>/Log/Launch-<时间>.log` | 运行记录，含命令/网络/页面切换 |
| Minecraft 文件夹 | `$PCL_MINECRAFT_DIR`，否则 `$PCL_DATA_DIR/.minecraft` 或 `~/.minecraft` | 默认会选中官方启动器的 `~/.minecraft` |

可用环境变量（已核实）：`PCL_DATA_DIR`、`PCL_MINECRAFT_DIR`、`PCL_FORCE_X11`、`PCL_USE_WAYLAND=1`；Debug 构建另有 `PCL_PATH_*`、`PCL_DISABLE_DEBUG_HINT`。

## 8. 构建与测试基建

- 解决方案：`PCL Community Edition.slnx`，配置 `Debug / CI / Release / Beta`，平台 `AnyCPU / x64 / ARM64`；项目 `TargetFramework=net10.0`，`LangVersion=15.0`（**需要 .NET SDK 11**，见 `docs/DECISIONS.md`）。
- CI：`.github/workflows/build-test.yml` 在 push `dev` / PR 时分别构建 win-x64、win-arm64、linux-x64（ubuntu-24.04-arm）、linux-arm64、osx-x64、osx-arm64，并在 Ubuntu/Windows/macOS 跑测试；`reusable-build.yml` 使用 `dotnet-version: 11.0.x`；发布工作流 `release-publish.yml`、`mirrorchyan_*.yml`。
- 脚本：`scripts/publish.sh`（`dotnet publish` 单 RID）、`scripts/package-linux.sh`（AppImage/deb/rpm，需要 appimagetool、nfpm）、`scripts/package-macos.sh`、`scripts/verify-artifact.sh`。
- 测试：`PCL.Core.Test` 使用 MSTest 4.5.3 + `Microsoft.NET.Test.Sdk`，覆盖动画、导航、下载、加密、平台、XAML 校验、本地化等；测试会引用 `PCL.Launcher` 程序集，部分用例依赖真实桌面环境（`TestToast`、`TestSrvQuery` 等在无桌面/无网络时跳过）。
- 直接 NuGet 依赖 36 个，关键的有 Avalonia 12.1.6、Microsoft.Extensions.* 11.0.0、Sentry 6.6.0、SQLitePCLRaw 3.0.5、Dapper 2.1.89、LiteDB 5.0.21、SharpZipLib 1.4.2、polly 8.9.1、YamlDotNet 18.1.0。

## 9. 对新增代码的约束（来自现状）

- 新功能放新文件/新项目：`Flasher.Core`（不引用 UI）+ 独立的刷机页面目录；通过 `PCL.Core/Platform` 的接口访问系统能力，避免 `PCL.Launcher` 直接调 Windows API。
- 页面注册走 `IPageModule`：新增 `FlasherPageModule`，不要改 `PageCatalogBuilder`；这样同步上游时冲突面最小。
- 上游文件改动要加 `// FORK: 原因`（XAML 用注释）并登记 `docs/FORK-PATCHES.md`。
- 新代码风格遵循上游技术规范（LF、UTF-8 无 BOM、`_PascalCase` 私有方法、`LibraryImport` 替代 `DllImport`、`GeneratedRegex` 等）；提交信息按 `CLAUDE.md` 用英文 Conventional Commits。

## 10. 待查/未知

- 联机大厅 `PCL.Core/Link` 的完整外部依赖（EasyTier 下载、服务器地址、密钥）尚未逐个核实。
- `PCL.Core/Updates` 的公告/更新源在 Linux 下的完整请求清单（日志已见 MirrorChyan/Minio 请求）未逐一列出。
- `PCL.Core/App/Config.cs` 的配置分组与默认值很多，摘除 MC 时还要清理 `Config`/`States` 里的 MC 项；本次只做了目录级盘点，没有逐项清理清单。
- 页面模块之外是否还有反射/硬编码引用 MC 类型（编译时 `Compile Remove` 会暴露出来，届时再补）。
