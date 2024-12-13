using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSky.Controls.Overlay;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace UniSky.Services.Overlay;

public abstract class OverlayService
{
    protected async Task<IOverlayController> ShowOverlayForAppWindow<T>(Func<OverlayControl> factory, object parameter) where T : OverlayControl
    {
        var control = factory();
        var appWindow = await AppWindow.TryCreateAsync();

        var controller = new AppWindowOverlayController(appWindow, control);
        control.SetOverlayController(controller);
        control.InvokeShowing(parameter);

        ElementCompositionPreview.SetAppWindowContent(appWindow, control);

        await appWindow.TryShowAsync();

        control.InvokeShown();

        return controller;
    }

    protected async Task<IOverlayController> ShowOverlayForCoreWindow<T>(Func<OverlayControl> factory, object parameter) where T : OverlayControl
    {
        IOverlayController controller = null;

        var currentViewId = ApplicationView.GetForCurrentView().Id;
        var view = CoreApplication.CreateNewView();
        var newViewId = 0;
        await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            newViewId = ApplicationView.GetForCurrentView().Id;

            var control = factory();
            controller = new ApplicationViewOverlayController(control, currentViewId, newViewId);
            control.SetOverlayController(controller);

            Window.Current.Content = control;
            Window.Current.Activate();

            control.InvokeShowing(parameter);
            control.InvokeShown();
        });

        await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId, ViewSizePreference.UseMinimum);

        return controller;
    }
}
