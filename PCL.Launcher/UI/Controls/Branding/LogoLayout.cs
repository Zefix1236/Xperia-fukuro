using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace PCL.UI.Controls.Branding;

/// <summary>
///     九个字形（X P E R I A ふ く ろ）的静态排版结果，坐标已归一化到内容左上角 (0, 0)。
/// </summary>
/// <remarks>
///     排版规则（单位 em 均相对字号）：假名字号 = 主字号 × 0.82，假名整体上移 0.07 个假名字号以对齐
///     英文大写的视觉中线；基线对齐；字距为英文之间 +0.05 主字号、A → ふ +0.14 假名字号、
///     假名之间 −0.22 假名字号。收起位置一律按字形轮廓的 <see cref="Geometry.Bounds" /> 计算，
///     不用字框宽度估算。
/// </remarks>
internal sealed class LogoLayout
{
    /// <summary>假名字号相对主字号的比例。</summary>
    internal const double KanaScale = 0.82d;

    /// <summary>假名整体上移量（× 假名字号）。</summary>
    internal const double KanaLiftEm = 0.07d;

    /// <summary>英文之间的字距（× 主字号）。</summary>
    internal const double LatinTrackingEm = 0.05d;

    /// <summary>A 与 ふ 之间的字距（× 假名字号）。</summary>
    internal const double LatinToKanaTrackingEm = 0.14d;

    /// <summary>假名之间的字距（× 假名字号）。</summary>
    internal const double KanaTrackingEm = -0.22d;

    /// <summary>收起时七个字形平移进 X 内部的位置（× X 墨迹宽度）。</summary>
    internal const double CollapseInsideX = 0.33d;

    /// <summary>收起时 く 左边缘相对 X 右边缘的偏移（× 主字号，负值表示压进 X）。</summary>
    internal const double KanaOverlapEm = 0.08d;

    private static readonly string[] Texts = ["X", "P", "E", "R", "I", "A", "ふ", "く", "ろ"];
    private static readonly bool[] KanaFlags = [false, false, false, false, false, false, true, true, true];

    private LogoLayout(LogoGlyph anchor, LogoGlyph accent, IReadOnlyList<LogoGlyph> fadingGlyphs,
        double width, double height)
    {
        Anchor = anchor;
        Accent = accent;
        FadingGlyphs = fadingGlyphs;
        Width = width;
        Height = height;
    }

    /// <summary>X：收起过程中不动。</summary>
    public LogoGlyph Anchor { get; }

    /// <summary>く：收起后贴在 X 右侧，使用强调色。</summary>
    public LogoGlyph Accent { get; }

    /// <summary>收起时先绘制并淡出的七个字形：P E R I A ふ ろ。</summary>
    public IReadOnlyList<LogoGlyph> FadingGlyphs { get; }

    /// <summary>展开后的内容宽度，控件的固定宽度用它。</summary>
    public double Width { get; }

    /// <summary>展开后的内容高度。</summary>
    public double Height { get; }

    /// <summary>X 的墨迹范围（归一化坐标）。</summary>
    public Rect AnchorInk => Anchor.InkBounds;

    /// <summary>收起后 く 左边缘的目标位置。</summary>
    public double AccentTargetLeft => AnchorInk.Right - KanaOverlapEm * FontSize;

    /// <summary>排版时使用的主字号。</summary>
    public double FontSize { get; private init; }

    /// <summary>按字体与字号排版九个字形。</summary>
    public static LogoLayout Build(FontFamily fontFamily, double fontSize)
    {
        var typeface = new Typeface(fontFamily, FontStyle.Normal, FontWeight.Medium);
        var culture = CultureInfo.CurrentUICulture;
        var mainSize = fontSize;
        var kanaSize = fontSize * KanaScale;

        // 第一遍：量出各字形的排版框与基线，取共同基线。
        var shaped = new (FormattedText Text, bool IsKana, double Size)[Texts.Length];
        var baseline = 0d;
        for (var i = 0; i < Texts.Length; i++)
        {
            var isKana = KanaFlags[i];
            var size = isKana ? kanaSize : mainSize;
            var text = new FormattedText(Texts[i], culture, FlowDirection.LeftToRight, typeface, size, Brushes.White);
            shaped[i] = (text, isKana, size);
            baseline = Math.Max(baseline, text.Baseline);
        }

        // 第二遍：按字距排 x，按基线排 y；假名再整体上移。
        var raw = new (Geometry Geometry, double X, double Y)[Texts.Length];
        var pen = 0d;
        for (var i = 0; i < Texts.Length; i++)
        {
            var (text, isKana, size) = shaped[i];
            var y = baseline - text.Baseline - (isKana ? KanaLiftEm * kanaSize : 0d);
            var geometry = text.BuildGeometry(new Point(0, 0))
                           ?? throw new InvalidOperationException($"字形 {Texts[i]} 无法生成矢量轮廓");
            raw[i] = (geometry, pen, y);

            pen += text.WidthIncludingTrailingWhitespace;
            if (i + 1 < Texts.Length)
                pen += Gap(isKana, KanaFlags[i + 1], mainSize, kanaSize);
        }

        // 归一化：把内容左上角移到 (0, 0)。
        double minLeft = double.MaxValue, minTop = double.MaxValue;
        double maxRight = double.MinValue, maxBottom = double.MinValue;
        foreach (var (geometry, x, y) in raw)
        {
            var bounds = geometry.Bounds.Translate(new Vector(x, y));
            minLeft = Math.Min(minLeft, bounds.Left);
            minTop = Math.Min(minTop, bounds.Top);
            maxRight = Math.Max(maxRight, bounds.Right);
            maxBottom = Math.Max(maxBottom, bounds.Bottom);
        }

        var glyphs = new LogoGlyph[raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            var (geometry, x, y) = raw[i];
            glyphs[i] = new LogoGlyph(Texts[i], KanaFlags[i], geometry, x - minLeft, y - minTop);
        }

        // 收起目标：先按 X 的墨迹算出中心线，七个字形平移到 X 内部 33% 处，く 贴到 X 右侧。
        var anchor = glyphs[0];
        var anchorInk = anchor.InkBounds;
        var fadeLeft = anchorInk.Left + anchorInk.Width * CollapseInsideX;
        var accentTargetLeft = anchorInk.Right - KanaOverlapEm * mainSize;

        var fading = new List<LogoGlyph>(7);
        foreach (var glyph in glyphs)
        {
            if (ReferenceEquals(glyph, anchor)) continue;
            if (glyph.IsKana && glyph.Text == "く")
            {
                glyph.CollapseDx = accentTargetLeft - glyph.InkBounds.Left;
                continue;
            }

            glyph.CollapseDx = fadeLeft - glyph.InkBounds.Left;
            fading.Add(glyph);
        }

        var width = Math.Max(pen - minLeft, maxRight - minLeft);
        var height = maxBottom - minTop;
        return new LogoLayout(anchor, glyphs[7], fading, width, height) { FontSize = mainSize };
    }

    private static double Gap(bool fromKana, bool toKana, double mainSize, double kanaSize)
    {
        if (!fromKana && !toKana) return LatinTrackingEm * mainSize;
        if (!fromKana) return LatinToKanaTrackingEm * kanaSize;
        return KanaTrackingEm * kanaSize;
    }
}

/// <summary>单个字形：轮廓、展开位置、墨迹范围与收起时的水平平移量。</summary>
internal sealed class LogoGlyph(string text, bool isKana, Geometry geometry, double x, double y)
{
    private readonly Rect _inkBounds = geometry.Bounds.Translate(new Vector(x, y));

    public string Text { get; } = text;

    public bool IsKana { get; } = isKana;

    /// <summary>在 (0, 0) 处构建的矢量轮廓；绘制时平移到 <see cref="X" /> / <see cref="Y" />。</summary>
    public Geometry Geometry { get; } = geometry;

    /// <summary>展开时的排版左边缘。</summary>
    public double X { get; } = x;

    /// <summary>展开时的排版顶边（已含基线与假名上移）。</summary>
    public double Y { get; } = y;

    /// <summary>展开时的墨迹范围（归一化坐标）。</summary>
    public Rect InkBounds => _inkBounds;

    /// <summary>收起时相对展开位置的水平平移量。</summary>
    public double CollapseDx { get; set; }
}
