using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Overlay;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.Services.Overlay;

internal class ApplicationViewOverlayController : IOverlayController
{
    private readonly IOverlayControl control;
    private readonly int hostViewId;
    private readonly int viewId;
    private readonly ISettingsService settingsService;
    private readonly IOverlaySizeProvider overlaySizeProvider;

    private readonly string settingsKey;
    private bool hasActivated = false;
    private long titlePropertyChangedRef;

    private FrameworkElement Control
        => (FrameworkElement)control;

    public ApplicationViewOverlayController(IOverlayControl control,
                                            int hostViewId,
                                            int viewId,
                                            IOverlaySizeProvider overlaySizeProvider)
    {
        this.control = control;
        this.hostViewId = hostViewId;
        this.viewId = viewId;
        this.overlaySizeProvider = overlaySizeProvider;
        this.settingsService = ServiceContainer.Scoped.GetRequiredService<ISettingsService>();
        this.settingsKey = "CoreWindow_LastSize_" + control.GetType().FullName.Replace(".", "_");

        // a surprise tool that'll help us later
        // (instanciating this now so it handles min. window sizes, etc.)
        this.SafeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();

        var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
        systemNavigationManager.BackRequested += OnBackRequested;

        if (ApiInformation.IsTypePresent("Windows.UI.Core.Preview.SystemNavigationManagerPreview"))
        {
            var systemNavigationManagerPreview = SystemNavigationManagerPreview.GetForCurrentView();
            systemNavigationManagerPreview.CloseRequested += OnCloseRequested;
        }

        var coreWindow = CoreWindow.GetForCurrentThread();
        coreWindow.SizeChanged += OnWindowSizeChanged;
        coreWindow.Activated += OnActivated;
        coreWindow.Closed += OnWindowClosed;

        titlePropertyChangedRef = Control.RegisterPropertyChangedCallback(OverlayControl.TitleContentProperty, OnTitleChanged);
        OnTitleChanged(Control, OverlayControl.TitleContentProperty);

        Window.Current.Content = new Grid()
        {
            Children =
            {
                new Canvas() { Name = "RenderTargetRoot" },
                (UIElement)control,
            }
        };
    }

    private void OnTitleChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (dp != OverlayControl.TitleContentProperty)
            return;

        var applicationView = ApplicationView.GetForCurrentView();
        applicationView.Title = control.TitleContent?.ToString() ?? "";
    }

    private void OnActivated(CoreWindow sender, WindowActivatedEventArgs args)
    {
        if (!hasActivated)
        {
            var initialSize = settingsService.Read(settingsKey, control.PreferredWindowSize);
            if (overlaySizeProvider != null)
            {
                var size = overlaySizeProvider.GetDesiredSize();
                if (size != null)
                    initialSize = size.Value;
            }

            var applicationView = ApplicationView.GetForCurrentView();
            applicationView.TryResizeView(initialSize);

            hasActivated = true;
        }
    }

    public UIElement Root => (UIElement)control;
    public bool IsStandalone => true;
    public ISafeAreaService SafeAreaService { get; }

    public async Task<bool> TryHideSheetAsync()
    {
        if (await control.InvokeHidingAsync())
        {
            await ApplicationViewSwitcher.SwitchAsync(hostViewId, viewId, ApplicationViewSwitchingOptions.ConsolidateViews);
            this.Control.UnregisterPropertyChangedCallback(OverlayControl.TitleContentProperty, this.titlePropertyChangedRef);
            return true;
        }

        return false;
    }

    private async void OnBackRequested(object sender, BackRequestedEventArgs e)
    {
        e.Handled = true;
        await TryHideSheetAsync();
    }

    private async void OnCloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
    {
        var deferral = e.GetDeferral();
        if (!await control.InvokeHidingAsync())
            e.Handled = true;

        deferral.Complete();
    }

    private void OnWindowClosed(object sender, CoreWindowEventArgs e)
    {
        control.InvokeHidden();
    }

    private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        settingsService.Save(settingsKey, e.Size);
    }
}
