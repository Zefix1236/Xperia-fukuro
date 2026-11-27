using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Flasher.Core.Catalog;
using Flasher.Core.Download;
using Flasher.Core.Sony;
using PCL.Core.App.Localization;
using PCL.UI.Controls;

namespace PCL.Views.Pages.Download;

/// <summary>XperiFirm 二级页：选择区域型号、查询版本、勾选文件并下载。</summary>
public sealed class FirmwareDownloadDetailView : UserControl
{
    private readonly SonyProduct _product;
    private readonly Action _back;
    private readonly MyComboBox _modelBox = new();
    private readonly MyButton _queryButton;
    private readonly MyButton _downloadButton;
    private readonly MyButton _cancelButton;
    private readonly MyTextBox _outputBox = new();
    private readonly StackPanel _versionPanel = new() { Spacing = 2 };
    private readonly StackPanel _filePanel = new() { Spacing = 2 };
    private readonly TextBlock _statusText;
    private readonly ProgressBar _progressBar;
    private readonly List<(SonyServiceInfo Service, MyListItem Item)> _versionItems = [];
    private readonly List<(SonyFileResource File, CheckBox Check)> _fileItems = [];

    private SonyModel? _model;
    private SonyServiceInfo? _selectedVersion;
    private SonySoftwareService? _service;
    private CancellationTokenSource? _cancellation;
    private bool _busy;
    private bool _suppressSelection;
    private bool _settingOutput;
    private bool _outputEdited;

    public FirmwareDownloadDetailView(SonyProduct product, Action back)
    {
        _product = product;
        _back = back;

        _queryButton = new MyButton
        {
            Text = Lang.Text("Firmware.Query"),
            ColorType = MyButton.ColorState.Highlight,
            MinWidth = 120d,
            Height = 32d,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _queryButton.Click += (_, _) => _ = QueryAsync();

        _downloadButton = new MyButton
        {
            Text = Lang.Text("Firmware.Download"),
            ColorType = MyButton.ColorState.Highlight,
            MinWidth = 140d,
            Height = 32d,
            IsEnabled = false
        };
        _downloadButton.Click += (_, _) => _ = DownloadAsync();

        _cancelButton = new MyButton
        {
            Text = Lang.Text("Common.Action.Cancel"),
            ColorType = MyButton.ColorState.Red,
            MinWidth = 100d,
            Height = 32d,
            IsEnabled = false
        };
        _cancelButton.Click += (_, _) => _cancellation?.Cancel();

        _statusText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12d,
            Opacity = 0.75d
        };
        _progressBar = new ProgressBar
        {
            Minimum = 0d,
            Maximum = 1d,
            Value = 0d,
            Height = 6d
        };

        _modelBox.SelectionChanged += (_, _) => OnModelChanged();
        _modelBox.MinWidth = 340d;
        _modelBox.Height = 32d;
        _modelBox.HorizontalAlignment = HorizontalAlignment.Left;
        _outputBox.Height = 32d;
        _outputBox.TextChanged += (_, _) =>
        {
            if (!_settingOutput)
                _outputEdited = true;
        };

        var content = BuildContent();
        var scroll = new MyScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
        Scroll = scroll;
        Content = scroll;

        LoadModels();
    }

    public MyScrollViewer Scroll { get; }

    public void Cancel()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    private Control BuildContent()
    {
        var root = new StackPanel
        {
            Margin = new Thickness(5d, 5d, 5d, 25d),
            Spacing = 12d
        };

        var header = new MyCard
        {
            Margin = new Thickness(20d, 20d, 20d, 0d),
            UseAnimation = false
        };
        var headerContent = new StackPanel
        {
            Margin = new Thickness(18d, 16d, 18d, 16d),
            Spacing = 6d
        };
        var backButton = new MyButton
        {
            Text = Lang.Text("Common.Action.Back"),
            ColorType = MyButton.ColorState.Normal,
            Height = 30d,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        backButton.Click += (_, _) =>
        {
            Cancel();
            _back();
        };
        headerContent.Children.Add(backButton);
        headerContent.Children.Add(new TextBlock
        {
            Text = _product.MarketingName,
            FontSize = 18d,
            FontWeight = FontWeight.SemiBold
        });
        headerContent.Children.Add(new TextBlock
        {
            Text = $"{_product.ProductCode} · {_product.ReleaseYear}",
            FontSize = 12d,
            Opacity = 0.65d
        });
        header.Children.Add(headerContent);
        root.Children.Add(header);

        var card = new MyCard
        {
            Margin = new Thickness(20d, 15d, 20d, 0d)
        };
        var content = new StackPanel
        {
            Margin = new Thickness(20d, 40d, 20d, 20d),
            Spacing = 10d
        };
        card.Children.Add(content);
        content.Children.Add(Label("Firmware.Model"));
        content.Children.Add(_modelBox);
        content.Children.Add(_queryButton);
        content.Children.Add(Label("Firmware.Version"));
        content.Children.Add(_versionPanel);
        content.Children.Add(Label("Firmware.Files"));
        content.Children.Add(_filePanel);
        content.Children.Add(Label("Firmware.Output"));
        content.Children.Add(_outputBox);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10d,
            Margin = new Thickness(0d, 4d, 0d, 0d)
        };
        actions.Children.Add(_downloadButton);
        actions.Children.Add(_cancelButton);
        content.Children.Add(actions);
        content.Children.Add(_progressBar);
        content.Children.Add(_statusText);
        root.Children.Add(card);

        return root;
    }

    private static TextBlock Label(string key)
    {
        return new TextBlock
        {
            Text = Lang.Text(key),
            FontSize = 13d,
            Opacity = 0.7d,
            Margin = new Thickness(0d, 4d, 0d, 0d)
        };
    }

    private void LoadModels()
    {
        _suppressSelection = true;
        try
        {
            _modelBox.ItemsSource = _product.Models
                .Select(model => model.HasSpc
                    ? model.DisplayName
                    : $"{model.DisplayName} · {Lang.Text("Firmware.NoSpc")}")
                .ToList();
            _modelBox.SelectedIndex = _product.Models.Count > 0 ? 0 : -1;
        }
        finally
        {
            _suppressSelection = false;
        }

        OnModelChanged();
    }

    private void OnModelChanged()
    {
        if (_suppressSelection)
            return;
        var index = _modelBox.SelectedIndex;
        if (index < 0 || index >= _product.Models.Count)
            return;

        _model = _product.Models[index];
        _selectedVersion = null;
        _service = null;
        _outputEdited = false;
        _versionItems.Clear();
        _fileItems.Clear();
        _versionPanel.Children.Clear();
        _filePanel.Children.Clear();
        _progressBar.Value = 0d;
        _statusText.Text = _model.HasSpc
            ? Lang.Text("Firmware.SelectHint")
            : Lang.Text("Firmware.NoSpc");
        _queryButton.IsEnabled = _model.HasSpc && !_busy;
        _downloadButton.IsEnabled = false;
    }

    private async Task QueryAsync()
    {
        if (_busy || _model is null)
            return;
        if (!_model.HasSpc)
        {
            _statusText.Text = Lang.Text("Firmware.NoSpc");
            return;
        }

        ResetCancellation();
        SetBusy(true);
        _versionItems.Clear();
        _fileItems.Clear();
        _versionPanel.Children.Clear();
        _filePanel.Children.Clear();
        _service = null;
        _selectedVersion = null;
        _downloadButton.IsEnabled = false;
        _progressBar.Value = 0d;
        _statusText.Text = Lang.Text("Firmware.Loading");

        try
        {
            using var api = new SonyApiClient();
            var selection = SonyDeviceSelection.From(_product, _model);
            var match = await api.MatchAsync(selection, _cancellation!.Token);
            if (!string.Equals(match.DeviceProblem, "NO_PROBLEM", StringComparison.Ordinal))
            {
                _statusText.Text = Lang.Text("Firmware.Error", match.DeviceProblem);
                return;
            }

            var versions = Deduplicate(match.Services);
            if (versions.Count == 0)
            {
                _statusText.Text = Lang.Text("Firmware.NoVersions");
                return;
            }

            foreach (var version in versions)
            {
                var item = new MyListItem
                {
                    Title = version.SoftwareVersion,
                    Info = $"{Lang.Text("Firmware.Android", version.AndroidVersion)}"
                        + $" · {version.AndroidVersionUpdateType} · {FormatLogicType(version.LogicType)}",
                    Type = MyListItem.CheckType.Clickable,
                    Height = 44d,
                    IsScaleAnimationEnabled = false
                };
                item.Click += (_, _) => _ = LoadServiceAsync(version);
                _versionItems.Add((version, item));
                _versionPanel.Children.Add(item);
            }

            _statusText.Text = $"{Lang.Text("Firmware.Version")} · {versions.Count}";
        }
        catch (OperationCanceledException)
        {
            _statusText.Text = Lang.Text("Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadServiceAsync(SonyServiceInfo version)
    {
        if (_busy || version.SelfLink is null)
            return;

        ResetCancellation();
        SetBusy(true);
        _selectedVersion = version;
        _service = null;
        _filePanel.Children.Clear();
        _downloadButton.IsEnabled = false;
        _progressBar.Value = 0d;
        _statusText.Text = Lang.Text("Firmware.Loading");
        UpdateVersionHighlight();

        try
        {
            using var api = new SonyApiClient();
            var service = await api.GetServiceAsync(version.SelfLink, _cancellation!.Token);
            _service = service;

            _fileItems.Clear();
            _filePanel.Children.Clear();
            foreach (var file in service.Files.OrderBy(file => file.PlainSize))
            {
                var check = new CheckBox
                {
                    IsChecked = true,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0d, 0d, 10d, 0d)
                };
                var text = new StackPanel
                {
                    Spacing = 2d,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = file.FileKey,
                            FontSize = 14d,
                            FontWeight = FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = $"{file.Name} · {FormatSize(file.PlainSize)}",
                            FontSize = 12d,
                            Opacity = 0.65d,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                };
                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                    Margin = new Thickness(0d, 4d, 0d, 4d)
                };
                row.Children.Add(check);
                Grid.SetColumn(text, 1);
                row.Children.Add(text);

                _fileItems.Add((file, check));
                _filePanel.Children.Add(row);
            }

            if (!_outputEdited)
                SetOutputPath(service);

            _downloadButton.IsEnabled = service.Files.Count > 0;
            _statusText.Text = service.Files.Count == 0
                ? Lang.Text("Firmware.NoVersions")
                : service.SoftwareVersion;
        }
        catch (OperationCanceledException)
        {
            _statusText.Text = Lang.Text("Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task DownloadAsync()
    {
        if (_busy || _service is null)
            return;

        var output = _outputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(output))
        {
            ShowError(new InvalidOperationException(Lang.Text("Firmware.Output")));
            return;
        }

        ResetCancellation();
        SetBusy(true);
        _progressBar.Value = 0d;

        try
        {
            Directory.CreateDirectory(output);
            using var api = new SonyApiClient();
            using var manager = new DownloadManager();
            var files = _fileItems
                .Where(pair => pair.Check.IsChecked == true)
                .Select(pair => pair.File)
                .OrderBy(file => file.PlainSize)
                .ToList();
            if (files.Count == 0)
            {
                _statusText.Text = Lang.Text("Firmware.SelectFiles");
                return;
            }

            var completed = 0;
            foreach (var file in files)
            {
                _cancellation!.Token.ThrowIfCancellationRequested();
                if (file.ChunkInfoLink is null)
                    throw new InvalidDataException($"{file.FileKey} 缺少 chunk-info 链接");

                _statusText.Text = $"{file.FileKey} ({completed + 1}/{files.Count})";
                var manifest = await api.GetManifestAsync(file.ChunkInfoLink, _cancellation.Token);
                var progress = new Progress<DownloadProgress>(value =>
                    UpdateProgress(file.FileKey, value));
                var result = await manager.DownloadAsync(
                    file,
                    manifest,
                    output,
                    progress,
                    _cancellation.Token);
                completed++;
                _statusText.Text = Lang.Text(
                    "Firmware.Done",
                    Path.GetFileName(result.FilePath));
            }

            _progressBar.Value = 1d;
            _statusText.Text = Lang.Text("Firmware.Done", output);
        }
        catch (OperationCanceledException)
        {
            _statusText.Text = Lang.Text("Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void UpdateProgress(string fileKey, DownloadProgress progress)
    {
        _progressBar.Value = progress.Fraction;
        _statusText.Text = $"{fileKey}  {progress.BytesDownloaded / 1024d / 1024d:F1}"
            + $" / {progress.TotalBytes / 1024d / 1024d:F1} MiB"
            + $"  ·  {progress.BytesPerSecond / 1024d / 1024d:F1} MiB/s";
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _modelBox.IsEnabled = !busy;
        _queryButton.IsEnabled = !busy && _model?.HasSpc == true;
        _downloadButton.IsEnabled = !busy && _service?.Files.Count > 0;
        _cancelButton.IsEnabled = busy;
    }

    private void UpdateVersionHighlight()
    {
        foreach (var (service, item) in _versionItems)
            item.Opacity = ReferenceEquals(service, _selectedVersion) ? 1d : 0.65d;
    }

    private void SetOutputPath(SonySoftwareService service)
    {
        var root = GetDefaultOutputRoot();
        var version = SanitizeFolderName(service.SoftwareVersion);
        _settingOutput = true;
        try
        {
            _outputBox.Text = Path.Combine(root, _product.ProductCode, version);
        }
        finally
        {
            _settingOutput = false;
        }
    }

    private static string GetDefaultOutputRoot()
    {
        var custom = Environment.GetEnvironmentVariable("XPERIA_FIRMWARE_DIR");
        if (!string.IsNullOrWhiteSpace(custom))
            return custom;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(profile)
            ? Path.Combine(Path.GetTempPath(), "XperiaFirmware")
            : Path.Combine(profile, "XperiaFirmware");
    }

    private void ShowError(Exception exception)
    {
        LauncherCommon.Log(exception, "固件下载失败");
        _statusText.Text = Lang.Text("Firmware.Error", exception.Message);
    }

    private void ResetCancellation()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
    }

    private static List<SonyServiceInfo> Deduplicate(IEnumerable<SonyServiceInfo> services)
    {
        var result = new List<SonyServiceInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var service in services)
        {
            if (!seen.Add(service.SoftwareVersion))
                continue;
            var group = services
                .Where(item => string.Equals(
                    item.SoftwareVersion,
                    service.SoftwareVersion,
                    StringComparison.Ordinal))
                .ToList();
            result.Add(
                group.FirstOrDefault(item => item.IsContentErase && item.IsTarget)
                ?? group.FirstOrDefault(item => item.IsContentErase)
                ?? group.FirstOrDefault(item => item.IsTarget)
                ?? group[0]);
        }

        return [.. result.OrderByDescending(service => service.IsTarget)];
    }

    private static string FormatLogicType(string logicType)
    {
        return logicType switch
        {
            "SOFTWARE_UPDATE_CONTENT_ERASE" => "SUCE",
            "CR_SOFTWARE_UPDATE" => "CSU",
            _ => logicType
        };
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / 1024d / 1024d / 1024d:F2} GiB";
        if (bytes >= 1024L * 1024)
            return $"{bytes / 1024d / 1024d:F1} MiB";
        if (bytes >= 1024L)
            return $"{bytes / 1024d:F1} KiB";
        return $"{bytes} B";
    }

    private static string SanitizeFolderName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(value) ? "firmware" : value;
    }
}
