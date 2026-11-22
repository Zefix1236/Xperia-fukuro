# logo（品牌控件）

分支 `dsh-2f00facb-d3e6`（harness worktree）。最后更新：2026-11-22。

## 这轮的目标

另一段对话负责 logo 的视觉设计，本对话负责把它接进软件。这一轮完成"自绘控件 + 演示窗口"，
标题栏集成等用户定播放行为后再做。

## 已实现

| 文件 | 内容 |
| --- | --- |
| `PCL.Launcher/UI/Controls/Branding/XperiaFukuroLogo.cs` | 自绘控件，继承 `Control`；四个 StyledProperty：`FontSize` / `FontFamily` / `Foreground` / `Accent`，外加 `FollowPointer`；`FontSize`、`FontFamily` 变化触发重新排版 |
| `PCL.Launcher/UI/Controls/Branding/LogoLayout.cs` | 九个字形的静态排版、X 拉伸、ふ/ろ 压缩、自绘 V 形几何与收起目标 |
| `PCL.Launcher/UI/Controls/Branding/LogoPlayhead.cs` | 播放头：`frame` 0..18、30 单位/秒、cubic-bezier(0.77, 0, 0.175, 1) 二分 24 次 |
| `PCL.Launcher/Assets/Fonts/ZenMaruGothic-Medium.subset.ttf` | Zen Maru Gothic Medium 的 9 字形子集（3 KB，OFL-1.1），同目录 `ZenMaruGothic-OFL.txt` |
| `LogoDemo/` | 独立演示项目（不在 `.slnx` 里），链接控件源码、内嵌同一份字体；`--selftest` 自测、`--frames` 渲染静帧、`--sequence` 输出逐帧 PNG |

### 2026-11-22 第二轮：く 改成自绘 V 形 + X 拉伸 + ふ/ろ 压缩

播放头模型、缓动曲线、淡出时机、触摸/悬停逻辑都没动，只改排版与绘制。

常量（`LogoLayout` 里的 `const`）：

| 名称 | 值 | 含义 |
| --- | --- | --- |
| `StrokeEm` | 0.090 | く 的线宽 = 主字号 × 此值（S） |
| `NestGapRatio` | 1.0 | 收起态 く 与 X 的笔画间隙 = S × 此值 |
| `ChevronHeightRatio` | 1.0 | く 的高度比例 |
| `XStretch` | 1.21 | X（及由它推算的 く）横向拉伸 |
| `KanaSqueeze` | 0.94 | ふ / ろ 横向压缩 |
| `GapFuKu` | 0.145 | ふ 与 く 的墨迹间隙（× 主字号） |
| `GapKuRo` | 0.145 | く 与 ろ 的墨迹间隙（× 主字号） |
| `KanaScale` | 0.82 | 假名字号比例（沿用） |
| `KanaLiftEm` | 0.07 | 假名上提（× 假名字号，沿用） |
| `LatinTrackingEm` | 0.05 | 英文字距（× 主字号，沿用） |
| `LatinToKanaTrackingEm` | 0.14 | A → ふ 间距（× 假名字号，按 advance，沿用） |
| `CollapseInsideX` | 0.33 | 七个字形收起后叠放位置（× X 拉伸后的 advance） |
| `ChevronTipOffsetRatio` | 0.0117 | く 相对 X 墨迹中心的上移量（× X 墨迹高）：交叉点在框中心上方 0.0234 倍，取一半折中「臂间隙均匀」与「上下留白相等」 |

排版要点：

- X 用 `ScaleX(1.21)`，缩放原点在它自己的排版原点；P 的原点 = `X.advance × 1.21 + 0.05 × 主字号`。
- ふ / ろ 用 `ScaleX(0.94)`，缩放原点在各自排版原点；字距按墨迹边缘算。
- く 是自绘折线：从 X 缩放后的墨迹框推 `W = 宽 − S`、`H = 高 − S`，`Hc = H`、`Wc = W/2`，
  `w = Wc + S`、`h = Hc + S`；三点 `(S/2+Wc, S/2) → (S/2, h/2) → (S/2+Wc, S/2+Hc)`，
  `StreamGeometry` + 圆头圆角 `Pen`（粗细 S），不填充、用 `Accent`；垂直中心对齐 X 墨迹中心。
- 收起：七个字形叠到 `pile = X 原点 + X.advance × 1.21 × 0.33`；く 的顶点从
  `左边缘 + S/2` 移到 `X 墨迹中心 + 2S×NestGapRatio/cosα`，`tanα = W/H`。
- 垂直：X 的两条斜线交叉点比墨迹框中心高 0.0234 × 墨迹高（圆角字形不对称）。对齐交叉点时
  臂间隙均匀（实测 50/50、47/47 @1.5x）但上下留白 0.23/1.81 DIP；对齐框中心时留白相等但
  下部间隙偏紧（差约 0.59 DIP，S 的 14%）。这里取一半（0.0117），两边偏差各半。

### 行为（未变部分）

- 鼠标进入 / 离开切换目标；触摸、笔用 `PointerReleased` 切换（只对非鼠标指针生效）。
- `Render` 先画透明矩形，整块控件区域参与命中测试。
- 宽度固定为展开态宽度，内容靠左；`ArrangeOverride` 里用 `DesiredSize` 顶住。
- 内容四周 3px 内边距，尺寸只由展开态排版决定。
- rAF 逐帧推进，到目标即停止；反向只改方向不改当前帧；单帧最多推进 0.1 秒。

## 自测（本机 Linux x64，SDK 11 / Avalonia 12.1.6）

```
./LogoDemo/bin/Debug/net10.0/LogoDemo --selftest
```

11 项播放头/缓动检查 + 24 项排版检查全部 OK。排版相关的关键断言：

- X 墨迹宽 = 原始宽 × 1.21，墨迹高不变；ふ / ろ 墨迹宽 = 原始宽 × 0.94。
- P 原点 − X 原点 = 0.05 × 主字号 + (1.21 − 1) × X advance。
- ふ→く、く→ろ 的墨迹间隙都等于 0.145 × 主字号。
- く 的尺寸 w = Wc + S、h = Hc + S；两条臂斜率 = X 右半边斜率（平行）；尖角 = X 斜线交叉点。
- 收起顶点 = X 墨迹中心 + 2S/cosα；换算成垂直间距正好是 S × NestGapRatio。
- 七个字形收起后原点都等于 pile；收起时墨迹左边缘都不越过 X 的墨迹左边缘。
- 控件尺寸 = 排版尺寸 + 6（整像素取整），收起后不变。

排版实测（48px）：X 墨迹 `0, 0, 29.845, 33.598`，く 墨迹 `224.183, 0, 17.083, 33.598`，
排版尺寸 `272.20 × 33.60`。く 的 `Geometry.Bounds` 是 `(S/2, S/2, Wc, Hc)`（不含描边），
`GetRenderBounds(pen)` 是 `(0, 0, 17.083, 33.598)`，与显式墨迹完全一致。

渲染检查（`--frames`）：96px 的 5 张关键帧 + 24px 标题栏两张 + **240px 的收起 / 展开大图**，
收起态墨迹右边缘 315px、展开态 2046px（画布 2051×261 @1.5x），与排版算出的数值吻合。
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
- 标题栏背景是流动渐变，X 与 く 统一用白色（`Accent` 也留空走默认白色；早先试过 `#CFE8FF`，实测发灰、像半透明，已弃用）。
- 如果以后要"启动时播一次再收起"或循环播放，在这里加：`TitleBar` 里 `Loaded` 后调 `LogoBrand.Expand()`
  延时再 `Collapse()`；控件侧不用改。
