using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PCL.UI.Controls.Branding;

namespace LogoDemo;

internal sealed class DemoWindow : Window
{
    private static readonly IBrush PageBackground = new SolidColorBrush(Color.Parse("#0D1526"));

    private static readonly IBrush AccentBrush = Brushes.White;

    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#8FB6E6"));

    private static readonly IBrush TitleBarBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0d, 0.5d, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1d, 0.5d, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#013E7D"), 0d),
            new GradientStop(Color.Parse("#0569C9"), 0.55d),
            new GradientStop(Color.Parse("#0A8AF0"), 1d)
        }
    };

    public DemoWindow()
    {
        Title = "XperiaFukuroLogo · 演示";
        Width = 900;
        Height = 620;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = PageBackground;
        Content = BuildContent();
    }

    private static Control BuildContent()
    {
        var readout = new TextBlock
        {
            FontSize = 13,
            Foreground = MutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "frame —"
        };

        var big = new XperiaFukuroLogo
        {
            FontSize = 96d,
            Foreground = Brushes.White,
            Accent = AccentBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        big.FrameAdvanced += frame =>
        {
            var pe = LogoPlayhead.Ease(frame / 18d);
            var po = LogoPlayhead.Ease(Math.Min(frame / 9d, 1d));
            readout.Text = $"frame {frame,5:F2} / 18      pe {pe:F3}      po {po:F3}";
        };

        var bigCard = new Border
        {
            Height = 200,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.Parse("#111C30")),
            Child = big
        };

        // 模拟真实标题栏：悬停区域是整条 48px 高的条，不是 logo 自己。
        var titleLogo = new XperiaFukuroLogo
        {
            FontSize = 24d,
            Foreground = Brushes.White,
            Accent = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            FollowPointer = false
        };
        var titleBarPreview = new Border
        {
            Height = 48,
            CornerRadius = new CornerRadius(8),
            Background = TitleBarBrush,
            Child = new Grid
            {
                Children =
                {
                    titleLogo,
                    new TextBlock
                    {
                        Text = "整条标题栏悬停都展开",
                        Margin = new Thickness(240, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.Parse("#D8EBFF"))
                    }
                }
            }
        };
        titleBarPreview.PointerEntered += (_, e) =>
        {
            if (e.Pointer.Type == PointerType.Mouse) titleLogo.Expand();
        };
        titleBarPreview.PointerExited += (_, e) =>
        {
            if (e.Pointer.Type == PointerType.Mouse) titleLogo.Collapse();
        };

        var sizes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 36,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        foreach (var size in new double[] { 48d, 32d, 24d })
        {
            sizes.Children.Add(new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new XperiaFukuroLogo
                    {
                        FontSize = size,
                        Foreground = Brushes.White,
                        Accent = AccentBrush,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"{size:F0}px",
                        FontSize = 12,
                        Foreground = MutedBrush,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            });
        }

        return new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(32, 24, 32, 24),
            Children =
            {
                new TextBlock
                {
                    Text = "XperiaFukuroLogo",
                    FontSize = 22,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = "九个字形 X P E R I A ふ < ろ；收起后只留 X 和自绘的 < 。大 logo 悬停展开、移出收起；下面那条模拟标题栏是整条悬停触发。",
                    FontSize = 13,
                    Foreground = MutedBrush,
                    TextWrapping = TextWrapping.Wrap
                },
                bigCard,
                readout,
                titleBarPreview,
                sizes,
                new TextBlock
                {
                    Text = "自测要点：在最大那个 logo 上来回快速移动鼠标，观察 frame 读数是否连续（每帧最多推进 30×dt），字形不应跳变或闪烁。",
                    FontSize = 12,
                    Foreground = MutedBrush,
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
    }
}
