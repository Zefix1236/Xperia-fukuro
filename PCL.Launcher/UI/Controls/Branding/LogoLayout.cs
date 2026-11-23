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
///     「く」不再是字体字形，而是 X 右半边骨架的平移拷贝：两臂分别落在 X 两条斜臂中心线向外平移
///     <see cref="NestGapRatio" /> × 笔画宽的位置，各自平行、间隙恒定；上下端对 X 墨迹框的留白相等。
///     因为 X 本身的右半边就是「上臂短、下臂长」，所以 &lt; 的两臂长度不等，这是 X 的结构，不是缺陷。
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

    /// <summary>く 与 X 两条中心线的垂直间隙，单位是 S（1 表示两笔边缘间隙 = S）。</summary>
    internal const double NestGapRatio = 1.0d;

    /// <summary>く 的高度占 X 墨迹高的比例；1 表示与 X 同高（上下留白为 0），0.93 表示各留 3.5%。</summary>
    internal const double ChevronHeightRatio = 0.93d;

    /// <summary>X 的横向拉伸。</summary>
    internal const double XStretch = 1.21d;

    /// <summary>ふ / ろ 的横向压缩。</summary>
    internal const double KanaSqueeze = 0.94d;

    /// <summary>收起时七个字形平移进 X 内部的位置（× X 拉伸后的 advance）。</summary>
    internal const double CollapseInsideX = 0.33d;

    private LogoLayout(LogoGlyph anchor, LogoGlyph chevron, List<LogoGlyph> fadingGlyphs,
        double width, double height, double strokeThickness, double fontSize, double xAdvance, double pileX,
        XGlyphSkeleton skeleton, double xStrokeWidth, double centerlineGap, double expandedDx,
        Point chevronUpper, Point chevronVertex, Point chevronLower)
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
        Skeleton = skeleton;
        XStrokeWidth = xStrokeWidth;
        CenterlineGap = centerlineGap;
        ChevronExpandedDx = expandedDx;
        ChevronUpperEnd = chevronUpper;
        ChevronVertex = chevronVertex;
        ChevronLowerEnd = chevronLower;
    }

    /// <summary>X：收起过程中不动，始终横向拉伸 <see cref="XStretch" />。</summary>
    public LogoGlyph Anchor { get; }

    /// <summary>く：自绘折线，用 <see cref="StrokeThickness" /> 的圆头圆角笔画，始终使用强调色。</summary>
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

    /// <summary>X 字形骨架（参考字号下的测量结果）。</summary>
    public XGlyphSkeleton Skeleton { get; }

    /// <summary>X 拉伸后的垂直笔画宽（当前字号）。</summary>
    public double XStrokeWidth { get; }

    /// <summary>く 与 X 两条中心线的垂直距离；(Sx + S) / 2 + NestGapRatio × S。</summary>
    public double CenterlineGap { get; }

    /// <summary>展开态相对收起态（嵌在 X 里）的右移量；收起动画就是把它乘以 (1 - pe)。</summary>
    public double ChevronExpandedDx { get; }

    /// <summary>折线上端点（排版坐标）。</summary>
    public Point ChevronUpperEnd { get; }

    /// <summary>折线尖角（排版坐标）。</summary>
    public Point ChevronVertex { get; }

    /// <summary>折线下端点（排版坐标）。</summary>
    public Point ChevronLowerEnd { get; }

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

        // 5) く：X 右半边骨架的平移拷贝。
        var skeleton = XGlyphSkeleton.For(fontFamily);
        var scale = fontSize / XGlyphSkeleton.ReferenceSize;
        var xStroke = skeleton.StrokeWidth * scale;
        var aUpper = skeleton.InterceptUpper * scale;
        var aLower = skeleton.InterceptLower * scale;
        var mUpper = skeleton.SlopeUpper;
        var mLower = skeleton.SlopeLower;

        var centerlineGap = (xStroke + stroke) / 2d + NestGapRatio * stroke;
        var shiftUpper = centerlineGap * Math.Sqrt(1d + mUpper * mUpper);
        var shiftLower = centerlineGap * Math.Sqrt(1d + mLower * mLower);

        var yVertex = ((aLower + shiftLower) - (aUpper + shiftUpper)) / (mUpper - mLower);
        var xVertex = aUpper + shiftUpper + mUpper * yVertex;

        var margin = (1d - ChevronHeightRatio) * (skeleton.InkBottom - skeleton.InkTop) * scale / 2d;
        var yUp = skeleton.InkTop * scale + stroke / 2d + margin;
        var yLow = skeleton.InkBottom * scale - stroke / 2d - margin;
        var xUp = aUpper + shiftUpper + mUpper * yUp;
        var xLow = aLower + shiftLower + mLower * yLow;

        var chevronGeometry = BuildChevronGeometry(
            new Point(xUp, yUp), new Point(xVertex, yVertex), new Point(xLow, yLow));
        var chevronInk = new Rect(
            xVertex - stroke / 2d,
            yUp - stroke / 2d,
            Math.Max(xUp, xLow) + stroke / 2d - (xVertex - stroke / 2d),
            yLow - yUp + stroke);

        var chevron = new LogoGlyph("く", true, chevronGeometry, 1d, chevronInk)
        {
            X = fu.InkBounds.Right + GapFuKu * mainSize - chevronInk.Left,
            Y = xGlyph.Y
        };

        // 6) ろ：假名字号 + 横向压缩，左墨迹边缘挨着 く 展开后的右墨迹边缘。
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

        // 9) 收起态 く 恰好嵌在 X 右半边：平移量 = -(展开位移)。
        var expandedDx = chevron.X - xGlyph.X;
        chevron.CollapseDx = -expandedDx;

        var upper = new Point(chevron.X + xUp, chevron.Y + yUp);
        var vertex = new Point(chevron.X + xVertex, chevron.Y + yVertex);
        var lower = new Point(chevron.X + xLow, chevron.Y + yLow);

        return new LogoLayout(xGlyph, chevron, fadingGlyphs,
            maxRight - minLeft, maxBottom - minTop, stroke, mainSize, xAdvance, pile,
            skeleton, xStroke, centerlineGap, expandedDx, upper, vertex, lower);
    }

    private static StreamGeometry BuildChevronGeometry(Point upper, Point vertex, Point lower)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(upper, false);
            context.LineTo(vertex, true);
            context.LineTo(lower, true);
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
