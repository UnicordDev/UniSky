using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Overlay;
using UniSky.Controls.Sheet;
using UniSky.Services.Overlay;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace UniSky.Services;

internal class SheetService : OverlayService, ISheetService
{
    private readonly SheetRootControl sheetRoot;
    private readonly ITypedSettings settingsService;

    public SheetService(ITypedSettings settingsService)
    {
        this.settingsService = settingsService;
        this.sheetRoot = Window.Current.Content.FindDescendant<SheetRootControl>();
    }

    public Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : SheetControl, new()
        => ShowAsync<T>(() => new T(), parameter);

    public async Task<IOverlayController> ShowAsync<T>(Func<SheetControl> factory, object parameter = null) where T : SheetControl
    {
        if (sheetRoot != null && !settingsService.UseMultipleWindows)
        {
            var safeArea = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();

            var control = factory();
            var controller = new SheetRootController(sheetRoot, safeArea);

            control.SetOverlayController(controller);

            sheetRoot.ShowSheet(control, parameter);
            return controller;
        }
        else
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
    }
}
