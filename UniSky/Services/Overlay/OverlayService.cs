using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Overlay;
using UniSky.Controls.Sheet;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace UniSky.Services.Overlay;

public abstract class OverlayService
{
    private readonly ILogger<OverlayService> logger
        = ServiceContainer.Scoped.GetRequiredService<ILogger<OverlayService>>();

    protected async Task<IOverlayController> ShowOverlayForWindow<T>(Func<T> factory, object parameter) where T : FrameworkElement, IOverlayControl
    {
        try
        {
            if (ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 8, 0))
            {
                return await ShowOverlayForAppWindow<T>(factory, parameter);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to create app window, falling back to CoreWindow! This is probably bad!");
        }

        return await ShowOverlayForCoreWindow<T>(factory, parameter);
    }

    protected async Task<IOverlayController> ShowOverlayForAppWindow<T>(Func<T> factory, object parameter) where T : FrameworkElement, IOverlayControl
    {
        var control = factory();
        var appWindow = await AppWindow.TryCreateAsync();
        if (appWindow == null)
            throw new InvalidOperationException("Failed to create app window! Falling back!");

        var controller = new AppWindowOverlayController(appWindow, control, parameter as IOverlaySizeProvider);
        await controller.ShowAsync(parameter);
        return controller;
    }

    protected async Task<IOverlayController> ShowOverlayForCoreWindow<T>(Func<T> factory, object parameter) where T : FrameworkElement, IOverlayControl
    {
        IOverlayController controller = null;

        var currentViewId = ApplicationView.GetForCurrentView().Id;
        var view = CoreApplication.CreateNewView();
        var newViewId = 0;
        await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        {
            newViewId = ApplicationView.GetForCurrentView().Id;

            var control = factory();
            controller = new ApplicationViewOverlayController(control, currentViewId, newViewId, parameter as IOverlaySizeProvider);
            await controller.ShowAsync(parameter);
        });

        await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId, ViewSizePreference.UseMinimum);

        return controller;
    }
}
