# logo（品牌控件）

分支 `dsh-2f00facb-d3e6`（harness worktree）。最后更新：2026-11-22。

## 这轮的目标

另一段对话负责 logo 的视觉设计，本对话负责把它接进软件。这一轮完成"自绘控件 + 演示窗口"，
标题栏集成等用户定播放行为后再做。

## 已实现

| 文件 | 内容 |
| --- | --- |
| `PCL.Launcher/UI/Controls/Branding/XperiaFukuroLogo.cs` | 自绘控件，继承 `Control`；四个 StyledProperty：`FontSize` / `FontFamily` / `Foreground` / `Accent`，外加 `FollowPointer`；`FontSize`、`FontFamily` 变化触发重新排版 |
| `PCL.Launcher/UI/Controls/Branding/LogoLayout.cs` | 九个字形的静态排版、X 拉伸、ふ/ろ 压缩、`<` 的骨架几何与收起目标 |
| `PCL.Launcher/UI/Controls/Branding/XGlyphSkeleton.cs` | X 右半边骨架测量（参考字号 1000、FillContains 扫描、按字体族缓存） |
| `PCL.Launcher/UI/Controls/Branding/LogoPlayhead.cs` | 播放头：`frame` 0..18、30 单位/秒、cubic-bezier(0.77, 0, 0.175, 1) 二分 24 次 |
| `PCL.Launcher/Assets/Fonts/ZenMaruGothic-Medium.subset.ttf` | Zen Maru Gothic Medium 的 9 字形子集（3 KB，OFL-1.1），同目录 `ZenMaruGothic-OFL.txt` |
| `LogoDemo/` | 独立演示项目（不在 `.slnx` 里），链接控件源码、内嵌同一份字体；`--selftest` 自测、`--frames` 渲染静帧、`--sequence` 输出逐帧 PNG |

### 2026-11-23 第三轮：く 改成 X 右半边骨架的平移拷贝

按另一段对话给出的方案重做。不再用"对称 V + 在交叉点与框中心之间二选一"，而是把 X 右半边的
两条中心线各自向外平移，得到一条**上臂短、下臂长**的 `<` —— 这正是 X 自身的结构，所以
"臂间隙均匀"和"上下留白相等"可以同时成立。

#### X 骨架测量（`XGlyphSkeleton`）

- 在字号 1000 下 `BuildGeometry`，取墨迹上下缘；在 0.20 / 0.33 / 0.67 / 0.80 高度处、
  从右往左 0.5 步长扫 `FillContains`，取最右一段墨迹的中心与横向宽（× `XStretch`），
  拟合两条中心线 `x = a + m·y`；0.26 / 0.74 高度做残差自检。- 交叉点 `yc = (a1 − a2) / (m2 − m1)`，
  笔画宽 `Sx = 平均(wh / sqrt(1 + m²))`。结果按字体族缓存，与字号无关。

实测（Zen Maru Gothic Medium，参考字号 1000）：

| 量 | 实测 | 说明 |
| --- | --- | --- |
| `m1`（右下臂） | 0.7580 | 拉伸后斜率 |
| `m2`（右上臂） | −0.7613 | 拉伸后斜率 |
| `Sx` | 100.81 | = 0.1008 em，比 `StrokeEm`(0.09) 粗 |
| 交叉点比例 | 0.4760 | (yc − top) / H，理论 (b−a)/(2(a+b)) ≈ 0.477 |
| 残差 | ≤ 0.02 × S | 上下各一条自检行 |

#### `<` 的几何

- `S = 0.09 × 字号`；`dperp = (Sx + S) / 2 + NestGapRatio × S`
- `d_i = dperp × sqrt(1 + m_i²)`（把中心线水平平移这么多，垂直间隙就正好是 `NestGapRatio × S`）
- 尖角：`yv = ((a1+d1) − (a2+d2)) / (m2 − m1)`，`xv = a2 + d2 + m2·yv`
- 上下端：`margin = (1 − ChevronHeightRatio)/2 × H`，`yUp = top + S/2 + margin`，
  `yLow = bottom − S/2 − margin`，x 由各自的线给出
- 三点 `(xUp,yUp) → (xv,yv) → (xLow,yLow)`，`Pen` 粗细 S、圆头圆角、不填充

#### 动画

收起态折线**不动**（它本来就嵌在 X 右半边）；展开态整体右移
`E = (ふ 墨迹右缘 + GapFuKu × 字号) − (折线左缘)`，每帧 `tx = E × (1 − pe)`，垂直不动。

#### 已知代价（非 bug）

两臂右端不齐：48px 下 `|xLow − xUp| = 1.16 DIP`（≈ 0.27S），因为 X 自身就是上窄下宽。
这是设计要求里的"嵌套同形"，不动它。

#### 验收（渲染实测）

| 字号 | 纵横留白 | 高度比 | 上臂斜率 | 下臂斜率 | 上臂间隙 | 下臂间隙 | 间隙差 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 24 | 3 / 3 px | 0.941 | −0.763 | +0.766 | 0.92S | 0.92S | 0.00S |
| 48 | 4 / 5 px | 0.933 | −0.767 | +0.763 | 0.94S | 0.92S | 0.02S |
| 240 | 9 / 9 px | 0.929 | −0.762 | +0.758 | 0.97S | 0.95S | 0.02S |

- 斜率构造值 −0.7613 / +0.7580，像素拟合差 ≤ 0.006；
- 间隙用阈值 alpha>8 量，抗锯齿会两边各吃掉约 0.5px；补偿 1px 后是 0.98–1.00 S；
- 高度比 0.93 ± 1px 取整；右端不齐 1.16 DIP ≤ 0.5S。

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
