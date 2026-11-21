using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace PCL.UI.Controls.Branding;

/// <summary>
///     自绘的 Xperia-fukuro 字形 logo。
///     <para>
///         展开态为九个字形 <c>X P E R I A ふ く ろ</c>；收起态只留 X 与 く，其余七个字形水平平移进
///         X 内部的同时淡出，く 则贴到 X 右侧。
///     </para>
///     <para>
///         轮廓由 <see cref="FormattedText.BuildGeometry" /> 取得，动画由 <see cref="LogoPlayhead" />
///         的播放头驱动（<see cref="TopLevel.RequestAnimationFrame" /> 逐帧推进），不使用 Avalonia 的
///         Transitions / Animations，也不依赖 Lottie。
///     </para>
/// </summary>
public class XperiaFukuroLogo : Control
{
    /// <summary>默认字体：随程序集一起嵌入的 Zen Maru Gothic Medium（SIL OFL 1.1）。</summary>
    public static readonly FontFamily ZenMaruGothic = new(
        $"avares://{typeof(XperiaFukuroLogo).Assembly.GetName().Name}/Assets/Fonts#Zen Maru Gothic");

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<XperiaFukuroLogo, double>(nameof(FontSize), 24d);

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<XperiaFukuroLogo, FontFamily>(nameof(FontFamily), ZenMaruGothic);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<XperiaFukuroLogo, IBrush?>(nameof(Foreground), Brushes.White);

    public static readonly StyledProperty<IBrush?> AccentProperty =
        AvaloniaProperty.Register<XperiaFukuroLogo, IBrush?>(nameof(Accent), Brushes.White);

    private readonly LogoPlayhead _playhead = new();

    private LogoLayout? _layout;
    private bool _rafPending;
    private long _rafToken;
    private TimeSpan? _lastTick;

    static XperiaFukuroLogo()
    {
        AffectsMeasure<XperiaFukuroLogo>(FontSizeProperty, FontFamilyProperty);
        AffectsRender<XperiaFukuroLogo>(FontSizeProperty, FontFamilyProperty, ForegroundProperty, AccentProperty);
    }

    /// <summary>主字号；假名字号为其 0.82 倍。</summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>字形字体，默认嵌入的 Zen Maru Gothic。</summary>
    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>X、P、E、R、I、A、ふ、ろ 的颜色。</summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>く 的强调色。</summary>
    public IBrush? Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>是否已完全展开（播放头在 0 帧附近）。</summary>
    public bool IsExpanded => _playhead.Frame <= LogoPlayhead.ExpandedFrame + 1e-6d;

    /// <summary>播放头当前位置，0 为完全展开，18 为完全收起。</summary>
    internal double CurrentFrame => _playhead.Frame;

    /// <summary>当前排版结果；字体或字号改变后会重新生成。</summary>
    internal LogoLayout? CurrentLayout => EnsureLayout();

    /// <summary>播放头每推进一帧触发一次；演示窗口用它显示实时帧号。</summary>
    internal event Action<double>? FrameAdvanced;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FontSizeProperty || change.Property == FontFamilyProperty)
        {
            _layout = null;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = EnsureLayout();
        return layout is null ? default : new Size(layout.Width, layout.Height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        // 控件宽度固定为展开后的宽度，宽度不随父容器拉伸。
        var width = finalSize.Width > 0d ? Math.Min(DesiredSize.Width, finalSize.Width) : DesiredSize.Width;
        var height = finalSize.Height > 0d ? Math.Min(DesiredSize.Height, finalSize.Height) : DesiredSize.Height;
        return new Size(width, height);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // 透明矩形让整块控件区域都参与命中测试（悬停依赖它）。
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

        var layout = EnsureLayout();
        if (layout is null) return;

        var foreground = Foreground;
        var accent = Accent;
        var positionProgress = _playhead.PositionProgress;
        var remaining = 1d - _playhead.OpacityProgress;

        // 七个字形先画，位于 X 与 く 的下层；边平移边淡出。
        if (foreground is not null && remaining > 0.001d)
        {
            using (context.PushOpacity(remaining))
            {
                foreach (var glyph in layout.FadingGlyphs)
                {
                    using (context.PushTransform(
                               Matrix.CreateTranslation(glyph.X + glyph.CollapseDx * positionProgress, glyph.Y)))
                    {
                        context.DrawGeometry(foreground, null, glyph.Geometry);
                    }
                }
            }
        }

        // X 不动。
        if (foreground is not null)
        {
            var anchor = layout.Anchor;
            using (context.PushTransform(Matrix.CreateTranslation(anchor.X, anchor.Y)))
            {
                context.DrawGeometry(foreground, null, anchor.Geometry);
            }
        }

        // く 只做水平平移，始终使用强调色，不参与淡出。
        if (accent is not null)
        {
            var kana = layout.Accent;
            using (context.PushTransform(
                       Matrix.CreateTranslation(kana.X + kana.CollapseDx * positionProgress, kana.Y)))
            {
                context.DrawGeometry(accent, null, kana.Geometry);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        // 触摸没有悬停，只有鼠标（和带悬停的笔）走这条路径。
        if (e.Pointer.Type == PointerType.Mouse) SetExpanded(true);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (e.Pointer.Type == PointerType.Mouse) SetExpanded(false);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        // 触摸 / 笔没有悬停，用点按在展开与收起之间切换。
        if (e.Pointer.Type == PointerType.Mouse) return;

        var expanding = _playhead.Target <= LogoPlayhead.ExpandedFrame + 1e-6d;
        SetExpanded(!expanding);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_playhead.IsMoving) StartAnimation();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _rafToken++;
        _rafPending = false;
        _lastTick = null;
    }

    /// <summary>直接跳到某一帧；演示与自测用。</summary>
    internal void SeekFrame(double frame)
    {
        _playhead.Seek(frame);
        InvalidateVisual();
        FrameAdvanced?.Invoke(_playhead.Frame);
    }

    private void SetExpanded(bool expanded)
    {
        _playhead.SetTarget(expanded ? LogoPlayhead.ExpandedFrame : LogoPlayhead.CollapsedFrame);
        if (_playhead.IsMoving) StartAnimation();
        else InvalidateVisual();
    }

    private void StartAnimation()
    {
        if (_rafPending) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        _rafPending = true;
        var token = ++_rafToken;
        try
        {
            topLevel.RequestAnimationFrame(time => OnAnimationTick(token, time));
        }
        catch (Exception ex)
        {
            _rafPending = false;
            Console.Error.WriteLine($"[XperiaFukuroLogo] RequestAnimationFrame 请求失败：{ex.Message}");
        }
    }

    private void OnAnimationTick(long token, TimeSpan time)
    {
        if (token != _rafToken) return;

        _rafPending = false;
        if (TopLevel.GetTopLevel(this) is null)
        {
            _lastTick = null;
            return;
        }

        var previous = _lastTick;
        _lastTick = time;
        if (previous is { } last)
        {
            var seconds = (time - last).TotalSeconds;
            if (seconds > 0d)
            {
                // 掉帧保护：一次最多推进 0.1 秒，避免窗口卡顿恢复后瞬移。
                if (seconds > 0.1d) seconds = 0.1d;
                _playhead.Advance(seconds);
            }
        }

        InvalidateVisual();
        FrameAdvanced?.Invoke(_playhead.Frame);

        if (_playhead.IsMoving) StartAnimation();
        else _lastTick = null;
    }

    private LogoLayout? EnsureLayout()
    {
        if (_layout is not null) return _layout;

        var size = FontSize;
        var family = FontFamily;
        if (!(size > 0d) || double.IsInfinity(size) || family is null) return null;

        try
        {
            _layout = LogoLayout.Build(family, size);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[XperiaFukuroLogo] 字形排版失败：{ex.Message}");
            return null;
        }

        return _layout;
    }
}
