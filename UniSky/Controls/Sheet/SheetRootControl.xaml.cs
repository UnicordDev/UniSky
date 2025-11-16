using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using UniSky.Services;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace UniSky.Controls.Sheet;

[ContentProperty(Name = nameof(ContentElement))]
public sealed partial class SheetRootControl : UserControl, IOverlayRootControl
{
    public FrameworkElement ContentElement
    {
        get => (FrameworkElement)GetValue(ContentElementProperty);
        set => SetValue(ContentElementProperty, value);
    }

    public static readonly DependencyProperty ContentElementProperty =
        DependencyProperty.Register("ContentElement", typeof(FrameworkElement), typeof(SheetRootControl), new PropertyMetadata(null));

    public double TotalHeight
    {
        get => (double)GetValue(TotalHeightProperty);
        set => SetValue(TotalHeightProperty, value);
    }

    // Using a DependencyProperty as the backing store for TotalHeight.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TotalHeightProperty =
        DependencyProperty.Register("TotalHeight", typeof(double), typeof(SheetRootControl), new PropertyMetadata(0.0));

    private IOverlayController _controller;
    private TaskCompletionSource<object> _showTcs;
    private TaskCompletionSource<bool> _hideTcs;

    public SheetRootControl()
    {
        this.InitializeComponent();
        VisualStateManager.GoToState(this, "Closed", false);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!double.IsInfinity(HostControl.MaxHeight))
        {
            TotalHeight = 64;
            SheetRoot.Height = Math.Max(0, HostControl.MaxHeight - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
        }
        else
        {
            TotalHeight = finalSize.Height;
            SheetRoot.Height = Math.Max(0, finalSize.Height - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
        }

        return base.ArrangeOverride(finalSize);
    }


    private void ShowSheet(IOverlayController controller, IOverlayControl control, object parameter)
    {
        if (_controller != null)
        {
            throw new InvalidOperationException("Attempting to show two sheets at once!");
        }

        _controller = controller;

        SheetRoot.Child = (FrameworkElement)control;
        control.InvokeShowing(parameter);

        VisualStateManager.GoToState(this, "Open", true);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
        systemNavigationManager.BackRequested += OnBackRequested;
    }

    private bool HideSheet()
    {
        if (_controller == null)
            return false;

        VisualStateManager.GoToState(this, "Closed", true);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;

        var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
        systemNavigationManager.BackRequested -= OnBackRequested;

        _controller = null;

        return true;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        TitleBar.Height = e.SafeArea.Bounds.Top;
        SheetBorder.Margin = new Thickness(0, 16 + e.SafeArea.Bounds.Top, 0, 0);
        HostControl.Margin = new Thickness(e.SafeArea.Bounds.Left, 0, e.SafeArea.Bounds.Right, e.SafeArea.Bounds.Bottom);

        if (!double.IsInfinity(HostControl.MaxHeight))
        {
            SheetRoot.Height = Math.Max(0, HostControl.MaxHeight - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
        }
        else
        {
            SheetRoot.Height = Math.Max(0, ActualHeight - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
        }
    }

    private async void OnBackRequested(object sender, BackRequestedEventArgs e)
    {
        if (this._controller == null) return;

        e.Handled = true;
        await this._controller.TryHideAsync();
    }

    private async void RefreshContainer_RefreshRequested(MUXC.RefreshContainer sender, MUXC.RefreshRequestedEventArgs args)
    {
        if (this._controller == null) return;

        var deferral = args.GetDeferral();
        await this._controller.TryHideAsync();
        deferral.Complete();
    }

    private async void ShowSheetStoryboard_Completed(object sender, object e)
    {
        var elementToFocus = FocusManager.FindFirstFocusableElement(SheetRoot.Child);
        if (ApiInformation.IsMethodPresent(typeof(FocusManager).FullName, "TryFocusAsync")
            && elementToFocus is DependencyObject dep)
        {
            await FocusManager.TryFocusAsync(dep, FocusState.Programmatic);
        }
        else if (elementToFocus is Control controlToFocus)
        {
            controlToFocus.Focus(FocusState.Programmatic);
        }

        if (SheetRoot.Child is IOverlayControl control)
        {
            control.InvokeShown();
        }

        CommonShadow.CastTo = CompositionBackdropContainer;
        Effects.SetShadow(SheetBorder, CommonShadow);

        _showTcs.SetResult(null);
    }

    private void HideSheetStoryboard_Completed(object sender, object e)
    {
        if (SheetRoot.Child is IOverlayControl control)
        {
            control.InvokeHidden();
            SheetRoot.Child = null;
        }

        Effects.SetShadow(SheetBorder, null);
    }

    async Task IOverlayRootControl.ShowAsync(IOverlayController controller, IOverlayControl control, object param)
    {
        _hideTcs?.TrySetResult(false);

        _showTcs = new TaskCompletionSource<object>();
        ShowSheet(controller, control, param);
        await _showTcs.Task;
    }

    Task<bool> IOverlayRootControl.HideAsync()
    {
        _showTcs?.TrySetResult(null);

        return Task.FromResult(HideSheet());
    }
}
