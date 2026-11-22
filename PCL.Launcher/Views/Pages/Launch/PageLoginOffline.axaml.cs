using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.UI.Controls;

// FORK: 按原版 PageLoginLegacy 重写（红提示 + 头像 + 用户名）

namespace PCL.Views.Pages.Launch;

/// <summary>
///     离线登录视图（对齐原版 PCL 的 PageLoginLegacy）：提示、皮肤预览与可编辑用户名下拉；
///     选中已有离线档案直接切换，输入新名字后回车创建档案。
/// </summary>
public partial class PageLoginOffline : LoginPageBase
{
    /// <summary>没有输入名字时显示的默认皮肤（Steve）。</summary>
    private static readonly string DefaultSkinAddress =
        LaunchLoginHelper.GetOfflineSkinAddress(new string('0', 32));

    private readonly List<McProfile> _accounts = [];
    private readonly MyComboBox _comboName;
    private readonly MyHint _hintChinese;
    private readonly MySkin _skin;
    private bool _isSyncing;

    public PageLoginOffline()
    {
        InitializeComponent();

        _comboName = this.FindControl<MyComboBox>("ComboName")!;
        _hintChinese = this.FindControl<MyHint>("HintChinese")!;
        _skin = this.FindControl<MySkin>("Skin")!;

        _comboName.SelectionChanged += (_, _) => OnAccountSelected();
        _comboName.TextChanged += (_, _) => OnNameChanged();
        _comboName.AddHandler(
            KeyDownEvent,
            OnComboKeyDown,
            RoutingStrategies.Tunnel);

        RefreshAccounts();
    }

    public override void Reload()
    {
        RefreshAccounts();
    }

    private void RefreshAccounts()
    {
        _isSyncing = true;
        try
        {
            _accounts.Clear();
            _accounts.AddRange(
                ProfileService.Profiles.Where(profile => profile.ProfileType == ProfileType.Offline));
            _comboName.ItemsSource = _accounts.Select(profile => profile.UserName).ToList();
            _comboName.SelectedIndex = -1;
            _comboName.Text = "";
        }
        finally
        {
            _isSyncing = false;
        }

        UpdateSkinAndHint(string.Empty);
    }

    private void OnAccountSelected()
    {
        if (_isSyncing)
            return;
        var index = _comboName.SelectedIndex;
        if (index < 0 || index >= _accounts.Count)
            return;

        var profile = _accounts[index];
        UpdateSkinAndHint(profile.UserName);
        if (ReferenceEquals(ProfileService.Current, profile))
            return;
        ProfileService.Select(profile);
        RequestRefresh();
    }

    private void OnNameChanged()
    {
        if (_isSyncing)
            return;
        UpdateSkinAndHint(_comboName.Text ?? string.Empty);
    }

    private void OnComboKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        CommitName();
    }

    /// <summary>输入新名字后回车：已有同名档案则切换，否则按标准 UUID 创建。</summary>
    private void CommitName()
    {
        var name = (_comboName.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            return;

        var existing = _accounts.FirstOrDefault(
            profile => string.Equals(profile.UserName, name, StringComparison.Ordinal));
        if (existing is not null)
        {
            ProfileService.Select(existing);
            RequestRefresh();
            return;
        }

        var uuid = LaunchLoginHelper.GetOfflineUuid(name);
        ProfileService.Add(
            new McProfile
            {
                ProfileType = ProfileType.Offline,
                Uuid = uuid,
                UserName = name,
                Description = "",
                AccessToken = uuid,
                ClientToken = uuid,
                ProfileId = Guid.NewGuid().ToString("N")
            });
        RequestRefresh();
    }

    private void UpdateSkinAndHint(string name)
    {
        var trimmed = name.Trim();
        _skin.Address = string.IsNullOrEmpty(trimmed)
            ? DefaultSkinAddress
            : LaunchLoginHelper.GetOfflineSkinAddress(LaunchLoginHelper.GetOfflineUuid(trimmed));

        _hintChinese.IsVisible = trimmed.Any(
            character => !char.IsAsciiLetterOrDigit(character) && character != '_');
    }
}
