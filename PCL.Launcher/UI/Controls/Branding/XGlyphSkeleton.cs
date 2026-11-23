using System;
using System.Collections.Concurrent;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace PCL.UI.Controls.Branding;

/// <summary>
///     X 字形右半边的骨架：在参考字号 <see cref="ReferenceSize" /> 下扫描轮廓，得到两条斜臂的中心线、
///     交叉点与（横向拉伸后的）垂直笔画宽。结果只跟字体有关，按字体族缓存；排版时乘 fontSize / 1000。
/// </summary>
/// <remarks>
///     扫描避开了端头圆角与交叉点附近的重叠区：上半取 0.20 / 0.33 高度，下半取 0.67 / 0.80 高度，
///     另外各取 0.26 / 0.74 高度做残差自检。x 方向的测量值直接乘 <see cref="LogoLayout.XStretch" />，
///     所以这里的线和宽度都已经是拉伸后的坐标。
/// </remarks>
internal sealed class XGlyphSkeleton
{
    internal const double ReferenceSize = 1000d;

    private const double ScanStep = 0.5d;
    private const double UpperRowA = 0.20d;
    private const double UpperRowB = 0.33d;
    private const double LowerRowA = 0.67d;
    private const double LowerRowB = 0.80d;
    private const double CheckUpperRow = 0.26d;
    private const double CheckLowerRow = 0.74d;

    private static readonly ConcurrentDictionary<string, XGlyphSkeleton> Cache = new(StringComparer.Ordinal);

    private XGlyphSkeleton(double inkTop, double inkBottom,
        double slopeUpper, double interceptUpper, double slopeLower, double interceptLower,
        double strokeWidth, double crossingX, double crossingY,
        double residualUpper, double residualLower)
    {
        InkTop = inkTop;
        InkBottom = inkBottom;
        SlopeUpper = slopeUpper;
        InterceptUpper = interceptUpper;
        SlopeLower = slopeLower;
        InterceptLower = interceptLower;
        StrokeWidth = strokeWidth;
        CrossingX = crossingX;
        CrossingY = crossingY;
        ResidualUpper = residualUpper;
        ResidualLower = residualLower;
    }

    /// <summary>墨迹上缘（参考字号本地坐标）。</summary>
    public double InkTop { get; }

    /// <summary>墨迹下缘（参考字号本地坐标）。</summary>
    public double InkBottom { get; }

    /// <summary>墨迹高度。</summary>
    public double Height => InkBottom - InkTop;

    /// <summary>右上臂中心线斜率（为负：y 增大时向左）。</summary>
    public double SlopeUpper { get; }

    /// <summary>右上臂中心线截距（x = Intercept + Slope × y）。</summary>
    public double InterceptUpper { get; }

    /// <summary>右下臂中心线斜率（为正）。</summary>
    public double SlopeLower { get; }

    /// <summary>右下臂中心线截距。</summary>
    public double InterceptLower { get; }

    /// <summary>拉伸后 X 斜臂的垂直笔画宽（参考字号）。</summary>
    public double StrokeWidth { get; }

    /// <summary>两条中心线的交叉点 x。</summary>
    public double CrossingX { get; }

    /// <summary>两条中心线的交叉点 y。</summary>
    public double CrossingY { get; }

    /// <summary>上半残差（自检行与拟合线的距离）。</summary>
    public double ResidualUpper { get; }

    /// <summary>下半残差。</summary>
    public double ResidualLower { get; }

    /// <summary>取某个字体族的骨架，按字体名缓存。</summary>
    public static XGlyphSkeleton For(FontFamily family) =>
        Cache.GetOrAdd(family.Name, _ => Measure(family));

    private static XGlyphSkeleton Measure(FontFamily family)
    {
        var typeface = new Typeface(family, FontStyle.Normal, FontWeight.Medium);
        var text = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            typeface, ReferenceSize, Brushes.White);
        var geometry = text.BuildGeometry(new Point(0, 0))
                       ?? throw new InvalidOperationException("X 字形无法生成矢量轮廓");

        var bounds = geometry.Bounds;
        var top = bounds.Top;
        var height = bounds.Height;
        var scanStart = bounds.Right + 2d;

        double RowY(double fraction) => top + fraction * height;

        var upperA = MeasureRow(geometry, RowY(UpperRowA), scanStart);
        var upperB = MeasureRow(geometry, RowY(UpperRowB), scanStart);
        var lowerA = MeasureRow(geometry, RowY(LowerRowA), scanStart);
        var lowerB = MeasureRow(geometry, RowY(LowerRowB), scanStart);

        var slopeUpper = (upperB.CenterX - upperA.CenterX) / (RowY(UpperRowB) - RowY(UpperRowA));
        var interceptUpper = upperA.CenterX - slopeUpper * RowY(UpperRowA);
        var slopeLower = (lowerB.CenterX - lowerA.CenterX) / (RowY(LowerRowB) - RowY(LowerRowA));
        var interceptLower = lowerA.CenterX - slopeLower * RowY(LowerRowA);

        if (slopeUpper >= 0d || slopeLower <= 0d)
            throw new InvalidOperationException(
                $"X 的斜臂方向异常（上 {slopeUpper:F3} / 下 {slopeLower:F3}），可能不是直线笔画");

        var crossingY = (interceptLower - interceptUpper) / (slopeUpper - slopeLower);
        var crossingX = interceptUpper + slopeUpper * crossingY;

        static double Perpendicular(double horizontalWidth, double slope) =>
            horizontalWidth / Math.Sqrt(1d + slope * slope);

        var strokeWidth = (Perpendicular(upperA.Width, slopeUpper) + Perpendicular(upperB.Width, slopeUpper)
                           + Perpendicular(lowerA.Width, slopeLower) + Perpendicular(lowerB.Width, slopeLower)) / 4d;

        var checkUpper = MeasureRow(geometry, RowY(CheckUpperRow), scanStart);
        var checkLower = MeasureRow(geometry, RowY(CheckLowerRow), scanStart);
        var residualUpper = Math.Abs(checkUpper.CenterX - (interceptUpper + slopeUpper * RowY(CheckUpperRow)));
        var residualLower = Math.Abs(checkLower.CenterX - (interceptLower + slopeLower * RowY(CheckLowerRow)));

        return new XGlyphSkeleton(top, top + height,
            slopeUpper, interceptUpper, slopeLower, interceptLower,
            strokeWidth, crossingX, crossingY, residualUpper, residualLower);
    }

    /// <summary>在给定 y 上从右往左扫，取最右一段墨迹的中心与横向宽（已乘 XStretch）。</summary>
    private static (double CenterX, double Width) MeasureRow(Geometry geometry, double y, double xStart)
    {
        double? right = null;
        var left = xStart;
        var misses = 0;

        for (var x = xStart; x > xStart - ReferenceSize * 2d; x -= ScanStep)
        {
            if (geometry.FillContains(new Point(x, y)))
            {
                right ??= x;
                left = x;
                misses = 0;
            }
            else if (right is not null && ++misses >= 2)
            {
                break;
            }
        }

        if (right is null)
            throw new InvalidOperationException($"X 在 y = {y:F1} 处没有墨迹");

        var center = (left + right.Value) / 2d;
        var width = right.Value - left;
        return (center * LogoLayout.XStretch, width * LogoLayout.XStretch);
    }
}
