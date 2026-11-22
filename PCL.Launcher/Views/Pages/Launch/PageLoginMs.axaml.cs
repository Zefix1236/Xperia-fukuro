using Avalonia.Controls;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.UI.Controls;

// FORK: 按原版 PageLoginMs 重写（账号下拉 + 登录按钮）

namespace PCL.Views.Pages.Launch;

/// <summary>
///     正版（微软）登录视图：登录状态与设备码登录由 <see cref="PageLoginMsViewModel" /> 负责，
///     页面维护“已保存账号”下拉、导航事件转发与购买 / 官网链接。
/// </summary>
public partial class PageLoginMs : StackPanel, ILoginPage, ILoginPage_Bindable
{
    private readonly List<McProfile> _accounts = [];
    private readonly MyComboBox _comboAccounts;
    private bool _isSyncingAccounts;

    public PageLoginMs()
    {
        InitializeComponent();
        DataContext = ViewModel;

        ViewModel.BackRequested += RequestBack;
        ViewModel.RefreshRequested += RequestRefresh;

        _comboAccounts = this.FindControl<MyComboBox>("ComboAccounts")!;
        _comboAccounts.SelectionChanged += (_, _) => OnAccountSelected();

        this.FindControl<MyTextButton>("BtnPurchase")!.Click += (_, _) =>
            LauncherUi.Interaction?.OpenWebsite(
                "https://www.xbox.com/zh-cn/games/store/minecraft-java-bedrock-edition-for-pc/9nxp44l49shj");
        this.FindControl<MyTextButton>("BtnWebsite")!.Click += (_, _) =>
            LauncherUi.Interaction?.OpenWebsite("https://www.minecraft.net/zh-hans");

        ResetLoginState();
        RefreshAccounts();
    }

    public PageLoginMsViewModel ViewModel { get; } = new();

    public void Reload()
    {
        ViewModel.ResetLoginState();
        RefreshAccounts();
    }

    public event Action? BackRequested;

    public event Action<GameLaunch.McLoginType>? LoginTypeRequested;

    public event Action? RefreshRequested;

    /// <summary>刷新“已保存账号”下拉：只列正版档案，选中即切换。</summary>
    private void RefreshAccounts()
    {
        _isSyncingAccounts = true;
        try
        {
            _accounts.Clear();
            _accounts.AddRange(
                ProfileService.Profiles.Where(profile => profile.ProfileType == ProfileType.Microsoft));
            _comboAccounts.ItemsSource = _accounts.Select(profile => profile.UserName).ToList();
            _comboAccounts.SelectedIndex = -1;
        }
        finally
        {
            _isSyncingAccounts = false;
        }
    }

    private void OnAccountSelected()
    {
        if (_isSyncingAccounts)
            return;
        var index = _comboAccounts.SelectedIndex;
        if (index < 0 || index >= _accounts.Count)
            return;

        var profile = _accounts[index];
        if (ReferenceEquals(ProfileService.Current, profile))
            return;
        ProfileService.Select(profile);
        RequestRefresh();
    }

    private void RequestBack()
    {
        BackRequested?.Invoke();
    }

    private void RequestRefresh()
    {
        RefreshRequested?.Invoke();
    }

    private void ResetLoginState()
    {
        ViewModel.ResetLoginState();
    }
}
