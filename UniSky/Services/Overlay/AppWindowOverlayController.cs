﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Overlay;
using UniSky.Helpers;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;

namespace UniSky.Services.Overlay;

internal class AppWindowOverlayController : IOverlayController
{
    private readonly AppWindow appWindow;
    private readonly OverlayControl control;
    private readonly ISettingsService settingsService;

    private readonly string settingsKey;
    private readonly long titlePropertyChangedRef;

    public AppWindowOverlayController(AppWindow window, OverlayControl control, IOverlaySizeProvider overlaySizeProvider)
    {
        this.appWindow = window;
        this.control = control;
        this.settingsService = ServiceContainer.Scoped.GetRequiredService<ISettingsService>();
        this.SafeAreaService = new AppWindowSafeAreaService(appWindow);

        this.settingsKey = "CoreWindow_LastSize_" + control.GetType().FullName.Replace(".", "_");
        var initialSize = settingsService.Read(settingsKey, control.PreferredWindowSize);
        if (overlaySizeProvider != null)
        {
            var size = overlaySizeProvider.GetDesiredSize();
            if (size != null)
                initialSize = size.Value with { Height = size.Value.Height };
        }

        appWindow.PersistedStateId = settingsKey;
        appWindow.CloseRequested += OnCloseRequested;
        appWindow.Closed += OnClosed;
        appWindow.Changed += OnChanged;
        appWindow.RequestSize(initialSize);

        PlaceAppWindow(initialSize);

        this.titlePropertyChangedRef = this.control.RegisterPropertyChangedCallback(OverlayControl.TitleContentProperty, OnTitleChanged);
        OnTitleChanged(this.control, OverlayControl.TitleContentProperty);
    }

    public UIElement Root => control;
    public bool IsStandalone => true;
    public ISafeAreaService SafeAreaService { get; }

    public async Task<bool> TryHideSheetAsync()
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
        control.UnregisterPropertyChangedCallback(OverlayControl.TitleContentProperty, titlePropertyChangedRef);
        control.InvokeHidden();
    }

    private void OnChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            var settingsKey = "AppWindow_LastSize_" + control.GetType().FullName.Replace(".", "_");
            settingsService.Save(settingsKey, new Size(control.ActualSize.X, control.ActualSize.Y));
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
        var currentViewRect = applicationView.VisibleBounds;
        var environment = applicationView.WindowingEnvironment;
        if (environment.Kind == WindowingEnvironmentKind.Overlapped)
        {
            var visibleCenter = new Point(currentViewRect.X + (currentViewRect.Width / 2), currentViewRect.Y + currentViewRect.Height / 2);

            var regions = environment.GetDisplayRegions();
            var currentRegion = regions[0];
            foreach (var region in regions)
            {
                var regionRect = new Rect(region.WorkAreaOffset, region.WorkAreaSize);
                if (regionRect.Contains(visibleCenter))
                    currentRegion = region;
            }

            var currentDisplayOffset = currentRegion.WorkAreaOffset;
            var currentDisplaySize = currentRegion.WorkAreaSize;
            var currentDisplayRect = new Rect(currentDisplayOffset, currentDisplaySize);
            var currentDisplayCenter = currentDisplaySize.Width / 2;

            var offsetFromLeftEdge = Math.Max(0, applicationView.VisibleBounds.Left - currentDisplayRect.Left);
            var offsetFromRightEdge = Math.Max(0, currentDisplayRect.Right - applicationView.VisibleBounds.Right);
            var maxWidth = Math.Min(Math.Max(offsetFromLeftEdge, offsetFromRightEdge), currentDisplayRect.Width / 4.0 * 3.0);
            var maxHeight = Math.Min(Math.Min(currentDisplayRect.Height, applicationView.VisibleBounds.Height), currentDisplayRect.Height / 4.0 * 3.0);

            double width = initialSize.Width, height = initialSize.Height;
            SizeHelpers.Scale(ref width, ref height, maxWidth, maxHeight);

            if ((applicationView.AdjacentToLeftDisplayEdge && applicationView.AdjacentToRightDisplayEdge) ||
                Math.Max(offsetFromLeftEdge, offsetFromRightEdge) < width) // not enough space 
            {
                width = initialSize.Width;
                height = initialSize.Height;
                SizeHelpers.Scale(ref width, ref height, currentDisplayRect.Width / 4.0 * 3.0, currentDisplayRect.Height / 4.0 * 3.0);

                appWindow.RequestSize(new Size(width, height + 32));
                appWindow.RequestMoveRelativeToDisplayRegion(currentRegion, new Point((currentDisplayCenter - (width / 2)) + 20, 150));
            }
            else if (offsetFromRightEdge > offsetFromLeftEdge)
            {
                // right
                appWindow.RequestSize(new Size(width, height + 32));
                appWindow.RequestMoveRelativeToCurrentViewContent(new Point(applicationView.VisibleBounds.Width + 8, 0));
            }
            else
            {
                // left
                appWindow.RequestSize(new Size(width, height + 32));
                appWindow.RequestMoveRelativeToCurrentViewContent(new Point(-width - 8, 0));
            }
        }
    }
}
