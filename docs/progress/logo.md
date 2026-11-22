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
| `PCL.Launcher/Assets/Fonts/ZenMaruGothic-Medium.subset.ttf` | Zen Maru Gothic Medium 的 9 字形子集（3 KB，OFL-1.1），同目录 `ZenMaruGothic-OFL.txt`；完整字体 3.8 MB，子集化命令记在 `docs/LICENSE-NOTES.md` |
| `LogoDemo/` | 独立演示项目（不在 `.slnx` 里），链接控件源码、内嵌同一份字体；`--selftest` 自测、`--frames=<dir>` 渲染静帧 |

行为：

- 展开态九个字形；收起后只剩 X 与 く（く 用 `Accent`）。
- 其余七个字形先绘制（下层），水平平移到 X 内部 33% 处并淡出；X 不动；く 只水平平移到 X 右侧。
- 鼠标进入 / 离开切换目标；触摸、笔用 `PointerReleased` 切换（只对非鼠标指针生效）。
- `Render` 先画透明矩形，整块控件区域参与命中测试。
- 宽度固定为展开态宽度，内容靠左；`ArrangeOverride` 里用 `DesiredSize` 顶住，不被父容器拉伸。
- 内容四周留 3px 内边距（`ContentPadding`），防止抗锯齿边缘被裁掉；尺寸只由展开态排版决定，收起 / 展开过程中不变。
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
排版尺寸 `277.46 × 34.06`，控件 `DesiredSize` `284 × 41`（排版 + 6，向上取整到整像素）。
按此比例，96px 时收起态内容右边缘 73.4 DIP；加内边距后静帧墨迹从 (6,6) 开始，与 3px @2x 吻合。

渲染检查（`--frames`，96px）：frame 0 / 4.5 / 9 / 13.5 / 18 都能画出内容，9 之后只剩 X 与 く，
18 帧墨迹右边缘 153px / 2 = 76.5 DIP（73.4 + 3 内边距），说明淡出、位移和内边距都对；
五帧墨迹左上角都从 6px 开始，抗锯齿边缘没有贴到控件边界。

## 未完成 / 待确认

- 已经接进 `TitleBar.axaml`：默认标题类型（type 1）显示品牌字标，PCL 字标与 CE 徽标让位；
  **悬停触发区域是整条标题栏**（`PanTitle` 的 `PointerEntered` / `PointerExited` 驱动），控件自身设了
  `FollowPointer="False"`、`IsHitTestVisible="False"`，不影响标题栏拖动。改动已登记 `docs/FORK-PATCHES.md`。
- 播放行为仍是"悬停展开、移开收起"，没有做启动时播一次或循环；要加时改 `TitleBar` 里的两处事件即可。
- 规格里几处含糊（0.33 的基准、"30 帧/秒"的含义、く 的垂直位置等），见交给设计对话的问题清单。
- 字体已按用户要求子集化到 9 个字形（3 KB）；以后加字必须重新生成，否则缺字回退系统字体。
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

- 标题栏集成已完成（见上）。`LogoBrand` 的 `Margin` 是 16（控件内边距 3 + 外边距 16 = 视觉 19，
  与原来的 PCL 字标对齐）。要调起始位置只改这个 Margin。
- 标题栏背景是流动渐变，logo 用白色 + 浅蓝 `Accent="#CFE8FF"`。
- 如果以后要"启动时播一次再收起"或循环播放，在这里加：`TitleBar` 里 `Loaded` 后调 `LogoBrand.Expand()`
  延时再 `Collapse()`；控件侧不用改。
