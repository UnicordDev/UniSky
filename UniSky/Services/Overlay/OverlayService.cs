﻿using System;
using System.Threading.Tasks;
using UniSky.Controls.Overlay;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace UniSky.Services.Overlay;

internal abstract class OverlayService
{
    protected async Task<IOverlayController> ShowOverlayForWindow<T>(Func<T> factory, object parameter) where T : FrameworkElement, IOverlayControl
    {
        if (ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 8, 0))
        {
            return await ShowOverlayForAppWindow<T>(factory, parameter);
        }
        else
        {
            return await ShowOverlayForCoreWindow<T>(factory, parameter);
        }
    }

    protected async Task<IOverlayController> ShowOverlayForAppWindow<T>(Func<T> factory, object parameter) where T : FrameworkElement, IOverlayControl
    {
        var control = factory();
        var appWindow = await AppWindow.TryCreateAsync();

        var controller = new AppWindowOverlayController(appWindow, control, parameter as IOverlaySizeProvider);
        control.SetOverlayController(controller);
        control.InvokeShowing(parameter);

        ElementCompositionPreview.SetAppWindowContent(appWindow, control);

        await appWindow.TryShowAsync();

        control.InvokeShown();

        return controller;
    }

    protected async Task<IOverlayController> ShowOverlayForCoreWindow<T>(Func<T> factory, object parameter) where T : FrameworkElement, IOverlayControl
    {
        IOverlayController controller = null;

        var currentViewId = ApplicationView.GetForCurrentView().Id;
        var view = CoreApplication.CreateNewView();
        var newViewId = 0;
        await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            newViewId = ApplicationView.GetForCurrentView().Id;

            var control = factory();
            controller = new ApplicationViewOverlayController(control, currentViewId, newViewId, parameter as IOverlaySizeProvider);
            control.SetOverlayController(controller);

            Window.Current.Activate();

            control.InvokeShowing(parameter);
            control.InvokeShown();
        });

        await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId, ViewSizePreference.UseMinimum);

        return controller;
    }
}
