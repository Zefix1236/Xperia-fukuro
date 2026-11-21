using System;

namespace PCL.UI.Controls.Branding;

/// <summary>
///     收起 / 展开动画的播放头。
///     <para>
///         <c>frame</c> 范围 0..<see cref="CollapsedFrame" />：0 为完全展开，18 为完全收起，按每秒
///         <see cref="UnitsPerSecond" /> 个单位推进。改变目标时只改方向、不改当前帧，所以任意时刻
///         反向都会从当前进度平滑掉头，不会跳变。
///     </para>
///     <para>
///         <see cref="PositionProgress" />（pe）用于水平平移，<see cref="OpacityProgress" />（po）
///         用于七个字形的淡出：po 在前 9 帧就走完，因此淡化比位移先结束。
///     </para>
/// </summary>
internal sealed class LogoPlayhead
{
    /// <summary>完全展开时的帧号。</summary>
    public const double ExpandedFrame = 0d;

    /// <summary>完全收起时的帧号。</summary>
    public const double CollapsedFrame = 18d;

    /// <summary>每秒推进的帧数；18 / 30 = 0.6 秒走完全程。</summary>
    public const double UnitsPerSecond = 30d;

    // cubic-bezier(0.77, 0, 0.175, 1)：控制点 x1, x2 与 y1, y2。
    private const double BezierX1 = 0.77d;
    private const double BezierX2 = 0.175d;
    private const double BezierY1 = 0d;
    private const double BezierY2 = 1d;

    private const int BisectionIterations = 24;
    private const double MovingEpsilon = 1e-6d;

    /// <summary>当前帧。</summary>
    public double Frame { get; private set; } = CollapsedFrame;

    /// <summary>目标帧，取值只能是 <see cref="ExpandedFrame" /> 或 <see cref="CollapsedFrame" />。</summary>
    public double Target { get; private set; } = CollapsedFrame;

    /// <summary>是否还没走到目标。</summary>
    public bool IsMoving => Math.Abs(Frame - Target) > MovingEpsilon;

    /// <summary>位置进度：frame / 18 经过缓动。</summary>
    public double PositionProgress => Ease(Frame / CollapsedFrame);

    /// <summary>透明度进度：min(frame / 9, 1) 经过缓动。</summary>
    public double OpacityProgress => Ease(Math.Min(Frame / 9d, 1d));

    /// <summary>设置目标帧；不改变当前帧，因此可以随时反向。</summary>
    public void SetTarget(double target)
    {
        Target = Math.Clamp(target, ExpandedFrame, CollapsedFrame);
    }

    /// <summary>把播放头推进 <paramref name="seconds" /> 秒，返回是否仍在移动。</summary>
    public bool Advance(double seconds)
    {
        if (seconds > 0d && IsMoving)
        {
            var step = UnitsPerSecond * seconds;
            var delta = Target - Frame;
            Frame = Math.Abs(delta) <= step ? Target : Frame + Math.Sign(delta) * step;
        }

        return IsMoving;
    }

    /// <summary>直接跳到某一帧（测试与初始化用）。</summary>
    public void Seek(double frame)
    {
        Frame = Math.Clamp(frame, ExpandedFrame, CollapsedFrame);
        Target = Frame;
    }

    /// <summary>cubic-bezier(0.77, 0, 0.175, 1)：先求 x(t) = t 的参数，再取 y(t)。</summary>
    internal static double Ease(double t)
    {
        if (t <= 0d) return 0d;
        if (t >= 1d) return 1d;

        var low = 0d;
        var high = 1d;
        for (var i = 0; i < BisectionIterations; i++)
        {
            var mid = (low + high) / 2d;
            if (Curve(mid, BezierX1, BezierX2) < t) low = mid;
            else high = mid;
        }

        return Curve((low + high) / 2d, BezierY1, BezierY2);
    }

    private static double Curve(double t, double p1, double p2)
    {
        var inverse = 1d - t;
        return 3d * inverse * inverse * t * p1 + 3d * inverse * t * t * p2 + t * t * t;
    }
}
