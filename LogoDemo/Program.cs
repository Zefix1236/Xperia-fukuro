using Avalonia;

namespace LogoDemo;

internal static class Program
{
    /// <summary>是否运行自测（--selftest），此时不打开演示窗口。</summary>
    internal static bool SelfTestMode { get; private set; }

    /// <summary>--frames 模式：把若干帧渲染成 PNG，这里存输出目录；为 null 表示不渲染。</summary>
    internal static string? FramesDirectory { get; private set; }

    /// <summary>--sequence 模式：把整段动画逐帧渲染成 PNG 序列（做 GIF 用）。</summary>
    internal static string? SequenceDirectory { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        SelfTestMode = args.Contains("--selftest");

        var framesArgument = args.FirstOrDefault(arg =>
            arg == "--frames" || arg.StartsWith("--frames=", StringComparison.Ordinal));
        FramesDirectory = framesArgument switch
        {
            null => null,
            "--frames" => "logo-frames",
            _ => framesArgument[(framesArgument.IndexOf('=') + 1)..]
        };

        if (SelfTestMode)
        {
            var failures = SelfTest.RunMath();
            if (failures > 0)
            {
                Console.WriteLine($"播放头自测失败 {failures} 项");
                return 1;
            }
        }

        var sequenceArgument = args.FirstOrDefault(arg =>
            arg == "--sequence" || arg.StartsWith("--sequence=", StringComparison.Ordinal));
        SequenceDirectory = sequenceArgument switch
        {
            null => null,
            "--sequence" => "logo-sequence",
            _ => sequenceArgument[(sequenceArgument.IndexOf('=') + 1)..]
        };

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return SelfTestMode ? SelfTest.Failures : 0;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<DemoApp>().UsePlatformDetect().LogToTrace();
}
