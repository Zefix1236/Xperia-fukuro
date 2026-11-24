using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Flasher.Core.Catalog;
using PCL.Core.App.Localization;
using PCL.UI.Controls;
using PCL.UI.Controls.SvgIcon;

namespace PCL.Views.Pages.Download;

/// <summary>XperiFirm 机型行：图标瓷砖、名称与标签、右侧收藏爱心和进入箭头。</summary>
internal sealed class XperiFirmModelRow : Border
{
    private readonly MyIconButton _favorite;
    private readonly MyIconButton _enter;
    private readonly Action<SonyProduct> _toggleFavorite;
    private SonyProduct _product;
    private bool _isFavorite;
    private bool _suppressOpen;

    public XperiFirmModelRow(
        SonyProduct product,
        bool isFavorite,
        bool isLatest,
        Action<SonyProduct> open,
        Action<SonyProduct> toggleFavorite)
    {
        _product = product;
        _isFavorite = isFavorite;
        _toggleFavorite = toggleFavorite;
        Height = 58d;
        Padding = new Thickness(8d, 0d, 8d, 0d);
        CornerRadius = new CornerRadius(6d);
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);

        var tile = new Border
        {
            Width = 40d,
            Height = 40d,
            CornerRadius = new CornerRadius(8d),
            Background = Brush("ColorBrush7", Brushes.White),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new SvgIcon
            {
                Icon = "lucide/smartphone",
                Width = 20d,
                Height = 20d,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IconBrush = Brush("ColorBrush2", Brushes.Gray)
            }
        };

        var tags = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4d,
            Margin = new Thickness(0d, 3d, 0d, 0d)
        };
        tags.Children.Add(CreateTag(product.ProductCode, false));
        if (product.ReleaseYear > 0)
            tags.Children.Add(CreateTag(product.ReleaseYear.ToString(), false));
        if (isLatest)
            tags.Children.Add(CreateTag(Lang.Text("Firmware.Tag.Latest"), true));

        var text = new StackPanel
        {
            Spacing = 0d,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10d, 0d, 8d, 0d),
            Children =
            {
                new TextBlock
                {
                    Text = product.MarketingName,
                    FontSize = 14d,
                    Foreground = Brush("ColorBrush1", Brushes.Black),
                    TextTrimming = TextTrimming.CharacterEllipsis
                },
                tags
            }
        };

        _favorite = new MyIconButton
        {
            SvgIcon = isFavorite ? "lucide/heart-filled" : "lucide/heart",
            Theme = isFavorite ? MyIconButton.Themes.Red : MyIconButton.Themes.Color,
            Width = 26d,
            Height = 26d,
            Opacity = isFavorite ? 1d : 0d,
            IsHitTestVisible = isFavorite
        };
        _favorite.Click += (_, _) =>
        {
            _suppressOpen = true;
            Dispatcher.UIThread.Post(() => _suppressOpen = false);
            _toggleFavorite(_product);
        };
        ToolTip.SetTip(_favorite, Lang.Text("Firmware.Favorite.Tip"));

        _enter = new MyIconButton
        {
            SvgIcon = "lucide/chevron-right",
            Theme = MyIconButton.Themes.Color,
            Width = 22d,
            Height = 22d,
            Opacity = 0d,
            IsHitTestVisible = false
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2d,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _favorite, _enter }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };
        grid.Children.Add(tile);
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        Grid.SetColumn(actions, 2);
        grid.Children.Add(actions);
        Child = grid;

        PointerEntered += (_, _) =>
        {
            Background = Brush("ColorBrushBg1", Brushes.Transparent);
            _favorite.Opacity = 1d;
            _favorite.IsHitTestVisible = true;
            _enter.Opacity = 1d;
        };
        PointerExited += (_, _) =>
        {
            Background = Brushes.Transparent;
            _favorite.Opacity = _isFavorite ? 1d : 0d;
            _favorite.IsHitTestVisible = _isFavorite;
            _enter.Opacity = 0d;
        };
        PointerReleased += (_, _) =>
        {
            if (!_suppressOpen)
                open(_product);
        };
    }

    public SonyProduct Product => _product;

    public void SetFavorite(bool isFavorite)
    {
        _isFavorite = isFavorite;
        _favorite.SvgIcon = isFavorite ? "lucide/heart-filled" : "lucide/heart";
        _favorite.Theme = isFavorite ? MyIconButton.Themes.Red : MyIconButton.Themes.Color;
        _favorite.Opacity = isFavorite || IsPointerOver ? 1d : 0d;
        _favorite.IsHitTestVisible = isFavorite || IsPointerOver;
    }

    private Border CreateTag(string text, bool highlight)
    {
        return new Border
        {
            Height = 18d,
            Padding = new Thickness(5d, 0d, 5d, 0d),
            CornerRadius = new CornerRadius(3d),
            Background = highlight
                ? Brush("ColorBrush4", Brushes.DodgerBlue)
                : Brush("ColorBrush7", Brushes.White),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11d,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = highlight
                    ? Brushes.White
                    : Brush("ColorBrush1", Brushes.Gray),
                Opacity = highlight ? 1d : 0.65d
            }
        };
    }

    private static IBrush Brush(string key, IBrush fallback)
    {
        return Application.Current is { } app
            && app.TryFindResource(key, out var resource)
            && resource is IBrush brush
                ? brush
                : fallback;
    }
}
