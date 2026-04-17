using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using UniSky.Services;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UniSky.Controls.Overlay;

public sealed partial class OverlayRootControl : UserControl, IOverlayRootControl
{
    private IOverlayController _controller;

    public OverlayRootControl()
    {
        this.InitializeComponent();

        VisualStateManager.GoToState(this, "Closed", false);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;
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

        var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
        systemNavigationManager.BackRequested += OnBackRequested;
    }

    private Task<bool> HideOverlayAsync()
    {
        VisualStateManager.GoToState(this, "Closed", true);

        var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
        systemNavigationManager.BackRequested -= OnBackRequested;

        _controller = null;

        return Task.FromResult(true);
    }

    private async void OnBackRequested(object sender, BackRequestedEventArgs e)
    {
        if (this._controller == null) return;

        e.Handled = true;
        await _controller.TryHideAsync();
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

    Task<bool> IOverlayRootControl.HideAsync()
    {
        return HideOverlayAsync();
    }
}
