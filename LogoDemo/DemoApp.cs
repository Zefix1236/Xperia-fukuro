using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace LogoDemo;

public sealed class DemoApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (Program.SequenceDirectory is { } sequenceDirectory)
        {
            var failures = FrameRenderer.RunSequence(sequenceDirectory);
            Console.Out.Flush();
            Environment.Exit(failures == 0 ? 0 : 1);
        }

        // 渲染检查同样需要平台初始化，跑完直接退出，不进消息循环。
        if (Program.FramesDirectory is { } framesDirectory)
        {
            Console.WriteLine("== 渲染检查 ==");
            var failures = FrameRenderer.Run(framesDirectory);
            Console.Out.Flush();
            Environment.Exit(failures == 0 ? 0 : 1);
        }

        // 排版自测需要字体管理器，此时平台已初始化；跑完直接退出，不进消息循环。
        if (Program.SelfTestMode)
        {
            var failures = SelfTest.RunLayout();
            Console.Out.Flush();
            Environment.Exit(failures == 0 ? 0 : 1);
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new DemoWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
