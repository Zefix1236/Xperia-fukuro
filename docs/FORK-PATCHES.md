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

## 品牌字标（logo 动画，2026-11-22）

| 文件 | 改动 | 原因 | 标记位置 |
| --- | --- | --- | --- |
| `PCL.Launcher/Views/Shell/TitleBar.axaml` | 增加 `xmlns:branding`；在 `PanTitleLeft` 的 logo 网格里加 `LogoBrand`（`XperiaFukuroLogo`，24px，`FollowPointer="False"`，`IsHitTestVisible="False"`），默认隐藏 | 默认标题类型改用自家品牌字标；不参与命中测试，标题栏拖动不受影响 | `<!-- FORK: 默认标题类型用自绘品牌字标取代 PCL 字标与 CE 徽标；悬停由整条标题栏驱动 -->` |
| `PCL.Launcher/Views/Shell/TitleBar.axaml.cs` | 构造函数里给 `PanTitle` 挂 `PointerEntered` / `PointerExited`，鼠标进出整条标题栏时调用 `LogoBrand.Expand()` / `Collapse()`；`RefreshTitleAppearance` 里把 `ShapeTitleLogo` 恒隐藏、`LogoBrand` 仅 type == 1 显示、`CELogo` 在 type 0/1 时隐藏 | 悬停触发区域是整条标题栏；PCL 字标与 CE 徽标让位给品牌字标 | `// FORK: 悬停区域是整条标题栏…`、`// FORK: 整条标题栏悬停时展开品牌字标`、`// FORK: 默认标题类型改用自绘品牌字标…`、`// FORK: 默认标题类型的 CE 徽标也一并让位给品牌字标` |
