using System.Threading.Tasks;
using UniSky.Controls.Sheet;
using Windows.UI.Xaml;

namespace UniSky.Services;

internal class SheetRootController(SheetRootControl rootControl,
                                   ISafeAreaService safeAreaService) : IOverlayController
{
    public UIElement Root => rootControl;
    public bool IsStandalone => false;
    public ISafeAreaService SafeAreaService => safeAreaService;

    public async Task<bool> TryHideSheetAsync()
    {
        return await rootControl.HideSheetAsync();
    }
}
