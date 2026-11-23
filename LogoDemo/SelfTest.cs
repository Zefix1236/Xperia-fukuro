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

        var skeleton = layout.Skeleton;
        var stroke = layout.StrokeThickness;
        var anchor = layout.Anchor;
        var chevron = layout.Chevron;
        var fu = layout.FadingGlyphs.First(glyph => glyph.Text == "ふ");
        var ro = layout.FadingGlyphs.First(glyph => glyph.Text == "ろ");
        var xInk = anchor.InkBounds;
        var kInk = chevron.InkBounds;

        // ---- X 骨架 ----
        Check("X 骨架上臂斜率为负、下臂斜率为正",
            skeleton.SlopeUpper < 0d && skeleton.SlopeLower > 0d,
            $"m2 {skeleton.SlopeUpper:F4}，m1 {skeleton.SlopeLower:F4}");
        Check("X 骨架拟合残差 ≤ 0.02 × S（参考字号）",
            skeleton.ResidualUpper <= 0.02d * LogoLayout.StrokeEm * XGlyphSkeleton.ReferenceSize &&
            skeleton.ResidualLower <= 0.02d * LogoLayout.StrokeEm * XGlyphSkeleton.ReferenceSize,
            $"上 {skeleton.ResidualUpper:F3}，下 {skeleton.ResidualLower:F3}");

        var crossingRatio = (skeleton.CrossingY - skeleton.InkTop) / skeleton.Height;
        Check("交叉点在墨迹框上方（比例在 0.45~0.50）",
            crossingRatio is > 0.45d and < 0.50d, $"比例 {crossingRatio:F4}");
        Console.WriteLine($"  骨架：m1={skeleton.SlopeLower:F4}，m2={skeleton.SlopeUpper:F4}，" +
                          $"Sx={skeleton.StrokeWidth:F2}（参考字号），交叉点比例 {crossingRatio:F4}，" +
                          $"残差 上 {skeleton.ResidualUpper:F3} / 下 {skeleton.ResidualLower:F3}");

        // ---- く 的高度与留白 ----
        Check("く 高度 = ChevronHeightRatio × X 墨迹高",
            Math.Abs(kInk.Height - LogoLayout.ChevronHeightRatio * xInk.Height) < 1e-6d,
            $"{kInk.Height:F4} vs {LogoLayout.ChevronHeightRatio * xInk.Height:F4}");
        var topMargin = kInk.Top - xInk.Top;
        var bottomMargin = xInk.Bottom - kInk.Bottom;
        Check("上下留白差 ≤ 0.1 × X 墨迹高",
            Math.Abs(topMargin - bottomMargin) <= 0.1d * xInk.Height,
            $"上 {topMargin:F4}，下 {bottomMargin:F4}");
        Check("留白 = (1 - ChevronHeightRatio) / 2 × X 墨迹高",
            Math.Abs(topMargin - (1d - LogoLayout.ChevronHeightRatio) / 2d * xInk.Height) < 1e-6d,
            $"{topMargin:F4} vs {(1d - LogoLayout.ChevronHeightRatio) / 2d * xInk.Height:F4}");

        // ---- 臂斜率与 X 一致（平行）----
        var upperSlope = (layout.ChevronUpperEnd.X - layout.ChevronVertex.X)
                         / (layout.ChevronUpperEnd.Y - layout.ChevronVertex.Y);
        var lowerSlope = (layout.ChevronLowerEnd.X - layout.ChevronVertex.X)
                         / (layout.ChevronLowerEnd.Y - layout.ChevronVertex.Y);
        Check("上臂斜率 = X 上臂斜率", Math.Abs(upperSlope - skeleton.SlopeUpper) < 1e-9d,
            $"{upperSlope:F6} vs {skeleton.SlopeUpper:F6}");
        Check("下臂斜率 = X 下臂斜率", Math.Abs(lowerSlope - skeleton.SlopeLower) < 1e-9d,
            $"{lowerSlope:F6} vs {skeleton.SlopeLower:F6}");

        // ---- 中心线距离与沿臂的间隙 ----
        var expectedGap = (layout.XStrokeWidth + stroke) / 2d + LogoLayout.NestGapRatio * stroke;
        Check("中心线距离 = (Sx + S)/2 + NestGapRatio × S",
            Math.Abs(layout.CenterlineGap - expectedGap) < 1e-9d,
            $"{layout.CenterlineGap:F4} vs {expectedGap:F4}");

        static double PerpendicularGap(LogoLayout l, bool upper, double yLayout)
        {
            var sk = l.Skeleton;
            var k = l.FontSize / XGlyphSkeleton.ReferenceSize;
            var m = upper ? sk.SlopeUpper : sk.SlopeLower;
            var a = (upper ? sk.InterceptUpper : sk.InterceptLower) * k;
            var xAnchor = l.Anchor.X + a + m * (yLayout - l.Anchor.Y);
            var end = upper ? l.ChevronUpperEnd : l.ChevronLowerEnd;
            var xChevron = end.X - l.ChevronExpandedDx + m * (yLayout - end.Y);
            return (xChevron - xAnchor) / Math.Sqrt(1d + m * m);
        }

        var gapOk = true;
        var maxGapError = 0d;
        foreach (var upper in new[] { true, false })
        {
            var end = upper ? layout.ChevronUpperEnd : layout.ChevronLowerEnd;
            var from = end.Y + (layout.ChevronVertex.Y - end.Y) * 0.1d;
            var to = layout.ChevronVertex.Y + (end.Y - layout.ChevronVertex.Y) * 0.1d;
            for (var i = 0; i <= 4; i++)
            {
                var y = from + (to - from) * i / 4d;
                // PerpendicularGap 量的是两条中心线的距离，边缘间隙要再减掉两笔的半宽。
                var gap = PerpendicularGap(layout, upper, y) - (layout.XStrokeWidth + stroke) / 2d;
                var error = Math.Abs(gap - LogoLayout.NestGapRatio * stroke);
                maxGapError = Math.Max(maxGapError, error);
                if (error > 0.1d * stroke) gapOk = false;
            }
        }

        Check("两条臂到 X 的垂直间隙 = NestGapRatio × S（误差 ≤ 0.1S）", gapOk,
            $"最大误差 {maxGapError:F4}，S = {stroke:F3}");

        // ---- 排版与字距 ----
        Check("九个字形 = X + く（自绘）+ 七个淡出字形",
            anchor.Text == "X" && chevron.Text == "く" && layout.FadingGlyphs.Count == 7);
        Check("X 墨迹宽 = 原始宽 × XStretch",
            Math.Abs(xInk.Width - anchor.RawInk.Width * LogoLayout.XStretch) < 1e-9d);
        Check("ふ / ろ 墨迹宽 = 原始宽 × KanaSqueeze",
            Math.Abs(fu.InkBounds.Width - fu.RawInk.Width * LogoLayout.KanaSqueeze) < 1e-9d &&
            Math.Abs(ro.InkBounds.Width - ro.RawInk.Width * LogoLayout.KanaSqueeze) < 1e-9d);
        Check("ふ → く 墨迹间隙 = GapFuKu × 主字号",
            Math.Abs(chevron.InkBounds.Left - fu.InkBounds.Right - LogoLayout.GapFuKu * main) < 1e-9d,
            $"{chevron.InkBounds.Left - fu.InkBounds.Right:F4}");
        Check("く → ろ 墨迹间隙 = GapKuRo × 主字号",
            Math.Abs(ro.InkBounds.Left - chevron.InkBounds.Right - LogoLayout.GapKuRo * main) < 1e-9d,
            $"{ro.InkBounds.Left - chevron.InkBounds.Right:F4}");

        // ---- 收起 ----
        Check("收起位移 = -(展开位移)",
            Math.Abs(chevron.CollapseDx + layout.ChevronExpandedDx) < 1e-9d,
            $"E {layout.ChevronExpandedDx:F4}，dx {chevron.CollapseDx:F4}");
        Check("七个字形收起后原点都等于 pile",
            layout.FadingGlyphs.All(glyph => Math.Abs(glyph.X + glyph.CollapseDx - layout.PileX) < 1e-9d),
            $"pile {layout.PileX:F4}");
        Check("淡出字形收起时不越过 X 左边缘",
            layout.FadingGlyphs.All(glyph =>
                glyph.X + glyph.CollapseDx + glyph.ScaleX * glyph.RawInk.Left >= xInk.Left - 1e-9d));
        Check("右端不齐 |xLow - xUp| ≤ 0.5 × S（仅记录）",
            Math.Abs(layout.ChevronLowerEnd.X - layout.ChevronUpperEnd.X) <= 0.5d * stroke,
            $"{Math.Abs(layout.ChevronLowerEnd.X - layout.ChevronUpperEnd.X):F4}，S = {stroke:F3}");

        // ---- 多字号稳定性 ----
        var sweepOk = true;
        var sweepDetail = new List<string>();
        foreach (var size in new double[] { 24d, 32d, 48d, 64d, 96d, 128d, 180d, 240d })
        {
            var l = LogoLayout.Build(XperiaFukuroLogo.ZenMaruGothic, size);
            var xi = l.Anchor.InkBounds;
            var ki = l.Chevron.InkBounds;
            var heightRatio = ki.Height / xi.Height;
            var marginRatio = (ki.Top - xi.Top) / xi.Height;
            var upperGap = PerpendicularGap(l, true, (l.ChevronUpperEnd.Y + l.ChevronVertex.Y) / 2d);
            var lowerGap = PerpendicularGap(l, false, (l.ChevronLowerEnd.Y + l.ChevronVertex.Y) / 2d);
            var edgeGap = (upperGap + lowerGap) / 2d - (l.XStrokeWidth + l.StrokeThickness) / 2d;
            var gapRatio = edgeGap / l.StrokeThickness;
            if (Math.Abs(heightRatio - LogoLayout.ChevronHeightRatio) > 1e-6d ||
                Math.Abs(marginRatio - (1d - LogoLayout.ChevronHeightRatio) / 2d) > 1e-6d ||
                Math.Abs(gapRatio - LogoLayout.NestGapRatio) > 1e-6d)
                sweepOk = false;
            sweepDetail.Add($"{size:F0}:h{heightRatio:F3}/m{marginRatio:F3}/g{gapRatio:F3}");
        }

        Check("8 个字号扫描：高度比 / 留白比 / 间隙比恒定", sweepOk, string.Join(" ", sweepDetail));

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
                          $"KanaSqueeze={LogoLayout.KanaSqueeze}");
        Console.WriteLine($"  X 墨迹：{xInk}   く 墨迹（展开）：{kInk}");
        Console.WriteLine($"  留白：上 {topMargin:F4} / 下 {bottomMargin:F4}（48px），" +
                          $"右端不齐 {Math.Abs(layout.ChevronLowerEnd.X - layout.ChevronUpperEnd.X):F4}");
        Console.WriteLine($"  排版尺寸：{layout.Width:F2} × {layout.Height:F2}");
    }
}
