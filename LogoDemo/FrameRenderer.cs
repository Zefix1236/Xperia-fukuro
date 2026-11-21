using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PCL.UI.Controls.Branding;

namespace LogoDemo;

/// <summary>把 logo 在若干播放头位置渲染成 PNG，用于人眼检查和“不是空白”的自测。</summary>
internal static class FrameRenderer
{
    private static readonly double[] Frames = [0d, 4.5d, 9d, 13.5d, 18d];

    public static int Run(string directory)
    {
        Directory.CreateDirectory(directory);

        var accent = new SolidColorBrush(Color.Parse("#7FD3FF"));
        var control = new XperiaFukuroLogo
        {
            FontSize = 96d,
            Foreground = Brushes.White,
            Accent = accent
        };
        control.Measure(Size.Infinity);
        control.Arrange(new Rect(control.DesiredSize));

        var size = control.DesiredSize;
        var scale = 2d;
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(size.Width * scale)),
            Math.Max(1, (int)Math.Ceiling(size.Height * scale)));

        var failures = 0;
        foreach (var frame in Frames)
        {
            control.SeekFrame(frame);
            using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96d * scale, 96d * scale));
            bitmap.Render(control);

            var path = Path.Combine(directory, $"frame-{frame:00.0}.png");
            bitmap.Save(path);

            var length = new FileInfo(path).Length;
            if (length <= 0) failures++;
            Console.WriteLine($"  {Path.GetFileName(path),-16} {length,8} bytes");
        }

        Console.WriteLine($"  画布 {pixelSize.Width}×{pixelSize.Height}（{size.Width:F2}×{size.Height:F2} DIP @ {scale:0}x）");

        failures += RenderTitleBarStripes(directory, scale);
        if (failures > 0) Console.WriteLine($"  有 {failures} 帧渲染为空");

        return failures;
    }

    /// <summary>在 48 高的标题栏底色上渲染 24px 的展开 / 收起两态。</summary>
    private static int RenderTitleBarStripes(string directory, double scale)
    {
        var failures = 0;
        var accent = new SolidColorBrush(Color.Parse("#CFE8FF"));
        var gradient = new LinearGradientBrush
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

        foreach (var frame in new[] { 0d, 18d })
        {
            var logo = new XperiaFukuroLogo
            {
                FontSize = 24d,
                Foreground = Brushes.White,
                Accent = accent,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(19, 0, 0, 0)
            };
            logo.SeekFrame(frame);

            var strip = new Border
            {
                Width = 360d,
                Height = 48d,
                Background = gradient,
                Child = logo
            };
            strip.Measure(Size.Infinity);
            strip.Arrange(new Rect(strip.DesiredSize));

            var pixelSize = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(strip.DesiredSize.Width * scale)),
                Math.Max(1, (int)Math.Ceiling(strip.DesiredSize.Height * scale)));
            using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96d * scale, 96d * scale));
            bitmap.Render(strip);

            var path = Path.Combine(directory, $"titlebar-24px-{frame:00.0}.png");
            bitmap.Save(path);
            var length = new FileInfo(path).Length;
            if (length <= 0) failures++;
            Console.WriteLine($"  {Path.GetFileName(path),-24} {length,8} bytes");
        }

        return failures;
    }


    /// <summary>把整段动画（frame 0 → 18）逐帧渲染成 PNG 序列，用于合成 GIF。</summary>
    public static int RunSequence(string directory)
    {
        Directory.CreateDirectory(directory);

        var control = new XperiaFukuroLogo
        {
            FontSize = 96d,
            Foreground = Brushes.White,
            Accent = new SolidColorBrush(Color.Parse("#7FD3FF"))
        };
        control.Measure(Size.Infinity);
        control.Arrange(new Rect(control.DesiredSize));

        var size = control.DesiredSize;
        const double scale = 1.5d;
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(size.Width * scale)),
            Math.Max(1, (int)Math.Ceiling(size.Height * scale)));

        const int frameCount = 31; // 0.6 秒 × 50fps
        var failures = 0;
        for (var i = 0; i < frameCount; i++)
        {
            var frame = LogoPlayhead.CollapsedFrame * i / (frameCount - 1);
            control.SeekFrame(frame);

            using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96d * scale, 96d * scale));
            bitmap.Render(control);

            var path = Path.Combine(directory, $"seq-{i:00}.png");
            bitmap.Save(path);
            if (new FileInfo(path).Length <= 0) failures++;
        }

        Console.WriteLine($"  序列 {frameCount} 帧，{pixelSize.Width}×{pixelSize.Height}，输出 {directory}");
        return failures;
    }
}
