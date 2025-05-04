using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Overlay;
using UniSky.Helpers;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace UniSky.Services.Overlay;

internal class AppWindowOverlayController : IOverlayController
{
    private readonly AppWindow appWindow;
    private readonly IOverlayControl control;
    private readonly ISettingsService settingsService;
    private readonly IOverlaySizeProvider overlaySizeProvider;

    private readonly string settingsKey;
    private readonly long titlePropertyChangedRef;
    private FrameworkElement Control
        => (FrameworkElement)control;

    public AppWindowOverlayController(AppWindow window, IOverlayControl control, IOverlaySizeProvider overlaySizeProvider)
    {
        this.appWindow = window;
        this.control = control;
        this.settingsService = ServiceContainer.Scoped.GetRequiredService<ISettingsService>();
        this.SafeAreaService = new AppWindowSafeAreaService(appWindow);
        this.settingsKey = "CoreWindow_LastSize_" + control.GetType().FullName.Replace(".", "_");
        this.overlaySizeProvider = overlaySizeProvider;

        this.control.SetOverlayController(this);

        appWindow.PersistedStateId = settingsKey;
        appWindow.CloseRequested += OnCloseRequested;
        appWindow.Closed += OnClosed;
        appWindow.Changed += OnChanged;

        this.titlePropertyChangedRef = this.Control.RegisterPropertyChangedCallback(OverlayControl.TitleContentProperty, OnTitleChanged);
        OnTitleChanged(Control, OverlayControl.TitleContentProperty);
    }

    public UIElement Root => (UIElement)control;
    public bool IsStandalone => true;
    public ISafeAreaService SafeAreaService { get; }

    public async Task ShowAsync(object parameter)
    {
        control.InvokeShowing(parameter);

        ElementCompositionPreview.SetAppWindowContent(appWindow, (UIElement)control);

        var initialSize = control.PreferredWindowSize;
        if (overlaySizeProvider != null)
        {
            var size = overlaySizeProvider.GetDesiredSize();
            if (size != null)
                initialSize = size.Value;
        }

        appWindow.RequestSize(initialSize);

        PlaceAppWindow(initialSize);

        if (!await appWindow.TryShowAsync())
        {
            throw new InvalidOperationException("Failed to create app window! Falling back!");
        }

        control.InvokeShown();
    }

    public async Task<bool> TryHideAsync()
    {
        if (await control.InvokeHidingAsync())
        {
            await appWindow.CloseAsync();
            return true;
        }

        return false;
    }

    private async void OnCloseRequested(AppWindow sender, AppWindowCloseRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (!await control.InvokeHidingAsync())
            args.Cancel = true;

        deferral.Complete();
    }

    private void OnClosed(AppWindow sender, AppWindowClosedEventArgs args)
    {
        Control.UnregisterPropertyChangedCallback(OverlayControl.TitleContentProperty, titlePropertyChangedRef);
        control.InvokeHidden();
    }

    private void OnChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            var settingsKey = "AppWindow_LastSize_" + control.GetType().FullName.Replace(".", "_");
            settingsService.Save(settingsKey, new Size(Control.ActualSize.X, Control.ActualSize.Y));
        }
    }

    private void OnTitleChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (dp != OverlayControl.TitleContentProperty)
            return;

        appWindow.Title = control.TitleContent?.ToString() ?? "";
    }

    private void PlaceAppWindow(Size initialSize)
    {
        var applicationView = ApplicationView.GetForCurrentView();
        var displayInformation = DisplayInformation.GetForCurrentView();
        var dpiScale = displayInformation.LogicalDpi / 96.0f;
        var visibleBounds = applicationView.VisibleBounds;

        // this shit isn't DPI scaled correctly
        visibleBounds = new Rect(
            visibleBounds.X * dpiScale,
            visibleBounds.Y * dpiScale,
            visibleBounds.Width * dpiScale,
            visibleBounds.Height * dpiScale);

        initialSize = new Size(
            initialSize.Width * dpiScale,
            initialSize.Height * dpiScale);

        var minimumSize = new Size(320 * dpiScale, 320 * dpiScale);

        var environment = applicationView.WindowingEnvironment;
        if (environment.Kind == WindowingEnvironmentKind.Overlapped)
        {
            var visibleCenter = new Point(
                visibleBounds.X + (visibleBounds.Width / 2),
                visibleBounds.Y + visibleBounds.Height / 2);

            var regions = environment.GetDisplayRegions();
            var currentRegion = regions[0];
            foreach (var region in regions)
            {
                var regionRect = new Rect(region.WorkAreaOffset, region.WorkAreaSize);
                if (regionRect.Contains(visibleCenter))
                    currentRegion = region;
            }

            var displayOffset = currentRegion.WorkAreaOffset;
            var displaySize = currentRegion.WorkAreaSize;
            var displayRect = new Rect(displayOffset, displaySize);

            var offsetFromLeftEdge = Math.Max(0, visibleBounds.Left - displayRect.Left);
            var offsetFromRightEdge = Math.Max(0, displayRect.Right - visibleBounds.Right);
            if ((applicationView.AdjacentToLeftDisplayEdge && applicationView.AdjacentToRightDisplayEdge) ||
                Math.Max(offsetFromLeftEdge, offsetFromRightEdge) < ((displaySize.Width / 3.0) * 1.0)) // not enough space 
            {
                var windowCenter = new Point(
                    (visibleBounds.X - currentRegion.WorkAreaOffset.X + visibleBounds.Width / 2.0) + 21,
                    visibleBounds.Y - currentRegion.WorkAreaOffset.Y + visibleBounds.Height / 2.0);
                var windowSize = new Size(visibleBounds.Width, visibleBounds.Height);

                double width = initialSize.Width;
                double height = initialSize.Height;
                SizeHelpers.Scale(ref width, ref height,
                    Math.Max(minimumSize.Width, (windowSize.Width / 5.0) * 4.0),
                    Math.Max(minimumSize.Height, (windowSize.Height / 5.0) * 4.0));

                var position = new Point(windowCenter.X - (width / 2.0), windowCenter.Y - (height / 2.0));
                appWindow.RequestSize(new Size(width, height + 32));
                appWindow.RequestMoveRelativeToDisplayRegion(currentRegion, position);
            }
            else
            {
                double maxWidth = Math.Min(Math.Max(offsetFromLeftEdge, offsetFromRightEdge), displayRect.Width / 4.0 * 3.0),
                       maxHeight = Math.Min(Math.Min(displayRect.Height, visibleBounds.Height), displayRect.Height / 4.0 * 3.0);

                double width = Math.Max(minimumSize.Width, initialSize.Width),
                       height = Math.Max(minimumSize.Height, initialSize.Height);

                SizeHelpers.Scale(ref width, ref height, maxWidth, maxHeight);

                if (offsetFromRightEdge > offsetFromLeftEdge)
                {
                    // right
                    appWindow.RequestSize(new Size(width, height + 32));
                    // except where it is!
                    appWindow.RequestMoveRelativeToCurrentViewContent(new Point(((visibleBounds.Width) / dpiScale) + 8, 0));
                }
                else
                {
                    // left
                    appWindow.RequestSize(new Size(width, height + 32));
                    // ditto
                    appWindow.RequestMoveRelativeToCurrentViewContent(new Point((-width / dpiScale) - 8, 0));
                }
            }
        }
        else
        {
            appWindow.RequestSize(initialSize);
        }
    }
}
