using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Sheet;
using UniSky.Services.Overlay;
using Windows.UI.Xaml;

namespace UniSky.Services;

internal class SheetService(ITypedSettings settingsService) : OverlayService, ISheetService
{
    private readonly SheetRootControl sheetRoot = Window.Current.Content.FindDescendant<SheetRootControl>();

    public Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : SheetControl, new()
        => ShowAsync<T>(() => new T(), parameter);

    public async Task<IOverlayController> ShowAsync<T>(Func<SheetControl> factory, object parameter = null) where T : SheetControl
    {
        if (sheetRoot == null || settingsService.UseMultipleWindows)
            return await ShowOverlayForWindow<T>(factory, parameter);

        var safeArea = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();

        var control = factory();
        var controller = new SheetRootController(sheetRoot, safeArea);

        control.SetOverlayController(controller);

        sheetRoot.ShowSheet(control, parameter);
        return controller;
    }
}
