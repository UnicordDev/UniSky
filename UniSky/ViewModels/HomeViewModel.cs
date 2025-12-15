using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Controls.Settings;
using UniSky.Extensions;
using UniSky.Pages;
using UniSky.Services;
using Windows.Foundation.Metadata;
using Windows.Networking.PushNotifications;
using Windows.Phone;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
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
    Bookmarks,
    Chat,
    Profile,
    Settings,
}

public partial class HomeViewModel : ViewModelBase
{
    private const string HOME_ICON_GLYPH = "\uE80F";
    private const string SEARCH_ICON_GLYPH = "\uE71E";
    private const string FEEDS_ICON_GLYPH = "\uE728";
    private const string LISTS_ICON_GLYPH = "\uE71D";
    private const string CHAT_ICON_GLYPH = "\uE8F2";
    private const string SETTINGS_ICON_GLYPH = "\uE713";
    private const string BOOKMARKS_ICON_GLYPH = "\uE840";

    private readonly ILogger<HomeViewModel> logger;
    private readonly INavigationService homeNavigationService;
    private readonly IProtocolService protocolService;
    private readonly INotificationsService notificationsService;

    private readonly Dictionary<HomePages, MenuItemViewModel> AvailableMenuItems;

    private bool isLoaded;

    [ObservableProperty]
    private MenuItemViewModel _selectedMenuItem;

    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = [];
    public ObservableCollection<MenuItemViewModel> FooterMenuItems { get; } = [];
    public ObservableCollection<MenuItemViewModel> PinnedMenuItems { get; } = [];

    public HomeViewModel(
        INavigationServiceLocator navigationServiceLocator,
        IProtocolService protocolService,
        INotificationsService notificationsService,
        ILogger<HomeViewModel> logger)
    {
        this.homeNavigationService = navigationServiceLocator.GetNavigationService("Home");

        this.logger = logger;
        this.protocolService = protocolService;
        this.notificationsService = notificationsService;

        AvailableMenuItems = new Dictionary<HomePages, MenuItemViewModel>()
        {
            [HomePages.Home] = new MenuItemViewModel(this, HomePages.Home, HOME_ICON_GLYPH, typeof(FeedsPage)),
            [HomePages.Search] = new MenuItemViewModel(this, HomePages.Search, SEARCH_ICON_GLYPH, typeof(SearchPage)),
            [HomePages.Notifications] = new NotificationsMenuItemViewModel(this),
            [HomePages.Feeds] = new MenuItemViewModel(this, HomePages.Feeds, FEEDS_ICON_GLYPH, typeof(Page)),
            [HomePages.Lists] = new MenuItemViewModel(this, HomePages.Lists, LISTS_ICON_GLYPH, typeof(Page)),
            [HomePages.Bookmarks] = new MenuItemViewModel(this, HomePages.Bookmarks, BOOKMARKS_ICON_GLYPH, typeof(BookmarksPage)),
            [HomePages.Chat] = new MenuItemViewModel(this, HomePages.Chat, CHAT_ICON_GLYPH, typeof(Page)),
            [HomePages.Profile] = new ProfileMenuItemViewModel(this),
            [HomePages.Settings] = new MenuItemViewModel(this, HomePages.Settings, SETTINGS_ICON_GLYPH, typeof(Page))
        };

        MenuItems =
        [
            AvailableMenuItems[HomePages.Home],
            AvailableMenuItems[HomePages.Search],
            AvailableMenuItems[HomePages.Notifications],
            AvailableMenuItems[HomePages.Feeds],
            AvailableMenuItems[HomePages.Lists],
            AvailableMenuItems[HomePages.Bookmarks],
            AvailableMenuItems[HomePages.Chat],
        ];

        FooterMenuItems =
        [
            AvailableMenuItems[HomePages.Profile],
            AvailableMenuItems[HomePages.Settings],
        ];

        PinnedMenuItems =
        [
            AvailableMenuItems[HomePages.Home],
            AvailableMenuItems[HomePages.Search],
            AvailableMenuItems[HomePages.Notifications],
            AvailableMenuItems[HomePages.Chat],
            AvailableMenuItems[HomePages.Profile],
        ];

        SelectedMenuItem = AvailableMenuItems[HomePages.Home];

        var navigationManager = SystemNavigationManager.GetForCurrentView();
        navigationManager.BackRequested += OnBackRequested;

        Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        using var loading = this.GetLoadingContext();
        var protocol = this.protocolService.Protocol;

        await Task.WhenAll(
            MenuItems.Concat(FooterMenuItems)
                     .Select(s => s.LoadAsync()));

        try
        {
            await this.notificationsService.InitializeAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start notifications service!");
        }

    }

    [RelayCommand]
    private void GoBack()
    {
        this.homeNavigationService.GoBack();
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var sheetService = ServiceContainer.Scoped.GetRequiredService<ISheetService>();
        await sheetService.ShowAsync<SettingsSheet>();
    }

    private void OnBackRequested(object sender, BackRequestedEventArgs e)
    {
        if (homeNavigationService.CanGoBack)
        {
            e.Handled = true;
            homeNavigationService.GoBack();
        }
    }

    partial void OnSelectedMenuItemChanged(MenuItemViewModel oldValue, MenuItemViewModel newValue)
    {
        if (oldValue == newValue) return;

        if (newValue.Page == HomePages.Settings)
        {
            this.syncContext.Post(async () => await OpenSettingsAsync());
            SelectedMenuItem = oldValue;
            return;
        }

        if (oldValue != null)
            oldValue.IsSelected = false;

        if (newValue != null)
            newValue.IsSelected = true;

        this.syncContext.Post(() =>
        {
            var serviceLocator = ServiceContainer.Scoped.GetRequiredService<INavigationServiceLocator>();
            var service = serviceLocator.GetNavigationService("Home");
            service.Frame = newValue.Content;
        });
    }

    protected override void OnLoadingChanged(bool value)
    {
        if (!ApiInformation.IsApiContractPresent(typeof(PhoneContract).FullName, 1))
            return;

        this.syncContext.Post(() =>
        {
            try
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
            }
            catch { }
        });
    }
}
