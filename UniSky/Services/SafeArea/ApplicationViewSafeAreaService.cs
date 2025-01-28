using System;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Phone;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace UniSky.Services;

internal class ApplicationViewSafeAreaService : ISafeAreaService
{
    private readonly CoreWindow _window;
    private readonly ApplicationView _applicationView;
    private readonly CoreApplicationView _coreApplicationView;
    private readonly ThemeListener _themeListener;

    private readonly IThemeService themeService;

    private SafeAreaInfo _state;

    private event EventHandler<SafeAreaUpdatedEventArgs> _event;

    public SafeAreaInfo State
        => _state;

    public ApplicationViewSafeAreaService(IThemeService themeService)
    {
        this.themeService = themeService;

        _window = CoreWindow.GetForCurrentThread();
        _window.SizeChanged += CoreWindow_SizeChanged;
        _window.Activated += CoreWindow_Activated;

        _applicationView = ApplicationView.GetForCurrentView();
        _coreApplicationView = CoreApplication.GetCurrentView();

        if (_coreApplicationView.IsMain)
            _applicationView.SetPreferredMinSize(new Size(320, 640));
        else
            _applicationView.SetPreferredMinSize(new Size(320, 320));

        _applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
        _applicationView.VisibleBoundsChanged += ApplicationView_VisibleBoundsChanged;

        var titleBar = _coreApplicationView.TitleBar;
        titleBar.ExtendViewIntoTitleBar = true;

        titleBar.LayoutMetricsChanged
            += CoreTitleBar_LayoutMetricsChanged;
        titleBar.IsVisibleChanged
            += CoreTitleBar_IsVisibleChanged;

        _themeListener = new ThemeListener();
        _themeListener.ThemeChanged += OnThemeChanged;

        _state = new SafeAreaInfo(true, true, new Thickness(), ElementTheme.Default);
        SetTitlebarTheme(ElementTheme.Default);
    }

    public event EventHandler<SafeAreaUpdatedEventArgs> SafeAreaUpdated
    {
        add
        {
            _event += value;
            Update();
        }

        remove => _event -= value;
    }

    private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
    {
        Update();
    }

    private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
    {
        if (sender.IsVisible)
        {
            _state = _state with { HasTitleBar = true };
        }
        else
        {
            _state = _state with { HasTitleBar = false };
        }

        Update();
    }

    private void CoreWindow_Activated(CoreWindow sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == CoreWindowActivationState.Deactivated)
        {
            _state = _state with { IsActive = false };
        }
        else
        {
            _state = _state with { IsActive = true };
        }

        Update();
    }

    private void ApplicationView_VisibleBoundsChanged(ApplicationView sender, object args)
    {
        Update();
    }

    private void CoreWindow_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
    {
        Update();
    }

    private void Update()
    {
        if (_coreApplicationView.TitleBar.IsVisible)
        {
            _state = _state with { HasTitleBar = true };
        }
        else
        {
            _state = _state with { HasTitleBar = false };
        }

        UpdateBounds();
    }

    private void UpdateBounds()
    {
        var top = 0.0f;
        var left = 0.0f;
        var right = 0.0f;
        var bottom = 0.0f;

        var titleBar = _coreApplicationView.TitleBar;
        if (titleBar.IsVisible)
        {
            if (themeService.GetTheme() == AppTheme.SunValley)
                top += Math.Max(40, (float)titleBar.Height);
            else
                top += (float)titleBar.Height;
        }

        var bounds = _window.Bounds;
        var visibleBounds = _applicationView.VisibleBounds;
        top += (float)(visibleBounds.Top - bounds.Top);
        left += (float)(visibleBounds.Left - bounds.Left);
        right += (float)(bounds.Right - visibleBounds.Right);
        bottom += (float)(bounds.Bottom - visibleBounds.Bottom);

        _state = _state with { Bounds = new Thickness(left, top, right, bottom) };
        _event?.Invoke(this, new SafeAreaUpdatedEventArgs() { SafeArea = _state });
    }

    public void SetTitlebarTheme(ElementTheme theme)
    {
        if (themeService.GetTheme() == AppTheme.SunValley)
            theme = ElementTheme.Default;

        var actualTheme = theme switch
        {
            ElementTheme.Default => _themeListener.CurrentTheme == ApplicationTheme.Dark
                ? ElementTheme.Dark
                : ElementTheme.Light,
            _ => theme
        };

        var appTitleBar = _applicationView.TitleBar;
        appTitleBar.ButtonBackgroundColor = Colors.Transparent;
        appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        if (actualTheme == ElementTheme.Dark)
        {
            appTitleBar.ButtonForegroundColor = Colors.White;
            appTitleBar.ButtonInactiveForegroundColor = Colors.LightGray;
        }
        else
        {
            appTitleBar.ButtonForegroundColor = Colors.Black;
            appTitleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
        }

        if (ApiInformation.IsApiContractPresent(typeof(PhoneContract).FullName, 1))
        {
            var statusBar = StatusBar.GetForCurrentView();

            if (actualTheme == ElementTheme.Dark)
            {
                statusBar.ForegroundColor = Colors.White;
            }
            else
            {
                statusBar.ForegroundColor = Colors.Black;
            }
        }

        _state = _state with { Theme = theme };
        _event?.Invoke(this, new SafeAreaUpdatedEventArgs() { SafeArea = _state });
    }

    private void OnThemeChanged(ThemeListener sender)
    {
        SetTitlebarTheme(_state.Theme);
    }

    public void SetTitleBar(UIElement uiElement)
    {
        Window.Current.SetTitleBar(uiElement);
    }
}