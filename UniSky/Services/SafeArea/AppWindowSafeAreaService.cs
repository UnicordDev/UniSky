using System;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.WindowManagement;
using Windows.Foundation;
using Windows.UI.WindowManagement.Preview;
using System.Linq;

namespace UniSky.Services;

internal class AppWindowSafeAreaService : ISafeAreaService
{
    private readonly AppWindow _appWindow;
    private readonly ThemeListener _themeListener;

    private SafeAreaInfo _state;
    private event EventHandler<SafeAreaUpdatedEventArgs> _event;

    public SafeAreaInfo State
        => _state;

    public event EventHandler<SafeAreaUpdatedEventArgs> SafeAreaUpdated
    {
        add
        {
            _event += value;
            Update();
        }

        remove => _event -= value;
    }

    public AppWindowSafeAreaService(AppWindow appWindow)
    {
        this._appWindow = appWindow;
        this._themeListener = new ThemeListener();

        WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(320, 320));

        var titleBar = appWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;

        appWindow.Changed += OnChanged;

        _state = new SafeAreaInfo(true, true, new Thickness(), ElementTheme.Default);
        SetTitlebarTheme(ElementTheme.Default);
        Update();
    }

    private void OnChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        Update();
    }

    private void Update()
    {
        if (_appWindow.TitleBar.IsVisible)
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

        var titleBar = _appWindow.TitleBar;
        if (titleBar.IsVisible)
        {
            top += (float)titleBar.GetTitleBarOcclusions().Max(t => t.OccludingRect.Height);
        }

        _state = _state with { Bounds = new Thickness(left, top, right, bottom) };
        _event?.Invoke(this, new SafeAreaUpdatedEventArgs() { SafeArea = _state });
    }

    public void SetTitlebarTheme(ElementTheme theme)
    {
        var actualTheme = theme switch
        {
            ElementTheme.Default => _themeListener.CurrentTheme == ApplicationTheme.Dark
                ? ElementTheme.Dark
                : ElementTheme.Light,
            _ => theme
        };

        var appTitleBar = _appWindow.TitleBar;
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

        _state = _state with { Theme = theme };
        _event?.Invoke(this, new SafeAreaUpdatedEventArgs() { SafeArea = _state });
    }

    public void SetTitleBar(UIElement uiElement)
    {
        // N/A afaict?
    }
}