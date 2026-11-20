# DEV

构建、测试、运行命令。以本机（Arch Linux x64、GNOME Wayland）实测为准，2026-11-20 验证。

## 1. 环境要求

| 项目 | 要求 | 说明 |
| --- | --- | --- |
| .NET SDK | **11.0** 或更高 | 项目 `LangVersion=15.0`，SDK 10 会报 `CS1617`；上游 CI 用 `11.0.x` |
| .NET 运行时 | **10.0**（`Microsoft.NETCore.App`） | 目标框架是 `net10.0`；只装 SDK 11 自带的 11.0 运行时跑不起来 |
| 系统 | Linux（X11 或 Wayland）或 Windows 10 1607+；macOS 上游也支持 | 本仓库在 Linux 上已构建并运行通过 |
| 可选 | `adb`、`fastboot`、Java（只影响 MC 功能） | 刷机功能后续需要 adb/fastboot |
| 网络 | 首次还原 NuGet 包需要访问 nuget.org | 国内可用华为云 NuGet 镜像 |

上游官方文档（`https://docs.pclc.cc/ce/developers/setup`）主要面向 Windows，且部分内容还停留在旧项目名（"Plain Craft Launcher 2"）；以本文件为准。

## 2. 安装 SDK 11（不污染系统）

本机原装只有 SDK 10，因此把 SDK 11 和运行时 10 装进仓库内 `.dotnet/`（未跟踪，已在 `.git/info/exclude`）：

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 11.0 --install-dir "$PWD/.dotnet" --no-path
bash /tmp/dotnet-install.sh --channel 10.0 --runtime dotnet --install-dir "$PWD/.dotnet" --no-path
```

之后每个 shell 先设置（建议写进本地 shell 配置，不要提交）：

```bash
export DOTNET_ROOT="$PWD/.dotnet"
export DOTNET_CLI_HOME="$PWD/.dotnet-home"   # SDK 首次运行会往这里写哨兵文件，~/.dotnet 只读时必须改
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export PATH="$DOTNET_ROOT:$PATH"
```

验证：

```bash
dotnet --version          # 11.0.100
dotnet --list-runtimes | grep 10.0.15
```

如果系统本身就有 SDK 11 + 运行时 10，直接用自己的 `dotnet` 即可，不需要以上步骤。

## 3. 还原与构建

```bash
dotnet restore 'PCL Community Edition.slnx'
dotnet build   'PCL Community Edition.slnx' -c Debug
```

- Debug 实测：8 个项目全部成功，0 错误 / 205 警告，约 37 秒。
- 配置有 `Debug / CI / Release / Beta`，平台有 `AnyCPU / x64 / ARM64`；CI 工作流用 `CI` 配置 + 指定 RID/Platform 发布。
- 构建产物在 `PCL.Launcher/bin/Debug/net10.0/`。

Windows 目标交叉编译（Linux 上验证过）：

```bash
dotnet restore PCL.Launcher/PCL.Launcher.csproj -r win-x64 -p:Platform=x64
dotnet build   PCL.Launcher/PCL.Launcher.csproj -c Debug -r win-x64 -p:Platform=x64 --no-restore
```

0 错误 / 198 警告。`PCL.Platform` 已开 `EnableWindowsTargeting`，不需要额外开关。

## 4. 测试

```bash
dotnet test PCL.Core.Test/PCL.Core.Test.csproj -c Debug
```

- 实测：**487 个用例，480 通过，7 跳过，0 失败**，约 52 秒。
- 跳过的用例依赖 Windows 特性或外网（Toast、文件名规则、CNG、Minecraft 服务器查询）。
- **不要在测试时设置 `PCL_DATA_DIR`**：`GameFoldersTest.ApplySelectedFolderExpandsPlaceholderAndIgnoresEmpty` 会因此失败（测试假设数据目录默认在可执行文件旁）。需要可写目录时改 `XDG_CONFIG_HOME`、`TMPDIR`。
- 测试会启动 Avalonia/生命周期的一部分服务；在只读沙箱里需要：

```bash
XDG_CONFIG_HOME="$PWD/.run/test-config" TMPDIR="$PWD/.run/tmp" \
  dotnet test PCL.Core.Test/PCL.Core.Test.csproj -c Debug
```

## 5. 运行

直接跑构建产物：

```bash
./PCL.Launcher/bin/Debug/net10.0/PCL.Launcher
```

或：

```bash
dotnet run --project PCL.Launcher -c Debug
```

说明：

- Linux 默认按会话类型选原生 Wayland；X11 会话用 X11。强制 X11：`PCL_FORCE_X11=1`；强制 Wayland：`PCL_USE_WAYLAND=1`。
- 首次启动会依次弹"特殊版本提示（Debug）→ 协议授权 → 遥测询问"；接受后进主界面。下次不再弹。`PCL_DISABLE_DEBUG_HINT=1` 可跳过 Debug 提示。
- 日志在 `<数据目录>/Log/Launch-<时间>.log`；数据目录默认是 `<程序目录>/PCL`（便携）或 `~/PCLCE`（安装包/程序目录不可写时），可用 `PCL_DATA_DIR` 覆盖。
- **隔离运行**（推荐做 UI 验证时用，避免动用户的 `~/.minecraft` 和真实配置）：

```bash
XDG_CONFIG_HOME="$PWD/.run/config" \
XDG_DATA_HOME="$PWD/.run/data" \
XDG_CACHE_HOME="$PWD/.run/cache" \
PCL_DATA_DIR="$PWD/.run/pcl" \
PCL_MINECRAFT_DIR="$PWD/.run/minecraft" \
TMPDIR="$PWD/.run/tmp" \
PCL_DISABLE_DEBUG_HINT=1 \
./PCL.Launcher/bin/Debug/net10.0/PCL.Launcher
```

- 截图（X11 下）：先 `xprop -root _NET_CLIENT_LIST` 找到新窗口 ID，再 `import -window <id> out.png`。
- 应用会扫描 `~/.minecraft` 的已有实例（只读），并向 `<实例>/PCL/` 写配置；不隔离路径时请注意。

## 6. 发布

```bash
# 单个 RID 发布（默认 Beta 配置）
scripts/publish.sh linux-x64 Beta dist

# Linux 安装包（需要 appimagetool、nfpm）
scripts/package-linux.sh linux-x64 Beta 2.15.1 dist
```

- `scripts/publish.sh` 默认用 `$HOME/.dotnet/dotnet`，可用 `DOTNET=...` 覆盖。
- `package-linux.sh` 生成 AppImage、deb、rpm；`macos` 用 `scripts/package-macos.sh`。
- **不要自己发布构建产物**（`CLAUDE.md`：Releases、安装包由用户决定）。

## 7. 受限沙箱/本机特殊步骤

本工作区的文件沙箱把 `~/.nuget`、`~/.config`、`~/.dotnet`、`/tmp` 设为只读或临时，需要：

```bash
export NUGET_PACKAGES="$PWD/.local-nuget/packages"
export NUGET_HTTP_CACHE_PATH="$PWD/.local-nuget/http-cache"
dotnet restore 'PCL Community Edition.slnx' \
  -p:RestoreAdditionalProjectFallbackFolders="$HOME/.nuget/packages"
```

- 这样只把缺失的包下载到工作区，已有包从系统缓存只读复用。
- `/tmp` 在每次命令之间会清空，脚本里不要跨命令依赖 `/tmp` 文件；用 `$PWD/.run/tmp`。
- `dconf will not work properly`、`MESA/ZINK/EGL` 警告是沙箱无写权限/无 GPU 导致，普通桌面会话不会出现（或表现不同）。

## 8. 常见错误

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| `CS1617: /langversion 的选项"15.0"无效` | SDK 10 | 装 SDK 11，见第 2 节 |
| `NETSDK1127: 未安装目标包 Microsoft.NETCore.App` | 用 SDK 10 还原后又用 SDK 11 构建（或反之） | 用同一个 SDK 重新 `restore` |
| `Read-only file system : '/home/<user>/.nuget/...'` | 沙箱只读 | 见第 7 节 |
| 测试进程崩溃在 `Paths` 静态构造 | `~/.config` 不可写 | `XDG_CONFIG_HOME` 指到可写目录 |
| `GameFoldersTest...` 断言失败 | 设置了 `PCL_DATA_DIR` | 测试时不要设 |
| 窗口不出现、只有 `dconf`/MESA 警告 | 无显示会话/无 GPU | 在真实桌面会话运行；或 `PCL_FORCE_X11=1` |
| `Dependency injection failed: rpc-property@128` | 上游 RPC 属性注册警告 | 目前不影响启动，属已知警告（见 `docs/DECISIONS.md`） |

## 9. CI

- `.github/workflows/build-test.yml`：push `dev` / PR 时构建 win-x64、win-arm64、linux-x64、linux-arm64、osx-x64、osx-arm64，并在 Ubuntu/Windows/macOS 跑测试；使用 SDK `11.0.x`。
- `.github/workflows/release-publish.yml`、`mirrorchyan_*.yml` 是上游发布流程，我们暂不使用；按 `CLAUDE.md`，新增 CI 用新文件，不改上游工作流。
- 本地操作前先看 `git status`，`bin/`、`obj/`、`artifacts/` 都在 `.gitignore` 里；`.dotnet/`、`.dotnet-home/`、`.local-nuget/`、`.run/` 只在本机忽略（`.git/info/exclude`），不要提交。
