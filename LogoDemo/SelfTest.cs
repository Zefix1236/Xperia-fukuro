using Avalonia;
using Avalonia.Media;
using PCL.UI.Controls.Branding;

namespace LogoDemo;

/// <summary>播放头与缓动的自测，不依赖 Avalonia 平台。</summary>
internal static class SelfTest
{
    private const double Tick = 1d / 120d;
    private const double UnitsPerSecond = LogoPlayhead.UnitsPerSecond;

    private static int _failures;

    public static int Failures => _failures;

    public static int RunMath()
    {
        Console.WriteLine("== 播放头 / 缓动自测 ==");
        CheckEase();
        CheckDuration();
        CheckReversal();
        CheckFastToggling();
        return _failures;
    }

    public static int RunLayout()
    {
        Console.WriteLine("== 排版自测 ==");
        CheckLayout();
        return _failures;
    }

    private static void Check(string name, bool condition, string? detail = null)
    {
        if (condition)
        {
            Console.WriteLine($"  [OK]   {name}");
            return;
        }

        _failures++;
        Console.WriteLine($"  [FAIL] {name}{(detail is null ? string.Empty : " · " + detail)}");
    }

    private static void CheckEase()
    {
        Check("Ease(0) = 0", LogoPlayhead.Ease(0d) == 0d);
        Check("Ease(1) = 1", LogoPlayhead.Ease(1d) == 1d);

        var monotonic = true;
        var previous = -1d;
        for (var i = 0; i <= 200; i++)
        {
            var value = LogoPlayhead.Ease(i / 200d);
            if (value < previous - 1e-12d) monotonic = false;
            previous = value;
        }

        Check("Ease 在 0..1 上单调递增", monotonic);
        var middle = LogoPlayhead.Ease(0.5d);
        Check("Ease(0.5) 落在 (0,1) 内", middle is > 0d and < 1d, $"Ease(0.5) = {middle:F4}");
    }

    private static void CheckDuration()
    {
        var playhead = new LogoPlayhead();
        playhead.SetTarget(LogoPlayhead.ExpandedFrame);

        var elapsed = 0d;
        var guard = 0;
        while (playhead.IsMoving && guard++ < 100_000)
        {
            playhead.Advance(Tick);
            elapsed += Tick;
        }

        Check("18 → 0 用时 0.6 秒（±1 tick）", Math.Abs(elapsed - 0.6d) <= Tick + 1e-9d, $"实测 {elapsed:F4}s");
    }

    private static void CheckReversal()
    {
        var playhead = new LogoPlayhead();
        playhead.SetTarget(LogoPlayhead.ExpandedFrame);
        for (var i = 0; i < 60; i++) playhead.Advance(Tick);

        var beforeFlip = playhead.Frame;
        playhead.SetTarget(LogoPlayhead.CollapsedFrame);
        Check("反向瞬间帧号不变", Math.Abs(playhead.Frame - beforeFlip) < 1e-12d,
            $"翻转前 {beforeFlip:F4}，翻转后 {playhead.Frame:F4}");

        var maxStep = 0d;
        var previous = playhead.Frame;
        var guard = 0;
        while (playhead.IsMoving && guard++ < 100_000)
        {
            playhead.Advance(Tick);
            maxStep = Math.Max(maxStep, Math.Abs(playhead.Frame - previous));
            previous = playhead.Frame;
        }

        Check("反向过程单步位移 ≤ 30×dt", maxStep <= UnitsPerSecond * Tick + 1e-9d, $"最大 {maxStep:F6}");
    }

    private static void CheckFastToggling()
    {
        // 模拟鼠标 10 秒内以 20–80 ms 的随机间隔来回进出，检查帧号连续、无越界。
        var random = new Random(20261122);
        var playhead = new LogoPlayhead();
        const int steps = 2400;
        var elapsed = 0d;
        var nextFlip = 0d;
        var expanded = false;
        var previous = playhead.Frame;
        var maxStep = 0d;
        var minFrame = double.MaxValue;
        var maxFrame = double.MinValue;
        var stepOk = true;
        var progressOk = true;

        for (var i = 0; i < steps; i++)
        {
            if (elapsed >= nextFlip)
            {
                expanded = !expanded;
                playhead.SetTarget(expanded ? LogoPlayhead.ExpandedFrame : LogoPlayhead.CollapsedFrame);
                nextFlip = elapsed + 0.02d + random.NextDouble() * 0.06d;
            }

            playhead.Advance(Tick);
            elapsed += Tick;

            var step = Math.Abs(playhead.Frame - previous);
            previous = playhead.Frame;
            maxStep = Math.Max(maxStep, step);
            minFrame = Math.Min(minFrame, playhead.Frame);
            maxFrame = Math.Max(maxFrame, playhead.Frame);

            if (step > UnitsPerSecond * Tick + 1e-9d) stepOk = false;

            var pe = playhead.PositionProgress;
            var po = playhead.OpacityProgress;
            if (double.IsNaN(pe) || double.IsNaN(po) || pe is < 0d or > 1d || po is < 0d or > 1d)
                progressOk = false;
        }

        Check("快速来回：帧号始终在 0..18", minFrame >= 0d && maxFrame <= 18d,
            $"实际 {minFrame:F3}..{maxFrame:F3}，最大单步 {maxStep:F6}");
        Check("快速来回：单步位移不超过 30×dt", stepOk);
        Check("快速来回：pe / po 始终有限且在 0..1", progressOk);
        Check("快速来回：确实在两个方向上都跑过", minFrame < 9d && maxFrame > 9d);
    }

    private static void CheckLayout()
    {
        const double main = 48d;
        var control = new XperiaFukuroLogo { FontSize = main };
        var layout = control.CurrentLayout;
        Check("排版生成成功", layout is not null);
        if (layout is null) return;

        var stroke = layout.StrokeThickness;
        var anchor = layout.Anchor;
        var chevron = layout.Chevron;
        var fu = layout.FadingGlyphs.First(glyph => glyph.Text == "ふ");
        var ro = layout.FadingGlyphs.First(glyph => glyph.Text == "ろ");
        var p = layout.FadingGlyphs.First(glyph => glyph.Text == "P");

        Check("九个字形 = X + く（自绘）+ 七个淡出字形",
            anchor.Text == "X" && chevron.Text == "く" && layout.FadingGlyphs.Count == 7,
            $"anchor {anchor.Text}，chevron {chevron.Text}，fading {layout.FadingGlyphs.Count}");
        Check("宽高为正", layout.Width > 0d && layout.Height > 0d,
            $"{layout.Width:F2} × {layout.Height:F2}");

        // ---- 拉伸 / 压缩 ----
        Check("X 墨迹宽 = 原始宽 × XStretch",
            Math.Abs(anchor.InkBounds.Width - anchor.RawInk.Width * LogoLayout.XStretch) < 1e-9d,
            $"{anchor.InkBounds.Width:F4} vs {anchor.RawInk.Width * LogoLayout.XStretch:F4}");
        Check("X 墨迹高不变",
            Math.Abs(anchor.InkBounds.Height - anchor.RawInk.Height) < 1e-9d);
        Check("ふ 墨迹宽 = 原始宽 × KanaSqueeze",
            Math.Abs(fu.InkBounds.Width - fu.RawInk.Width * LogoLayout.KanaSqueeze) < 1e-9d);
        Check("ろ 墨迹宽 = 原始宽 × KanaSqueeze",
            Math.Abs(ro.InkBounds.Width - ro.RawInk.Width * LogoLayout.KanaSqueeze) < 1e-9d);

        // ---- 字距 ----
        var expectedP = layout.XAdvance * LogoLayout.XStretch + LogoLayout.LatinTrackingEm * main;
        Check("P 原点 − X 原点 = 0.05×主字号 + (XStretch−1)×X advance",
            Math.Abs((p.X - anchor.X) - expectedP) < 1e-9d,
            $"{p.X - anchor.X:F4} vs {expectedP:F4}");
        Check("ふ → く 墨迹间隙 = GapFuKu × 主字号",
            Math.Abs((chevron.InkBounds.Left - fu.InkBounds.Right) - LogoLayout.GapFuKu * main) < 1e-9d,
            $"{chevron.InkBounds.Left - fu.InkBounds.Right:F4}");
        Check("く → ろ 墨迹间隙 = GapKuRo × 主字号",
            Math.Abs((ro.InkBounds.Left - chevron.InkBounds.Right) - LogoLayout.GapKuRo * main) < 1e-9d,
            $"{ro.InkBounds.Left - chevron.InkBounds.Right:F4}");

        // ---- 自绘 く 的几何 ----
        var centerWidth = anchor.InkBounds.Width - stroke;
        var centerHeight = anchor.InkBounds.Height - stroke;
        var armWidth = centerWidth / 2d * LogoLayout.ChevronHeightRatio;
        var armHeight = centerHeight * LogoLayout.ChevronHeightRatio;
        Check("く 的整体尺寸 w = Wc + S",
            Math.Abs(layout.ChevronWidth - (armWidth + stroke)) < 1e-9d,
            $"w {layout.ChevronWidth:F4} vs {armWidth + stroke:F4}");
        Check("く 的整体尺寸 h = Hc + S",
            Math.Abs(layout.ChevronHeight - (armHeight + stroke)) < 1e-9d,
            $"h {layout.ChevronHeight:F4} vs {armHeight + stroke:F4}");
        var chevronPen = new Pen(Brushes.White, stroke, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        Console.WriteLine($"  く Geometry.Bounds = {chevron.Geometry.Bounds}");
        Console.WriteLine($"  く GetRenderBounds = {chevron.Geometry.GetRenderBounds(chevronPen)}");
        Console.WriteLine($"  く 显式墨迹（本地）= (0, 0, {layout.ChevronWidth:F4}, {layout.ChevronHeight:F4})");
        Check("く 的墨迹与自绘结果一致",
            Math.Abs(chevron.InkBounds.Width - layout.ChevronWidth) < 1e-6d &&
            Math.Abs(chevron.InkBounds.Height - layout.ChevronHeight) < 1e-6d);
        Check("く 的两条臂与 X 右半边斜线斜率一致（平行）",
            Math.Abs(armHeight / 2d / armWidth - centerHeight / centerWidth) < 1e-9d,
            $"臂 {armHeight / 2d / armWidth:F4}，X {centerHeight / centerWidth:F4}");
        Check("く 的垂直中心 = X 墨迹的垂直中心",
            Math.Abs(chevron.InkBounds.Center.Y - anchor.InkBounds.Center.Y) < 1e-9d,
            $"{chevron.InkBounds.Center.Y:F4} vs {anchor.InkBounds.Center.Y:F4}");

        // ---- 收起目标 ----
        var cosAlpha = 1d / Math.Sqrt(1d + layout.ChevronTanAlpha * layout.ChevronTanAlpha);
        var expectedCollapsedVertex = anchor.InkBounds.Center.X + 2d * stroke * LogoLayout.NestGapRatio / cosAlpha;
        Check("收起顶点 = X 墨迹中心 + 2S×NestGapRatio/cosα",
            Math.Abs(layout.ChevronCollapsedVertexX - expectedCollapsedVertex) < 1e-9d,
            $"{layout.ChevronCollapsedVertexX:F4} vs {expectedCollapsedVertex:F4}");
        Check("收起时 く 与 X 的垂直间距 = S × NestGapRatio",
            Math.Abs((layout.ChevronCollapsedVertexX - anchor.InkBounds.Center.X) * cosAlpha
                     - 2d * stroke * LogoLayout.NestGapRatio) < 1e-9d);
        Check("展开顶点 = く 左边缘 + S/2",
            Math.Abs(layout.ChevronExpandedVertexX - (chevron.InkBounds.Left + stroke / 2d)) < 1e-9d);

        var pileOk = layout.FadingGlyphs.All(glyph => Math.Abs(glyph.X + glyph.CollapseDx - layout.PileX) < 1e-9d);
        Check("七个字形收起后原点都等于 pile", pileOk, $"pile {layout.PileX:F4}");
        Check("pile = X 原点 + X advance × XStretch × 0.33",
            Math.Abs(layout.PileX - (anchor.X + layout.XAdvance * LogoLayout.XStretch * LogoLayout.CollapseInsideX)) < 1e-9d);
        Check("淡出字形收起时不越过 X 左边缘",
            layout.FadingGlyphs.All(glyph =>
                glyph.X + glyph.CollapseDx + glyph.ScaleX * glyph.RawInk.Left >= anchor.InkBounds.Left - 1e-9d));

        // ---- 尺寸与内边距 ----
        control.Measure(Size.Infinity);
        var expandedSize = control.DesiredSize;
        control.SeekFrame(LogoPlayhead.CollapsedFrame);
        control.InvalidateMeasure();
        control.Measure(Size.Infinity);
        var collapsedSize = control.DesiredSize;

        var padding = XperiaFukuroLogo.ContentPadding * 2d;
        Check("控件尺寸 = 排版尺寸 + 四周内边距（整像素取整）",
            Math.Abs(expandedSize.Width - (layout.Width + padding)) <= 1d &&
            Math.Abs(expandedSize.Height - (layout.Height + padding)) <= 1d,
            $"{expandedSize.Width:F2} × {expandedSize.Height:F2}，排版 {layout.Width:F2} × {layout.Height:F2}");
        Check("收起后控件尺寸不变",
            Math.Abs(expandedSize.Width - collapsedSize.Width) < 1e-9d &&
            Math.Abs(expandedSize.Height - collapsedSize.Height) < 1e-9d);

        var typeface = new Typeface(XperiaFukuroLogo.ZenMaruGothic, FontStyle.Normal, FontWeight.Medium);
        var resolved = FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface);
        Check("字体解析到 Zen Maru Gothic", resolved, resolved ? glyphTypeface?.FamilyName ?? "未知" : "解析失败");

        Console.WriteLine($"  常量：S={stroke / main:F4}em，NestGapRatio={LogoLayout.NestGapRatio}，" +
                          $"ChevronHeightRatio={LogoLayout.ChevronHeightRatio}，XStretch={LogoLayout.XStretch}，" +
                          $"KanaSqueeze={LogoLayout.KanaSqueeze}，GapFuKu={LogoLayout.GapFuKu}，GapKuRo={LogoLayout.GapKuRo}");
        Console.WriteLine($"  X 墨迹：{anchor.InkBounds}");
        Console.WriteLine($"  く 墨迹（展开）：{chevron.InkBounds}");
        Console.WriteLine($"  排版尺寸：{layout.Width:F2} × {layout.Height:F2}");
    }
}
