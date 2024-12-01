using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Phone;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace UniSky.Services;

public record class SafeAreaInfo(bool HasTitleBar, bool IsActive, Thickness Bounds);

public class SafeAreaUpdatedEventArgs : EventArgs
{
    public SafeAreaInfo SafeArea { get; init; }
}

internal class SafeAreaService : ISafeAreaService
{
    private readonly CoreWindow _window;
    private readonly ApplicationView _applicationView;
    private readonly CoreApplicationView _coreApplicationView;

    private SafeAreaInfo _state;

    private event EventHandler<SafeAreaUpdatedEventArgs> _event;

    public SafeAreaInfo State
        => _state;

    public SafeAreaService()
    {
        _window = CoreWindow.GetForCurrentThread();
        _window.SizeChanged += CoreWindow_SizeChanged;
        _window.Activated += CoreWindow_Activated;

        _applicationView = ApplicationView.GetForCurrentView();
        _coreApplicationView = CoreApplication.GetCurrentView();

        _applicationView.SetPreferredMinSize(new Size(320, 640));
        _applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
        _applicationView.VisibleBoundsChanged += ApplicationView_VisibleBoundsChanged;

        var appTitleBar = _applicationView.TitleBar;
        appTitleBar.ButtonBackgroundColor = Colors.Transparent;
        appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        var titleBar = _coreApplicationView.TitleBar;
        titleBar.ExtendViewIntoTitleBar = true;

        titleBar.LayoutMetricsChanged
            += CoreTitleBar_LayoutMetricsChanged;
        titleBar.IsVisibleChanged
            += CoreTitleBar_IsVisibleChanged;

        _state = new SafeAreaInfo(true, true, new Thickness());
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
            top += (float)titleBar.Height;

        var bounds = _window.Bounds;
        var visibleBounds = _applicationView.VisibleBounds;
        top += (float)(visibleBounds.Top - bounds.Top);
        left += (float)(visibleBounds.Left - bounds.Left);
        right += (float)(bounds.Right - visibleBounds.Right);
        bottom += (float)(bounds.Bottom - visibleBounds.Bottom);

        _state = _state with { Bounds = new Thickness(left, top, right, bottom) };
        _event?.Invoke(this, new SafeAreaUpdatedEventArgs() { SafeArea = _state });
    }
}