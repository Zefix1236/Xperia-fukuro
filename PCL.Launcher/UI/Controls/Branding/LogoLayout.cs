using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace PCL.UI.Controls.Branding;

/// <summary>
///     九个字形（X P E R I A ふ &lt; ろ）的静态排版结果，坐标已归一化到内容左上角 (0, 0)。
/// </summary>
/// <remarks>
///     与最早版本的差别：X 横向拉伸 <see cref="XStretch" />，ふ / ろ 横向压缩 <see cref="KanaSqueeze" />，
///     「く」不再是字体字形，而是按 X 缩放后的墨迹框推算出来的自绘 V 形折线（笔宽 = 主字号 ×
///     <see cref="StrokeEm" />，圆头圆角）。收起目标仍然按墨迹边缘计算。
/// </remarks>
internal sealed class LogoLayout
{
    // ---- 排版常量 ----

    /// <summary>假名字号相对主字号的比例。</summary>
    internal const double KanaScale = 0.82d;

    /// <summary>假名整体上移量（× 假名字号）。</summary>
    internal const double KanaLiftEm = 0.07d;

    /// <summary>英文字母之间的字距（× 主字号）。</summary>
    internal const double LatinTrackingEm = 0.05d;

    /// <summary>A 与 ふ 之间的字距（× 假名字号，按 advance 算）。</summary>
    internal const double LatinToKanaTrackingEm = 0.14d;

    /// <summary>ふ 与 く 之间的墨迹间隙（× 主字号）。</summary>
    internal const double GapFuKu = 0.145d;

    /// <summary>く 与 ろ 之间的墨迹间隙（× 主字号）。</summary>
    internal const double GapKuRo = 0.145d;

    // ---- 几何常量 ----

    /// <summary>く 的线宽（× 主字号），记为 S。</summary>
    internal const double StrokeEm = 0.090d;

    /// <summary>收起态 く 与 X 的笔画间隙（× S）。</summary>
    internal const double NestGapRatio = 1.0d;

    /// <summary>く 的高度比例（两条臂的张角）。Wc 与 Hc 同步缩放，所以斜率不受影响。</summary>
    internal const double ChevronHeightRatio = 0.93d;

    /// <summary>X（以及由 X 推算的 く）的横向拉伸。</summary>
    internal const double XStretch = 1.21d;

    /// <summary>ふ / ろ 的横向压缩。</summary>
    internal const double KanaSqueeze = 0.94d;

    /// <summary>收起时七个字形平移进 X 内部的位置（× X 拉伸后的 advance）。</summary>
    internal const double CollapseInsideX = 0.33d;

    /// <summary>
    ///     く 的垂直位置相对 X 墨迹框中心的上移量（× X 墨迹高）。
    ///     两个目标互相冲突：尖角对齐 X 斜线交叉点 → 臂间隙均匀但上下留白不均；
    ///     对齐墨迹框中心 → 留白相等但下部间隙偏紧。实测交叉点在框中心上方 0.0234 × 高，
    ///     这里取一半，把两种偏差各减半（间隙差 14% → 7%，留白差 1.58 → 0.79 DIP @48px）。
    /// </summary>
    internal const double ChevronTipOffsetRatio = 0.0117d;

    private LogoLayout(LogoGlyph anchor, LogoGlyph chevron, List<LogoGlyph> fadingGlyphs,
        double width, double height, double strokeThickness, double fontSize,
        double xAdvance, double pileX, double chevronExpandedVertexX, double chevronCollapsedVertexX,
        double chevronWidth, double chevronHeight, double chevronTanAlpha)
    {
        Anchor = anchor;
        Chevron = chevron;
        FadingGlyphs = fadingGlyphs;
        Width = width;
        Height = height;
        StrokeThickness = strokeThickness;
        FontSize = fontSize;
        XAdvance = xAdvance;
        PileX = pileX;
        ChevronExpandedVertexX = chevronExpandedVertexX;
        ChevronCollapsedVertexX = chevronCollapsedVertexX;
        ChevronWidth = chevronWidth;
        ChevronHeight = chevronHeight;
        ChevronTanAlpha = chevronTanAlpha;
    }

    /// <summary>X：收起过程中不动，始终横向拉伸 <see cref="XStretch" />。</summary>
    public LogoGlyph Anchor { get; }

    /// <summary>く：自绘 V 形折线，用 <see cref="StrokeThickness" /> 的圆头圆角笔画，始终使用强调色。</summary>
    public LogoGlyph Chevron { get; }

    /// <summary>收起时先绘制并淡出的七个字形：P E R I A ふ ろ。</summary>
    public IReadOnlyList<LogoGlyph> FadingGlyphs { get; }

    /// <summary>展开后的内容宽度，控件的固定宽度用它。</summary>
    public double Width { get; }

    /// <summary>展开后的内容高度。</summary>
    public double Height { get; }

    /// <summary>く 的线宽 S。</summary>
    public double StrokeThickness { get; }

    /// <summary>排版时使用的主字号。</summary>
    public double FontSize { get; }

    /// <summary>X 的原始 advance（未乘 <see cref="XStretch" />）。</summary>
    public double XAdvance { get; }

    /// <summary>收起时七个字形叠到的位置。</summary>
    public double PileX { get; }

    /// <summary>展开时 V 形顶点的横坐标。</summary>
    public double ChevronExpandedVertexX { get; }

    /// <summary>收起时 V 形顶点的横坐标。</summary>
    public double ChevronCollapsedVertexX { get; }

    /// <summary>く 的整体墨迹宽 w = Wc + S。</summary>
    public double ChevronWidth { get; }

    /// <summary>く 的整体墨迹高 h = Hc + S。</summary>
    public double ChevronHeight { get; }

    /// <summary>tanα = W / H，用于求 cosα。</summary>
    public double ChevronTanAlpha { get; }

    /// <summary>X 的墨迹范围（归一化坐标）。</summary>
    public Rect AnchorInk => Anchor.InkBounds;

    /// <summary>按字体与字号排版九个字形。</summary>
    public static LogoLayout Build(FontFamily fontFamily, double fontSize)
    {
        var typeface = new Typeface(fontFamily, FontStyle.Normal, FontWeight.Medium);
        var culture = CultureInfo.CurrentUICulture;
        var mainSize = fontSize;
        var kanaSize = fontSize * KanaScale;
        var stroke = fontSize * StrokeEm;

        // 1) 量出字体字形：X P E R I A ふ ろ（く 是自绘的，不量）。
        var texts = new[] { "X", "P", "E", "R", "I", "A", "ふ", "ろ" };
        var kanaFlags = new[] { false, false, false, false, false, false, true, true };
        var shaped = new (FormattedText Text, Geometry Geometry, bool Kana, double Size)[texts.Length];
        var baseline = 0d;
        for (var i = 0; i < texts.Length; i++)
        {
            var isKana = kanaFlags[i];
            var size = isKana ? kanaSize : mainSize;
            var text = new FormattedText(texts[i], culture, FlowDirection.LeftToRight, typeface, size, Brushes.White);
            var geometry = text.BuildGeometry(new Point(0, 0))
                           ?? throw new InvalidOperationException($"字形 {texts[i]} 无法生成矢量轮廓");
            shaped[i] = (text, geometry, isKana, size);
            baseline = Math.Max(baseline, text.Baseline);
        }

        double BaselineY(int index)
        {
            var isKana = shaped[index].Kana;
            return baseline - shaped[index].Text.Baseline - (isKana ? KanaLiftEm * kanaSize : 0d);
        }

        // 2) X：缩放原点在它的排版原点，纵向不变。
        var xGlyph = new LogoGlyph(texts[0], false, shaped[0].Geometry, XStretch)
        {
            X = 0d,
            Y = BaselineY(0),
            Advance = shaped[0].Text.WidthIncludingTrailingWhitespace
        };

        // 3) P E R I A：正常排版；X → P 的左边距额外补上拉伸增量。
        var latin = new List<LogoGlyph>(5);
        var pen = xGlyph.Advance * XStretch + LatinTrackingEm * mainSize;
        for (var i = 1; i <= 5; i++)
        {
            var glyph = new LogoGlyph(texts[i], false, shaped[i].Geometry, 1d)
            {
                X = pen,
                Y = BaselineY(i),
                Advance = shaped[i].Text.WidthIncludingTrailingWhitespace
            };
            latin.Add(glyph);
            pen += glyph.Advance + (i < 5 ? LatinTrackingEm * mainSize : LatinToKanaTrackingEm * kanaSize);
        }

        // 4) ふ：假名字号 + 横向压缩，缩放原点在它自己的排版原点。
        var fu = new LogoGlyph(texts[6], true, shaped[6].Geometry, KanaSqueeze)
        {
            X = pen,
            Y = BaselineY(6),
            Advance = shaped[6].Text.WidthIncludingTrailingWhitespace
        };

        // 5) く：从 X 缩放后的墨迹框推算 V 形折线。
        var xInk = xGlyph.InkBounds;
        var centerLineWidth = Math.Max(xInk.Width - stroke, 1e-3d);
        var centerLineHeight = Math.Max(xInk.Height - stroke, 1e-3d);
        var chevronArmHeight = centerLineHeight * ChevronHeightRatio;
        var chevronArmWidth = centerLineWidth / 2d * ChevronHeightRatio;
        var chevronWidth = chevronArmWidth + stroke;
        var chevronHeight = chevronArmHeight + stroke;
        var chevronGeometry = BuildChevronGeometry(stroke, chevronArmWidth, chevronArmHeight, chevronHeight);

        var chevron = new LogoGlyph("く", true, chevronGeometry, 1d,
            new Rect(0d, 0d, chevronWidth, chevronHeight))
        {
            X = fu.InkBounds.Right + GapFuKu * mainSize,
            // 垂直：尖角对齐 X 两条斜线的交叉点（比墨迹中心略高），不参与假名的上提。
            Y = xInk.Center.Y - chevronHeight / 2d - ChevronTipOffsetRatio * xInk.Height
        };

        // 6) ろ：假名字号 + 横向压缩，左墨迹边缘挨着 く 的右墨迹边缘。
        var ro = new LogoGlyph(texts[7], true, shaped[7].Geometry, KanaSqueeze)
        {
            X = chevron.InkBounds.Right + GapKuRo * mainSize - KanaSqueeze * shaped[7].Geometry.Bounds.Left,
            Y = BaselineY(7),
            Advance = shaped[7].Text.WidthIncludingTrailingWhitespace
        };

        // 7) 归一化到内容左上角。
        var all = new List<LogoGlyph>(9) { xGlyph };
        all.AddRange(latin);
        all.Add(fu);
        all.Add(chevron);
        all.Add(ro);

        double minLeft = double.MaxValue, minTop = double.MaxValue;
        double maxRight = double.MinValue, maxBottom = double.MinValue;
        foreach (var glyph in all)
        {
            var ink = glyph.InkBounds;
            minLeft = Math.Min(minLeft, ink.Left);
            minTop = Math.Min(minTop, ink.Top);
            maxRight = Math.Max(maxRight, ink.Right);
            maxBottom = Math.Max(maxBottom, ink.Bottom);
        }

        foreach (var glyph in all)
        {
            glyph.X -= minLeft;
            glyph.Y -= minTop;
        }

        // 8) 收起目标：七个字形叠到 X 内部 33%（× X 拉伸后的 advance）。
        var xAdvance = shaped[0].Text.WidthIncludingTrailingWhitespace;
        var pile = xGlyph.X + xAdvance * XStretch * CollapseInsideX;
        var fadingGlyphs = new List<LogoGlyph>(7);
        foreach (var glyph in latin)
        {
            glyph.CollapseDx = pile - glyph.X;
            fadingGlyphs.Add(glyph);
        }

        fu.CollapseDx = pile - fu.X;
        fadingGlyphs.Add(fu);
        ro.CollapseDx = pile - ro.X;
        fadingGlyphs.Add(ro);

        // 9) く 的收起位移：顶点横坐标按 S 与 X 的斜边角度推算。
        var anchorInk = xGlyph.InkBounds;
        var tanAlpha = centerLineWidth / centerLineHeight;
        var cosAlpha = 1d / Math.Sqrt(1d + tanAlpha * tanAlpha);
        var expandedVertexX = chevron.X + stroke / 2d;
        var collapsedVertexX = anchorInk.Center.X + 2d * stroke * NestGapRatio / cosAlpha;
        chevron.CollapseDx = collapsedVertexX - expandedVertexX;

        return new LogoLayout(xGlyph, chevron, fadingGlyphs,
            maxRight - minLeft, maxBottom - minTop, stroke, mainSize,
            xAdvance, pile, expandedVertexX, collapsedVertexX,
            chevronWidth, chevronHeight, tanAlpha);
    }

    private static StreamGeometry BuildChevronGeometry(double stroke, double armWidth, double armHeight, double height)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(stroke / 2d + armWidth, stroke / 2d), false);
            context.LineTo(new Point(stroke / 2d, height / 2d), true);
            context.LineTo(new Point(stroke / 2d + armWidth, stroke / 2d + armHeight), true);
            context.EndFigure(false);
        }

        return geometry;
    }
}

/// <summary>单个字形：轮廓、缩放、展开位置、墨迹范围与收起时的水平平移量。</summary>
internal sealed class LogoGlyph
{
    private readonly Rect _rawInk;

    internal LogoGlyph(string text, bool isKana, Geometry geometry, double scaleX, Rect? inkOverride = null)
    {
        Text = text;
        IsKana = isKana;
        Geometry = geometry;
        ScaleX = scaleX;
        _rawInk = inkOverride ?? geometry.Bounds;
    }

    public string Text { get; }

    public bool IsKana { get; }

    /// <summary>在 (0, 0) 处构建的矢量轮廓；绘制时先按 <see cref="ScaleX" /> 缩放，再平移到 <see cref="X" /> / <see cref="Y" />。</summary>
    public Geometry Geometry { get; }

    /// <summary>横向缩放系数，缩放原点在字形自己的排版原点。</summary>
    public double ScaleX { get; }

    /// <summary>展开时的排版左边缘（绘制原点）。</summary>
    public double X { get; set; }

    /// <summary>展开时的排版顶边（已含基线与假名上移）。</summary>
    public double Y { get; set; }

    /// <summary>本字形的原始墨迹范围（相对绘制原点）。</summary>
    public Rect RawInk => _rawInk;

    /// <summary>本字形的 advance（字体字形才有意义）。</summary>
    public double Advance { get; init; }

    /// <summary>展开时的墨迹范围（含缩放，已归一化）。</summary>
    public Rect InkBounds => new(X + ScaleX * _rawInk.Left, Y + _rawInk.Top,
        ScaleX * _rawInk.Width, _rawInk.Height);

    /// <summary>收起时相对展开位置的水平平移量。</summary>
    public double CollapseDx { get; set; }
}
