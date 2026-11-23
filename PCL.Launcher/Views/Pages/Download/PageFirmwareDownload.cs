using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Flasher.Core.Catalog;
using Flasher.Core.Sony;
using PCL.Core.App.Localization;
using PCL.UI.Controls;

namespace PCL.Views.Pages.Download;

/// <summary>XperiFirm 固件页：按 Xperia 1 / 5 / 10 分类折叠显示机型。</summary>
public sealed class PageFirmwareDownload : MyPageRight
{
    private readonly SonyCatalog _catalog = SonyCatalog.LoadEmbedded();

    public PageFirmwareDownload()
    {
        var content = new StackPanel
        {
            Margin = new Thickness(5d, 5d, 5d, 25d)
        };

        foreach (var (series, titleKey) in new[]
                 {
                     ("1", "Download.Firmware.Series1"),
                     ("5", "Download.Firmware.Series5"),
                     ("10", "Download.Firmware.Series10")
                 })
        {
            var products = _catalog.Products
                .Where(product => string.Equals(product.Series, series, StringComparison.Ordinal))
                .OrderBy(product => GenerationOrder(product.Generation))
                .ToList();
            if (products.Count == 0)
                continue;

            content.Children.Add(CreateSeriesCard(titleKey, products));
        }

        var scroll = new MyScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
        Child = scroll;
        PanScroll = scroll;
    }

    private static MyCard CreateSeriesCard(string titleKey, IReadOnlyList<SonyProduct> products)
    {
        var rows = new StackPanel
        {
            Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 12d),
            Spacing = 2d
        };
        foreach (var product in products)
        {
            rows.Children.Add(new MyListItem
            {
                Title = product.MarketingName,
                Info = product.ProductCode,
                SvgIcon = "lucide/smartphone",
                Type = MyListItem.CheckType.Clickable,
                Height = 42d,
                IsScaleAnimationEnabled = false
            });
        }

        var card = new MyCard
        {
            Title = Lang.Text(titleKey),
            Margin = new Thickness(20d, 15d, 20d, 0d),
            CanSwap = true,
            SwapControl = rows
        };
        card.Children.Add(rows);
        card.IsSwapped = true;
        // PCL English 没有真 Bold，合成粗体会让斜线产生彩色边缘；折叠标题改用系统真 Bold。
        card.MainTextBlock.FontFamily = new FontFamily(
            "Noto Sans CJK SC, Source Han Sans SC, Microsoft YaHei UI, Segoe UI, PingFang SC, Noto Sans");
        card.MainTextBlock.FontSize = 13d;
        card.MainTextBlock.FontWeight = FontWeight.Bold;
        return card;
    }

    private static int GenerationOrder(string generation)
    {
        return generation switch
        {
            "III" => 0,
            "IV" => 1,
            "V" => 2,
            "VI" => 3,
            "VII" => 4,
            "VIII" => 5,
            _ => 6
        };
    }
}
