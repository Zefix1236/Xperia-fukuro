using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Flasher.Core.Catalog;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.UI.Controls;
using PCL.UI.Controls.SvgIcon;

namespace PCL.Views.Pages.Download;

/// <summary>XperiFirm 一级页：搜索、收藏夹、按系列折叠的机型列表。</summary>
public sealed class PageFirmwareDownload : MyPageRight
{
    private readonly SonyCatalog _catalog = SonyCatalog.LoadEmbedded();
    private readonly TextBox _searchBox;
    private readonly TextBlock _searchPlaceholder;
    private readonly Border _searchBorder;
    private readonly MyIconButton _clearButton;
    private readonly Button _yearButton;
    private readonly TextBlock _yearText;
    private readonly MyCard _favoritesCard;
    private readonly StackPanel _favoritesPanel;
    private readonly TextBlock _favoritesEmpty;
    private readonly TextBlock _noMatch;
    private readonly List<SeriesCard> _series = [];
    private readonly Dictionary<string, XperiFirmModelRow> _seriesRows = [];
    private readonly HashSet<string> _favorites = [];
    private readonly HashSet<string> _latestCodes = [];
    private readonly List<int> _years;
    private TopLevel? _topLevel;
    private int _year;

    public PageFirmwareDownload()
    {
        LoadFavorites();
        ComputeLatest();

        _years = _catalog.Products
            .Select(product => product.ReleaseYear)
            .Where(year => year > 0)
            .Distinct()
            .OrderByDescending(year => year)
            .ToList();

        _searchBox = new TextBox
        {
            FontSize = 16d,
            Height = 38d,
            MinHeight = 0d,
            Padding = new Thickness(0d),
            BorderThickness = new Thickness(0d),
            BorderBrush = Brushes.Transparent,
            Background = Brushes.Transparent,
            Foreground = Brush("ColorBrush1", Brushes.Black),
            CaretBrush = Brush("ColorBrush1", Brushes.Black),
            VerticalContentAlignment = VerticalAlignment.Center,
            FocusAdorner = null
        };
        _searchBox.Classes.Add("SearchInput");
        _searchPlaceholder = new TextBlock
        {
            Text = Lang.Text("Firmware.Search.Hint"),
            FontSize = 16d,
            Foreground = Brush("ColorBrushGray4", Brushes.Gray),
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        _clearButton = new MyIconButton
        {
            SvgIcon = "lucide/x",
            Theme = MyIconButton.Themes.Color,
            Width = 22d,
            Height = 22d,
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4d, 0d, 8d, 0d)
        };
        ToolTip.SetTip(_clearButton, Lang.Text("Firmware.Search.Clear"));
        _clearButton.Click += (_, _) =>
        {
            _searchBox.Text = "";
            _searchBox.Focus();
        };

        _yearText = new TextBlock
        {
            Text = FormatYear(),
            FontSize = 13d,
            Foreground = Brush("ColorBrush1", Brushes.Black),
            VerticalAlignment = VerticalAlignment.Center
        };
        var yearArrow = new SvgIcon
        {
            Icon = "lucide/chevron-right",
            Width = 16d,
            Height = 16d,
            IconBrush = Brush("ColorBrushGray4", Brushes.Gray),
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = new RotateTransform(90d),
            RenderTransformOrigin = new RelativePoint(0.5d, 0.5d, RelativeUnit.Relative)
        };
        _yearButton = new Button
        {
            Background = Brushes.Transparent,
            BorderBrush = Brush("ColorBrush6", new SolidColorBrush(Color.Parse("#D5DEEA"))),
            BorderThickness = new Thickness(1d),
            CornerRadius = new CornerRadius(6d),
            Padding = new Thickness(10d, 5d, 10d, 5d),
            Height = 32d,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            FocusAdorner = null,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4d,
                Children = { _yearText, yearArrow }
            }
        };
        _yearButton.Flyout = CreateYearFlyout();

        var searchIcon = new SvgIcon
        {
            Icon = "lucide/search",
            Width = 22d,
            Height = 22d,
            IconBrush = Brush("ColorBrushGray4", Brushes.Gray),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0d, 0d, 8d, 0d)
        };
        var searchGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto")
        };
        searchGrid.Children.Add(searchIcon);
        Grid.SetColumn(_searchBox, 1);
        searchGrid.Children.Add(_searchBox);
        Grid.SetColumn(_searchPlaceholder, 1);
        searchGrid.Children.Add(_searchPlaceholder);
        Grid.SetColumn(_clearButton, 2);
        searchGrid.Children.Add(_clearButton);
        Grid.SetColumn(_yearButton, 3);
        searchGrid.Children.Add(_yearButton);

        _searchBorder = new Border
        {
            BorderThickness = new Thickness(0d),
            BorderBrush = Brushes.Transparent,
            CornerRadius = new CornerRadius(8d),
            Padding = new Thickness(16d, 5d, 16d, 5d),
            Child = searchGrid
        };
        var searchCard = new MyCard
        {
            Margin = new Thickness(20d, 20d, 20d, 0d),
            Height = 48d,
            CornerRadius = new CornerRadius(8d),
            UseAnimation = false
        };
        searchCard.Children.Add(_searchBorder);

        _favoritesPanel = new StackPanel { Spacing = 2d };
        _favoritesEmpty = new TextBlock
        {
            Text = Lang.Text("Firmware.Favorites.Empty"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12d,
            Opacity = 0.65d,
            Margin = new Thickness(0d, 4d, 0d, 4d)
        };
        var favoritesContent = new StackPanel
        {
            Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 12d),
            Spacing = 2d,
            Children = { _favoritesPanel, _favoritesEmpty }
        };
        _favoritesCard = new MyCard
        {
            Margin = new Thickness(20d, 15d, 20d, 0d),
            CanSwap = true,
            SwapControl = favoritesContent
        };
        _favoritesCard.Children.Add(favoritesContent);
        _favoritesCard.IsSwapped = false;
        ApplyTitleFont(_favoritesCard);
        SetFavoritesTitle(0);

        var content = new StackPanel
        {
            Margin = new Thickness(5d, 5d, 5d, 25d)
        };
        content.Children.Add(searchCard);
        content.Children.Add(_favoritesCard);

        foreach (var (series, titleKey, descriptionKey) in new[]
                 {
                     ("1", "Download.Firmware.Series1", "Firmware.Series.Description.1"),
                     ("5", "Download.Firmware.Series5", "Firmware.Series.Description.5"),
                     ("10", "Download.Firmware.Series10", "Firmware.Series.Description.10")
                 })
        {
            var products = _catalog.Products
                .Where(product => string.Equals(product.Series, series, StringComparison.Ordinal))
                .OrderBy(product => product.ReleaseYear)
                .ThenBy(product => product.MarketingName, StringComparer.Ordinal)
                .ToList();
            if (products.Count == 0)
                continue;

            var panel = new StackPanel
            {
                Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 12d),
                Spacing = 2d
            };
            var card = new MyCard
            {
                Margin = new Thickness(20d, 15d, 20d, 0d),
                CanSwap = true,
                SwapControl = panel
            };
            card.Children.Add(panel);
            card.IsSwapped = true;
            ApplyTitleFont(card);
            _series.Add(new SeriesCard(series, titleKey, descriptionKey, products, card, panel));
            content.Children.Add(card);
        }

        _noMatch = new TextBlock
        {
            Text = Lang.Text("Firmware.NoMatch"),
            FontSize = 12d,
            Opacity = 0.65d,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(20d, 20d, 20d, 0d),
            IsVisible = false
        };
        content.Children.Add(_noMatch);

        var scroll = new MyScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
        Child = scroll;
        PanScroll = scroll;

        _searchBox.TextChanged += (_, _) =>
        {
            _clearButton.IsVisible = !string.IsNullOrEmpty(_searchBox.Text);
            _searchPlaceholder.IsVisible = string.IsNullOrEmpty(_searchBox.Text);
            ApplyFilter();
        };
        ApplyFilter();
    }

    public event Action<SonyProduct>? ProductSelected;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(
            InputElement.KeyDownEvent,
            OnPageKeyDown,
            RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(InputElement.KeyDownEvent, OnPageKeyDown);
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnPageKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsEffectivelyVisible || e.Handled)
            return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)
            || e.KeyModifiers.HasFlag(KeyModifiers.Alt)
            || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            return;

        var focused = _topLevel?.FocusManager?.GetFocusedElement();
        if (focused is TextBox)
            return;

        var character = KeyToCharacter(e.Key, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        if (!character.HasValue)
            return;

        _searchBox.Focus();
        _searchBox.Text = (_searchBox.Text ?? string.Empty) + character.Value;
        _searchBox.CaretIndex = _searchBox.Text.Length;
        e.Handled = true;
    }

    private static char? KeyToCharacter(Key key, bool shift)
    {
        var code = (int)key;
        if (code >= (int)Key.A && code <= (int)Key.Z)
        {
            var letter = (char)('a' + code - (int)Key.A);
            return shift ? char.ToUpperInvariant(letter) : letter;
        }

        if (code >= (int)Key.D0 && code <= (int)Key.D9)
        {
            var index = code - (int)Key.D0;
            return shift ? ")!@#$%^&*("[index] : (char)('0' + index);
        }

        if (code >= (int)Key.NumPad0 && code <= (int)Key.NumPad9)
            return (char)('0' + code - (int)Key.NumPad0);

        return key switch
        {
            Key.OemMinus => shift ? '_' : '-',
            Key.OemPlus => shift ? '+' : '=',
            Key.OemComma => shift ? '<' : ',',
            Key.OemPeriod => shift ? '>' : '.',
            Key.OemQuestion => shift ? '?' : '/',
            Key.OemSemicolon => shift ? ':' : ';',
            Key.OemQuotes => shift ? '"' : '\'',
            Key.OemOpenBrackets => shift ? '{' : '[',
            Key.OemCloseBrackets => shift ? '}' : ']',
            Key.OemPipe => shift ? '|' : '\\',
            Key.OemTilde => shift ? '~' : '`',
            _ => null
        };
    }

    private MenuFlyout CreateYearFlyout()
    {
        var flyout = new MenuFlyout();
        var all = new MenuItem { Header = Lang.Text("Firmware.Year.All") };
        all.Click += (_, _) => SetYear(0);
        flyout.Items.Add(all);
        foreach (var year in _years)
        {
            var item = new MenuItem { Header = year.ToString() };
            item.Click += (_, _) => SetYear(year);
            flyout.Items.Add(item);
        }

        return flyout;
    }

    private void SetYear(int year)
    {
        _year = year;
        _yearText.Text = FormatYear();
        ApplyFilter();
    }

    private string FormatYear()
    {
        var value = _year == 0
            ? Lang.Text("Firmware.Year.All")
            : _year.ToString();
        return Lang.Text("Firmware.Year.Format", value);
    }

    private void ApplyFilter()
    {
        var filtering = IsFiltering();
        var anySeries = false;

        foreach (var series in _series)
        {
            var visible = series.Products.Where(Matches).ToList();
            series.Panel.Children.Clear();
            foreach (var product in visible)
                series.Panel.Children.Add(GetOrCreateSeriesRow(product));

            series.Card.Title = BuildSeriesTitle(series, visible.Count);
            series.Card.IsVisible = visible.Count > 0;
            series.Card.IsSwapped = !filtering;
            anySeries |= visible.Count > 0;
        }

        var favoriteCount = RebuildFavorites();
        _noMatch.IsVisible = filtering && !anySeries && favoriteCount == 0;
    }

    private int RebuildFavorites()
    {
        var visible = Favorites()
            .Where(Matches)
            .OrderBy(product => SeriesOrder(product.Series))
            .ThenBy(product => product.ReleaseYear)
            .ThenBy(product => product.MarketingName, StringComparer.Ordinal)
            .ToList();

        _favoritesPanel.Children.Clear();
        foreach (var product in visible)
        {
            _favoritesPanel.Children.Add(
                new XperiFirmModelRow(
                    product,
                    true,
                    _latestCodes.Contains(product.ProductCode),
                    OpenProduct,
                    ToggleFavorite));
        }

        _favoritesEmpty.IsVisible = visible.Count == 0;
        _favoritesEmpty.Text = _favorites.Count == 0
            ? Lang.Text("Firmware.Favorites.Empty")
            : Lang.Text("Firmware.NoMatch");
        SetFavoritesTitle(visible.Count);
        return visible.Count;
    }

    private IEnumerable<SonyProduct> Favorites()
    {
        return _catalog.Products.Where(product => _favorites.Contains(product.ProductCode));
    }

    private XperiFirmModelRow GetOrCreateSeriesRow(SonyProduct product)
    {
        if (_seriesRows.TryGetValue(product.ProductCode, out var row))
            return row;

        row = new XperiFirmModelRow(
            product,
            _favorites.Contains(product.ProductCode),
            _latestCodes.Contains(product.ProductCode),
            OpenProduct,
            ToggleFavorite);
        _seriesRows[product.ProductCode] = row;
        return row;
    }

    private void ToggleFavorite(SonyProduct product)
    {
        if (!_favorites.Remove(product.ProductCode))
            _favorites.Add(product.ProductCode);
        SaveFavorites();

        foreach (var row in _seriesRows.Values)
            row.SetFavorite(_favorites.Contains(row.Product.ProductCode));
        RebuildFavorites();
    }

    private void OpenProduct(SonyProduct product)
    {
        ProductSelected?.Invoke(product);
        LauncherCommon.Log($"XperiFirm 选择机型：{product.MarketingName} - {product.ProductCode}");
        // TODO: 二级页面完成后在这里跳转。
    }

    private bool Matches(SonyProduct product)
    {
        if (_year > 0 && product.ReleaseYear != _year)
            return false;

        var keyword = _searchBox.Text?.Trim();
        if (string.IsNullOrEmpty(keyword))
            return true;

        return product.MarketingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || product.ProductCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || product.Models.Any(model =>
                model.ModelName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsFiltering()
    {
        return _year > 0
            || !string.IsNullOrWhiteSpace(_searchBox.Text);
    }

    private static void ApplyTitleFont(MyCard card)
    {
        // PCL English 没有真 Bold，合成粗体会让 X 字符产生彩色边缘；卡片标题改用系统真 Bold。
        card.MainTextBlock.FontFamily = new FontFamily(
            "Noto Sans CJK SC, Source Han Sans SC, Microsoft YaHei UI, Segoe UI, PingFang SC, Noto Sans");
        card.MainTextBlock.FontSize = 13d;
        card.MainTextBlock.FontWeight = FontWeight.Bold;
    }

    private void SetFavoritesTitle(int count)
    {
        _favoritesCard.Title = $"{Lang.Text("Firmware.Favorites")}  {count}";
    }

    private string BuildSeriesTitle(SeriesCard series, int count)
    {
        return $"{Lang.Text(series.TitleKey)} · {Lang.Text(series.DescriptionKey)}"
            + $"  {Lang.Text("Firmware.Model.Count", count)}";
    }

    private void ComputeLatest()
    {
        foreach (var group in _catalog.Products.GroupBy(product => product.Series))
        {
            var latestYear = group.Max(product => product.ReleaseYear);
            foreach (var product in group.Where(product => product.ReleaseYear == latestYear))
                _latestCodes.Add(product.ProductCode);
        }
    }

    private void LoadFavorites()
    {
        try
        {
            var raw = Config.Preference.FirmwareFavorites;
            if (string.IsNullOrWhiteSpace(raw))
                return;
            foreach (var code in raw.Split(
                         ',',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                _favorites.Add(code);
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, "读取固件收藏夹失败");
        }
    }

    private void SaveFavorites()
    {
        try
        {
            Config.Preference.FirmwareFavorites = string.Join(
                ",",
                _favorites.OrderBy(code => code, StringComparer.Ordinal));
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, "保存固件收藏夹失败");
        }
    }

    private static IBrush Brush(string key, IBrush fallback)
    {
        return Application.Current is { } app
            && app.TryFindResource(key, out var resource)
            && resource is IBrush brush
                ? brush
                : fallback;
    }

    private static int SeriesOrder(string series)
    {
        return series switch
        {
            "1" => 0,
            "5" => 1,
            "10" => 2,
            _ => 3
        };
    }

    private sealed record SeriesCard(
        string Series,
        string TitleKey,
        string DescriptionKey,
        List<SonyProduct> Products,
        MyCard Card,
        StackPanel Panel);
}
