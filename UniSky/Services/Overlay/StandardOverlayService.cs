using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Overlay;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace UniSky.Services.Overlay;

public interface IOverlaySizeProvider
{
    Size? GetDesiredSize();
}

public interface IStandardOverlayService
{
    Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : StandardOverlayControl, new();
}

internal class StandardOverlayService(ITypedSettings settingsService, ISafeAreaService safeAreaService) : OverlayService, IStandardOverlayService
{
    private readonly OverlayRootControl overlayRoot
        = Window.Current.Content.FindDescendant<OverlayRootControl>();

    public async Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : StandardOverlayControl, new()
    {
        if (overlayRoot == null || settingsService.UseMultipleWindows)
            return await base.ShowOverlayForWindow<T>(() => new T(), parameter);

        var control = new T();
        var controller = new OverlayRootController(control, overlayRoot, safeAreaService);
        await controller.ShowAsync(parameter);
        return controller;
    }
}
