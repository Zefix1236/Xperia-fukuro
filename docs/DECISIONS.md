# DECISIONS

阶段 0 勘察期间确认的事实与决定。日期 2026-11-20，基线 `ba68b02a`。

## 1. 工具链：需要 .NET SDK 11，不是 10

**事实**

- 所有 csproj 的 `TargetFramework` 是 `net10.0`，但 `LangVersion` 是 `15.0`（C# 15）。
- 本机原装 SDK 只有 `10.0.113`，用它构建直接报 `CS1617: /langversion 的选项"15.0"无效`。
- 上游 CI（`.github/workflows/build-test.yml`、`reusable-build.yml`）使用 `dotnet-version: 11.0.x`；官方开发文档要求 ".NET SDK 11.0 或更高"，运行时为 ".NET Desktop Runtime 10.0"。
- `scripts/publish.sh` 默认 `DOTNET="$HOME/.dotnet/dotnet"`，即上游假设 SDK 11 装在用户目录。

**决定 D0-1**

- 本仓库的开发/构建使用 **.NET SDK 11.0.100 + .NET 10.0.15 运行时**。
- 为不污染系统，二者安装在仓库内 `.dotnet/`（约 1.1 GB，未跟踪，已加入 `.git/info/exclude`），构建时设置 `DOTNET_ROOT`、`DOTNET_CLI_HOME`。
- `CLAUDE.md` 里".NET 10"指的是目标框架/运行时，不是 SDK 版本；这条事实需要在下次修订 `CLAUDE.md` 时写清楚（需用户同意）。

**环境问题与绕过**

- 该工作区的文件沙箱把 `~/.nuget`、`~/.config`、`~/.dotnet` 设为只读，`dotnet restore` 会报 `Read-only file system`。
- 绕过方式（不修改仓库文件）：`NUGET_PACKAGES=$PWD/.local-nuget/packages`、`NUGET_HTTP_CACHE_PATH=$PWD/.local-nuget/http-cache`，并把系统已有的 `~/.nuget/packages` 作为只读 fallback：
  `dotnet restore ... -p:RestoreAdditionalProjectFallbackFolders=$HOME/.nuget/packages`。
- 普通开发机不需要这些参数，`docs/DEV.md` 里两种写法都记了。

## 2. Linux 构建：通过

命令与结果（SDK 11.0.100，Debug，AnyCPU）：

```bash
dotnet restore 'PCL Community Edition.slnx' -p:RestoreAdditionalProjectFallbackFolders=$HOME/.nuget/packages
dotnet build 'PCL Community Edition.slnx' -c Debug --no-restore
```

- **0 个错误，205 个警告**，全部 8 个项目构建成功（构建 37 秒；还原另计约 30 秒）。
- 警告主要是可空性（CS8601/CS8604）、未使用字段（CS0414/CS0649）、过时 API（CS0618）、MSTest 分析器、以及两条 Avalonia `AVLN3001`（`PageSetupJava`、`PageSetupLeft` 的 XAML 没有公共构造函数，运行时加载器不可达，设计上由代码创建）。
- 交叉编译 Windows 目标也通过：`dotnet restore PCL.Launcher/PCL.Launcher.csproj -r win-x64 -p:Platform=x64` + `dotnet build ... -r win-x64 --no-restore`，**0 错误，198 警告**。说明代码层面没有 Linux 专属编译障碍。

## 3. 测试：487 个用例，0 失败（1 个环境敏感）

命令：

```bash
dotnet test PCL.Core.Test/PCL.Core.Test.csproj -c Debug --no-build
```

结果：**通过 480，跳过 7，失败 0，共 487，耗时约 52 秒**。

- 跳过的用例是平台/网络相关：`TestToast`、`TestFileNameValidateWithParentFolder`（Windows 文件名规则）、`Version1KeyIsMigratedToCng`（Windows CNG）、`TestSrvQuery`（Minecraft 服务器查询，需要网络/服务器）。
- 发现一个**测试对 `PCL_DATA_DIR` 敏感**：`GameFoldersTest.ApplySelectedFolderExpandsPlaceholderAndIgnoresEmpty` 断言 `$.minecraft` 展开到可执行文件目录，但如果设置 `PCL_DATA_DIR`，实际路径会变成 `$PCL_DATA_DIR/.minecraft`，断言失败。这是测试假设问题（不是产品缺陷），跑测试时不要设 `PCL_DATA_DIR`；需要把数据目录指向可写位置时，用 `XDG_CONFIG_HOME`、`TMPDIR` 而不是 `PCL_DATA_DIR`。
- 第一次跑测试还遇到 `~/.config/PCLCE_Debug` 只读导致测试进程崩溃（`Paths` 静态构造函数抛异常），把 `XDG_CONFIG_HOME` 指到工作区后解决。

## 4. Linux 运行：通过（X11 与原生 Wayland 都能起窗口）

环境：Arch Linux x64、GNOME Wayland 会话、16 GB 内存、无独立 GPU 直通（沙箱内 MESA 走软件/失败回退）。

**4.1 X11（`PCL_FORCE_X11=1`）**

- 进程启动约 1 秒后创建窗口，X11 属性：`WM_CLASS="pcl-community-edition","PCL.Launcher"`、`_NET_WM_NAME="Plain Craft Launcher Community Edition "`、尺寸 900×550，`_NET_WM_WINDOW_TYPE_NORMAL`。
- 首次启动依次弹出：Debug 特殊版本提示（可用 `PCL_DISABLE_DEBUG_HINT=1` 跳过）→ 协议授权（PCL 协议，shimo 链接）→ 遥测询问。接受后进入 `Launch` 页面；日志有 `切换页面：Launch/`、`MainWindow(IsVisible=True, ...)`、持续动画帧。
- 窗口截图保存于 `.run/pcl-window.png`（第一次运行）与 `.run/pcl-main.png`（第二次运行，900×550，内容非空白）。

**4.2 原生 Wayland（默认，不设 `PCL_FORCE_X11`）**

- 会话类型是 `wayland`，`LinuxDisplayProtocolResolver` 选择原生 Wayland；日志出现 `帧驱动模式 clock=timer，原生 Wayland 固定帧率`，生命周期到 `WindowCreated`/`Running`，无崩溃。
- 标准输出有 MESA/ZINK/EGL 警告与 dconf 只读警告，判断为**沙箱没有 GPU/配置目录**导致，不是应用代码错误；在没有沙箱的普通桌面会话需要再验证一次。

**4.3 运行期行为（观察）**

- 启动时访问上游服务器：更新检查（"远程最新版本: 2.15.1"）、公告、Mojang 版本清单/下载源测速、EasyTier 依赖下载尝试（`staticassets.naids.com`、`s3.pysio.online`）。
- 扫描 `~/.minecraft` 下已有实例，并尝试写 `<实例>/PCL/config.v1.yml`；沙箱只读，日志中产生 5 次 `配置文件保存失败`，真实环境会成功写入用户数据。
- 遥测：`TelemetryService` 初始化 Sentry；本地构建没有 DSN（日志"未找到 Sentry DSN"），**实际没有数据发出**。`BuildProvenance` 在发布版（`BranchCode == 0`）会请求 GitHub 上游 Release 哈希，Debug 构建因 `BranchCode != 0` 跳过。
- 无关紧要的启动警告：`[远程执行服务|rpc] Dependency injection failed: rpc-property@128`（上游现有行为，暂未确认影响）；隔离配置目录导致 `用户密钥已重建（version 3）`。

**决定 D0-2（运行测试的隔离方式）**

以后在本机跑 UI 验证时，统一这样隔离，避免读写用户的真实数据：

```bash
XDG_CONFIG_HOME=$PWD/.run/config XDG_DATA_HOME=$PWD/.run/data \
PCL_DATA_DIR=$PWD/.run/pcl PCL_MINECRAFT_DIR=$PWD/.run/minecraft \
TMPDIR=$PWD/.run/tmp ./PCL.Launcher/bin/Debug/net10.0/PCL.Launcher
```

- 想截 X11 窗口图时加 `PCL_FORCE_X11=1`；Wayland 下用默认值。
- 首次启动要过协议/遥测弹窗；第二次启动会跳过。

## 5. Windows 专有代码：已隔离，不阻塞 Linux

- Windows 实现集中在 `PCL.Platform/Windows`（20 个文件），接口在 `PCL.Core/Platform`（约 30 个），Linux 实现在 `PCL.Platform/Linux`（13 个）。
- `PCL.Core` 内仍有按 `OperatingSystem.IsWindows()` 分支的工具类（进程、内核、环境变量、密钥保护、注册表 Java 扫描、管道、提权），但都有非 Windows 分支或降级路径；Linux 能完整启动并跑主界面。
- 已知功能差异（上游设计，不是缺陷）：
  - Linux/macOS 不支持应用内更新（`ManualUpdatePolicy`），只给下载页链接；
  - Windows 任务栏缩略图裁剪、GPU 偏好、Toast/AUMID、DPAPI/CNG 密钥保护、提权在 Linux 无对应实现；
  - `PCL.Launcher/Resources/linkd.exe` 是 Windows 联机组件，Linux 用不到。

## 6. 上游同步流程：流程通，当前无可合并内容

- `upstream` 已配置为 `PCL-Community/PCL-CE`，push 已禁用；`origin` 是 `Zefix1236/Xperia-fukuro`。
- `git fetch upstream --prune`：无新提交。`upstream-mirror`、`origin/upstream-mirror` 都等于 `upstream/dev`（`b51179f9`）；`main` 比它多 1 个文档提交（`ba68b02a`）。
- 演练：从 `main` 开 `sync/2026-11-20` → `git merge upstream-mirror` → "已经是最新的" → 删除分支。**冲突解决流程还无法验证**，需要等上游有新的 `dev` 提交后再走一遍。
- `git config rerere.enabled true` 已经设置。

**决定 D0-3**：同步流程暂时按 `CLAUDE.md` 原样执行；下次实际同步时记录上游哈希、冲突文件和构建/测试结果。

## 7. 阶段 0 的产物与未决事项

- 本阶段只改文档：`docs/{LICENSE-NOTES,ARCHITECTURE,DECISIONS,DEV,PROGRESS,FORK-PATCHES}.md`，未修改任何上游代码；`git status` 只应看到这些文档。
- 工具目录 `.dotnet/`、`.dotnet-home/`、`.local-nuget/`、`.run/` 都是本机产物，未跟踪、已写 `.git/info/exclude`，可随时删除（删后需重新还原/安装）。
- 运行期截图与日志留在 `.run/`，供用户查看，也可删除。

需要用户决定（与 `docs/LICENSE-NOTES.md` 第 4 节一致，此处只列与阶段 0 直接相关的）：

1. 产品显示名是否满足 PCL《合理使用指南》的"PCL 开头 + 第三方修改后缀"要求；仓库名是否保持不变。
2. 我们新增代码的许可证。
3. 摘除 Minecraft 的顺序与方式（整块移除，还是先隐藏入口再逐步排除编译）。
4. 是否保留 macOS 支持（上游支持，维护成本另算）。
