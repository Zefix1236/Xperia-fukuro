# logo（品牌控件）

分支 `dsh-2f00facb-d3e6`（harness worktree）。最后更新：2026-11-22。

## 这轮的目标

另一段对话负责 logo 的视觉设计，本对话负责把它接进软件。这一轮完成"自绘控件 + 演示窗口"，
标题栏集成等用户定播放行为后再做。

## 已实现

| 文件 | 内容 |
| --- | --- |
| `PCL.Launcher/UI/Controls/Branding/XperiaFukuroLogo.cs` | 自绘控件，继承 `Control`；四个 StyledProperty：`FontSize` / `FontFamily` / `Foreground` / `Accent`；`FontSize`、`FontFamily` 变化触发重新排版 |
| `PCL.Launcher/UI/Controls/Branding/LogoLayout.cs` | 九个字形的静态排版与收起目标（按 `Geometry.Bounds` 算） |
| `PCL.Launcher/UI/Controls/Branding/LogoPlayhead.cs` | 播放头：`frame` 0..18、30 单位/秒、cubic-bezier(0.77, 0, 0.175, 1) 二分 24 次 |
| `PCL.Launcher/Assets/Fonts/ZenMaruGothic-Medium.ttf` | Zen Maru Gothic Medium（3.8 MB，OFL-1.1），同目录 `ZenMaruGothic-OFL.txt` |
| `LogoDemo/` | 独立演示项目（不在 `.slnx` 里），链接控件源码、内嵌同一份字体；`--selftest` 自测、`--frames=<dir>` 渲染静帧 |

行为：

- 展开态九个字形；收起后只剩 X 与 く（く 用 `Accent`）。
- 其余七个字形先绘制（下层），水平平移到 X 内部 33% 处并淡出；X 不动；く 只水平平移到 X 右侧。
- 鼠标进入 / 离开切换目标；触摸、笔用 `PointerReleased` 切换（只对非鼠标指针生效）。
- `Render` 先画透明矩形，整块控件区域参与命中测试。
- 宽度固定为展开态宽度，内容靠左；`ArrangeOverride` 里用 `DesiredSize` 顶住，不被父容器拉伸。
- rAF 逐帧推进，到目标即停止；反向只改方向不改当前帧；单帧最多推进 0.1 秒（掉帧保护）。

## 自测（本机 Linux x64，SDK 11 / Avalonia 12.1.6）

```
./LogoDemo/bin/Debug/net10.0/LogoDemo --selftest
```

11 项播放头/缓动检查 + 7 项排版检查全部 OK：

- `18 → 0` 用时 0.6 秒（±1 tick）。
- 反向瞬间帧号不变，单步位移 ≤ 30×dt。
- 模拟 10 秒内每 20–80 ms 随机来回进出：帧号始终 0..18、单步不超限、pe/po 有限且在 0..1。
- `く` 收起目标 = X 墨迹右边缘 − 0.08em；七个字形收起后左边缘都等于 X 左边缘 + 33%×X 墨迹宽。
- 字体解析到 `Zen Maru Gothic`（nameID 16；nameID 1 是 "Zen Maru Gothic Medium"，字重 500）。

排版实测（48px）：X 墨迹 `0, 0.47, 24.67, 33.60`，く 墨迹 `218.69, 0, 234.58, 33.74`，
排版尺寸 `277.46 × 34.06`。按此比例，96px 时收起态右边缘 73.4 DIP，与渲染出的 147px（@2x）吻合。

渲染检查（`--frames`，96px）：frame 0 / 4.5 / 9 / 13.5 / 18 都能画出内容，9 之后只剩 X 与 く，
18 帧右边缘 73.5 DIP，说明淡出与位移都对。

## 未完成 / 待确认

- 没有接进 `TitleBar.axaml`：等用户定"启动播一次 / 悬停重播 / 循环"之后再改（改上游文件要加 `// FORK:` 标记并登记 `docs/FORK-PATCHES.md`）。
- 规格里几处含糊（0.33 的基准、"30 帧/秒"的含义、く 的垂直位置等），见交给设计对话的问题清单。
- 字体完整包 3.8 MB；如果嫌大可以子集化成 9 个字形（约几十 KB），需要用户决定。
- 触摸切换的初始态是收起，第一次点按展开。
- 没有键盘焦点与 AutomationPeer。

## 运行演示

```bash
export DOTNET_ROOT=~/Projects/Xperia-fukuro/.dotnet
export DOTNET_CLI_HOME=~/Projects/Xperia-fukuro/.dotnet-home
export NUGET_PACKAGES=~/Projects/Xperia-fukuro/.local-nuget/packages
"$DOTNET_ROOT/dotnet" run --project LogoDemo/LogoDemo.csproj
```

自测与静帧：

```bash
"$DOTNET_ROOT/dotnet" run --project LogoDemo/LogoDemo.csproj -- --selftest
"$DOTNET_ROOT/dotnet" run --project LogoDemo/LogoDemo.csproj -- --frames=LogoDemo/frames
```

## 交接

- 标题栏集成时：`TitleBar.axaml` 里 type == 1 的分支把 `ShapeTitleLogo` + `CELogo` 换成
  `<branding:XperiaFukuroLogo FontSize="24" ... />`，并加 `xmlns:branding`；`RefreshTitleAppearance`
  里把 `CELogo.IsVisible` 改成 `false`。改动记进 `docs/FORK-PATCHES.md`。
- 标题栏背景是流动渐变，logo 要用浅色；`Accent` 建议给一个偏亮的蓝。
