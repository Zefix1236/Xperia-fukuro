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
        var control = new XperiaFukuroLogo { FontSize = 48d };
        var layout = control.CurrentLayout;
        Check("排版生成成功", layout is not null);
        if (layout is null) return;

        Check("九个字形 = X + く + 七个淡出字形",
            layout.Anchor.Text == "X" && layout.Accent.Text == "く" && layout.FadingGlyphs.Count == 7,
            $"anchor {layout.Anchor.Text}，accent {layout.Accent.Text}，fading {layout.FadingGlyphs.Count}");
        Check("宽高为正", layout.Width > 0d && layout.Height > 0d,
            $"{layout.Width:F2} × {layout.Height:F2}");

        var expectedAccentLeft = layout.AnchorInk.Right - 0.08d * 48d;
        Check("く 收起目标 = X 墨迹右边缘 − 0.08em",
            Math.Abs(layout.AccentTargetLeft - expectedAccentLeft) < 1e-9d,
            $"目标 {layout.AccentTargetLeft:F4}，期望 {expectedAccentLeft:F4}");
        Check("く 平移后左边缘等于目标",
            Math.Abs(layout.Accent.InkBounds.Left + layout.Accent.CollapseDx - expectedAccentLeft) < 1e-9d,
            $"实际 {layout.Accent.InkBounds.Left + layout.Accent.CollapseDx:F4}");

        var fadeTarget = layout.AnchorInk.Left + layout.AnchorInk.Width * 0.33d;
        var fadeOk = layout.FadingGlyphs.All(glyph =>
            Math.Abs(glyph.InkBounds.Left + glyph.CollapseDx - fadeTarget) < 1e-9d);
        Check("七个字形收起后左边缘都落在 X 内部 33%", fadeOk, $"目标 {fadeTarget:F4}");

        control.Measure(Size.Infinity);
        var expandedSize = control.DesiredSize;
        control.SeekFrame(LogoPlayhead.CollapsedFrame);
        control.InvalidateMeasure();
        control.Measure(Size.Infinity);
        var collapsedSize = control.DesiredSize;

        var padding = XperiaFukuroLogo.ContentPadding * 2d;
        // Avalonia 的布局会把 DesiredSize 向上取整到整像素，所以允许 1 DIP 误差。
        Check("控件尺寸 = 排版尺寸 + 四周内边距（整像素取整）",
            Math.Abs(expandedSize.Width - (layout.Width + padding)) <= 1d &&
            Math.Abs(expandedSize.Height - (layout.Height + padding)) <= 1d,
            $"{expandedSize.Width:F2} × {expandedSize.Height:F2}，排版 {layout.Width:F2} × {layout.Height:F2}");
        Check("收起后控件尺寸不变",
            Math.Abs(expandedSize.Width - collapsedSize.Width) < 1e-9d &&
            Math.Abs(expandedSize.Height - collapsedSize.Height) < 1e-9d,
            $"{expandedSize.Width:F2} × {expandedSize.Height:F2} → {collapsedSize.Width:F2} × {collapsedSize.Height:F2}");

        var typeface = new Typeface(XperiaFukuroLogo.ZenMaruGothic, FontStyle.Normal, FontWeight.Medium);
        var resolved = FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface);
        Check("字体解析到 Zen Maru Gothic", resolved, resolved ? glyphTypeface?.FamilyName ?? "未知" : "解析失败");

        Console.WriteLine($"  X 墨迹：{layout.AnchorInk}");
        Console.WriteLine($"  く 墨迹（展开）：{layout.Accent.InkBounds}");
        Console.WriteLine($"  排版尺寸：{layout.Width:F2} × {layout.Height:F2}");
    }
}
