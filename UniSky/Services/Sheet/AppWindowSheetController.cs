using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Sheet;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;

namespace UniSky.Services;

public class AppWindowSheetController : ISheetController
{
    private readonly AppWindow appWindow;
    private readonly SheetControl control;
    private readonly ISettingsService settingsService;

    private readonly string settingsKey;

    public AppWindowSheetController(AppWindow window, SheetControl control)
    {
        this.appWindow = window;
        this.control = control;
        this.settingsService = ServiceContainer.Scoped.GetRequiredService<ISettingsService>();
        this.SafeAreaService = new AppWindowSafeAreaService(appWindow);

        this.settingsKey = "CoreWindow_LastSize_" + control.GetType().FullName.Replace(".", "_");
        var initialSize = settingsService.Read(settingsKey, control.PreferredWindowSize);

        appWindow.PersistedStateId = settingsKey;
        appWindow.CloseRequested += OnCloseRequested;
        appWindow.Closed += OnClosed;
        appWindow.Changed += OnChanged;
        appWindow.RequestSize(initialSize);

        PlaceAppWindow(initialSize);
    }

    public UIElement Root => control;
    public bool IsFullWindow => true;
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
            if ((applicationView.AdjacentToLeftDisplayEdge && applicationView.AdjacentToRightDisplayEdge) ||
                Math.Max(offsetFromLeftEdge, offsetFromRightEdge) < initialSize.Width) // not enough space 
            {
                appWindow.RequestMoveRelativeToDisplayRegion(currentRegion, new Point((currentDisplayCenter - (initialSize.Width / 2)) + 20, 150));
            }
            else if (offsetFromRightEdge > offsetFromLeftEdge)
            {
                // right
                appWindow.RequestMoveRelativeToCurrentViewContent(new Point(applicationView.VisibleBounds.Width + 8, 0));
            }
            else
            {
                // left
                appWindow.RequestMoveRelativeToCurrentViewContent(new Point(-initialSize.Width - 8, 0));
            }
        }
    }
}
