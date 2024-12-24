using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Notification;
using FishyFlip.Lexicon.Com.Atproto.Server;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Controls.Settings;
using UniSky.Extensions;
using UniSky.Models;
using UniSky.Moderation;
using UniSky.Pages;
using UniSky.Services;
using Windows.Foundation.Metadata;
using Windows.Phone;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.ViewModels;

// Keep in sync with HomePage.xaml
public enum HomePages
{
    Home,
    Search,
    Notifications,
    Feeds,
    Lists,
    Chat,
    Profile
}

public partial class HomeViewModel : ViewModelBase
{
    private readonly ISessionService sessionService;
    private readonly INavigationService rootNavigationService;
    private readonly INavigationService homeNavigationService;
    private readonly ILogger<HomeViewModel> logger;
    private readonly ILogger<ATProtocol> atLogger;
    private readonly IProtocolService protocolService;
    private readonly INotificationsService notificationsService;
    private readonly IBadgeService badgeService;
    private readonly IModerationService moderationService;
    private readonly SessionModel sessionModel;

    private readonly DispatcherTimer notificationUpdateTimer;
    private readonly DispatcherTimer refreshTokenTimer;

    private ProfileViewDetailed profile;

    [ObservableProperty]
    private string _avatarUrl;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    public int _notificationCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(HomeSelected),
        nameof(SearchSelected),
        nameof(NotificationsSelected),
        nameof(FeedsSelected),
        nameof(ListsSelected),
        nameof(ChatSelected),
        nameof(ProfileSelected))]
    private HomePages _page = (HomePages)(-1);

    public bool HomeSelected
        => Page == HomePages.Home;
    public bool SearchSelected
        => Page == HomePages.Search;
    public bool NotificationsSelected
        => Page == HomePages.Notifications;
    public bool FeedsSelected
        => Page == HomePages.Feeds;
    public bool ListsSelected
        => Page == HomePages.Lists;
    public bool ChatSelected
        => Page == HomePages.Chat;
    public bool ProfileSelected
        => Page == HomePages.Profile;

    public HomeViewModel(
        string profile,
        ISessionService sessionService,
        INavigationServiceLocator navigationServiceLocator,
        IProtocolService protocolService,
        INotificationsService notificationsService,
        IBadgeService badgeService,
        IModerationService moderationService,
        ILogger<HomeViewModel> logger,
        ILogger<ATProtocol> protocolLogger)
    {
        this.rootNavigationService = navigationServiceLocator.GetNavigationService("Root");
        this.homeNavigationService = navigationServiceLocator.GetNavigationService("Home");

        if (!sessionService.TryFindSession(profile, out var sessionModel))
        {
            rootNavigationService.Navigate<LoginPage>();
            return;
        }

        ApplicationData.Current.LocalSettings.Values["LastUsedUser"] = profile;

        this.sessionService = sessionService;
        this.logger = logger;
        this.protocolService = protocolService;
        this.sessionModel = sessionModel;
        this.notificationsService = notificationsService;
        this.badgeService = badgeService;
        this.moderationService = moderationService;
        this.atLogger = protocolLogger;

        var protocol = new ATProtocolBuilder()
            .WithLogger(atLogger)
            .EnableAutoRenewSession(true)
            .WithSessionRefreshInterval(TimeSpan.FromMinutes(30))
            .WithUserAgent(Constants.UserAgent)
            .Build();

        protocolService.SetProtocol(protocol);

        // TODO: throttle when in background
        this.notificationUpdateTimer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(1) };
        this.notificationUpdateTimer.Tick += OnNotificationTimerTick;

        this.refreshTokenTimer = new DispatcherTimer() { Interval = TimeSpan.FromHours(1) };
        this.refreshTokenTimer.Tick += OnRefreshTokenTimerTick;

        var navigationManager = SystemNavigationManager.GetForCurrentView();
        navigationManager.BackRequested += OnBackRequested;

        var window = Window.Current;
        window.VisibilityChanged += OnVisibilityChanged;

        //Task.Run(LoadAsync);
    }

    [RelayCommand]
    private void GoBack()
    {
        this.homeNavigationService.GoBack();
    }

    private async Task LoadAsync()
    {
        using var loading = this.GetLoadingContext();
        var protocol = this.protocolService.Protocol;

        try
        {
            await RefreshSessionAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authenticate!");
            this.syncContext.Post(() => rootNavigationService.Navigate<LoginPage>());
            return;
        }

        this.Page = HomePages.Home;

        await moderationService.ConfigureModerationAsync()
            .ConfigureAwait(false);

        try
        {
            await this.notificationsService.InitializeAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start notifications service!");
        }

        try
        {
            await Task.WhenAll(UpdateProfileAsync(), UpdateNotificationsAsync())
                .ConfigureAwait(false);

            this.syncContext.Post(() => notificationUpdateTimer.Start());
        }
        catch (ATNetworkErrorException ex) when (ex is { AtError.Detail.Error: "ExpiredToken" })
        {
            this.syncContext.Post(() => rootNavigationService.Navigate<LoginPage>());
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch user info!");
            this.SetErrored(ex);
        }
    }

    private Task RefreshSessionAsync()
    {
        return protocolService.RefreshSessionAsync(sessionModel);
    }

    private async Task UpdateProfileAsync()
    {
        var protocol = protocolService.Protocol;

        profile = (await protocol.GetProfileAsync(protocol.AuthSession.Session.Did)
            .ConfigureAwait(false))
            .HandleResult();

        AvatarUrl = profile.Avatar;
        DisplayName = profile.DisplayName;
    }

    private async Task UpdateNotificationsAsync()
    {
        var protocol = protocolService.Protocol;

        var notifications = (await protocol.GetUnreadCountAsync()
            .ConfigureAwait(false))
            .HandleResult();

        NotificationCount = (int)notifications.Count;
        this.badgeService.UpdateBadge(NotificationCount);
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var sheetService = ServiceContainer.Scoped.GetRequiredService<ISheetService>();
        await sheetService.ShowAsync<SettingsSheet>();
    }

    private async void OnNotificationTimerTick(object sender, object e)
    {
        try
        {
            await UpdateNotificationsAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update notifications");
        }
    }

    private async void OnRefreshTokenTimerTick(object sender, object e)
    {
        try
        {
            await RefreshSessionAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authenticate!");
            this.syncContext.Post(() => rootNavigationService.Navigate<LoginPage>());
            return;
        }
    }

    private void OnBackRequested(object sender, BackRequestedEventArgs e)
    {
        if (homeNavigationService.CanGoBack)
        {
            e.Handled = true;
            homeNavigationService.GoBack();
        }
    }

    private void OnVisibilityChanged(object sender, VisibilityChangedEventArgs e)
    {
        if (e.Visible)
        {
            Task.Run(LoadAsync);
        }
    }

    partial void OnPageChanged(HomePages oldValue, HomePages newValue)
    {
        if (oldValue != newValue)
        {
            this.syncContext.Post(NavigateToPage);
        }
    }

    protected override void OnLoadingChanged(bool value)
    {
        if (!ApiInformation.IsApiContractPresent(typeof(PhoneContract).FullName, 1))
            return;

        this.syncContext.Post(() =>
        {
            var statusBar = StatusBar.GetForCurrentView();
            _ = statusBar.ShowAsync();

            statusBar.ProgressIndicator.ProgressValue = null;

            if (value)
            {
                _ = statusBar.ProgressIndicator.ShowAsync();
            }
            else
            {
                _ = statusBar.ProgressIndicator.HideAsync();
            }
        });
    }

    private void NavigateToPage()
    {
        switch (Page)
        {
            case HomePages.Home:
                this.homeNavigationService.Navigate<FeedsPage>();
                break;
            case HomePages.Search:
                this.homeNavigationService.Navigate<SearchPage>();
                break;
            case HomePages.Notifications:
                this.homeNavigationService.Navigate<NotificationsPage>();
                break;
            case HomePages.Feeds:
            case HomePages.Lists:
            case HomePages.Chat:
                this.homeNavigationService.Navigate<Page>();
                break;
            case HomePages.Profile:
                this.homeNavigationService.Navigate<ProfilePage>(this.profile);
                break;
        }
    }

    internal void UpdateChecked()
    {
        this.OnPropertyChanged(nameof(HomeSelected),
                nameof(SearchSelected),
                nameof(NotificationsSelected),
                nameof(FeedsSelected),
                nameof(ListsSelected),
                nameof(ChatSelected),
                nameof(ProfileSelected));
    }
}
