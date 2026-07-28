using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using UniSky.Services;
using UniSky.Services.Navigation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.Controls.Overlay;

public sealed partial class OverlayRootControl : UserControl, IOverlayRootControl
{
    private readonly IBackNavigationCoordinator _backCoordinator;
    private readonly IDisposable _backRegistration;

    private IOverlayController _controller;

    public OverlayRootControl()
    {
        this.InitializeComponent();

        VisualStateManager.GoToState(this, "Closed", false);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        _backCoordinator = ServiceContainer.Scoped.GetRequiredService<IBackNavigationCoordinator>();
        _backRegistration = _backCoordinator.Register(new OverlayBackHandler(this), BackPriority.Overlay);
    }

    private sealed class OverlayBackHandler(OverlayRootControl owner) : IBackHandler
    {
        public bool CanGoBack
            => owner._controller != null;

        public bool TryGoBack()
        {
            var controller = owner._controller;
            if (controller == null)
                return false;

            _ = controller.TryHideAsync();
            return true;
        }
    }

    private void ShowOverlay(IOverlayController controller, IOverlayControl control, object parameter)
    {
        if (_controller != null)
        {
            throw new InvalidOperationException("Attempting to show two overlays at once!");
        }

        _controller = controller;

        SheetRoot.Child = (FrameworkElement)control;
        control.InvokeShowing(parameter);

        VisualStateManager.GoToState(this, "Open", true);

        _backCoordinator.Invalidate();
    }

    private Task<bool> HideOverlayAsync()
    {
        VisualStateManager.GoToState(this, "Closed", true);

        _controller = null;
        _backCoordinator.Invalidate();

        return Task.FromResult(true);
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        HostControl.Margin = e.SafeArea.Bounds;
    }

    private async void PrimaryTitleBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (this._controller == null) return;
        await _controller.TryHideAsync();
    }

    private void ShowOverlayStoryboard_Completed(object sender, object e)
    {
        if (SheetRoot.Child is IOverlayControl control)
        {
            control.InvokeShown();
        }
    }

    private void HideOverlayStoryboard_Completed(object sender, object e)
    {
        if (SheetRoot.Child is IOverlayControl control)
        {
            control.InvokeHidden();
            SheetRoot.Child = null;
        }
    }

    Task IOverlayRootControl.ShowAsync(IOverlayController controller, IOverlayControl control, object param)
    {
        ShowOverlay(controller, control, param);
        return Task.CompletedTask;
    }

    Task<bool> IOverlayRootControl.HideAsync(IOverlayController controller)
    {
        if (_controller != controller)
            return Task.FromResult(false);

        return HideOverlayAsync();
    }
}
