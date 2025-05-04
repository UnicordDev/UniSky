using System;
using System.Threading;
using System.Threading.Tasks;
using UniSky.Controls.Sheet;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniSky.Services;

interface IOverlayRootControl
{
    Task ShowAsync(IOverlayController controller, IOverlayControl control, object param);
    Task<bool> HideAsync();
}

internal class OverlayRootController : IOverlayController
{
    private readonly IOverlayControl control;
    private readonly IOverlayRootControl rootControl;
    private readonly ISafeAreaService safeAreaService;
    private readonly SemaphoreSlim hideSemaphore = new SemaphoreSlim(1, 1);

    private object previousFocus = null;

    public OverlayRootController(IOverlayControl control,
                                         IOverlayRootControl rootControl,
                                         ISafeAreaService safeAreaService)
    {
        this.control = control;
        this.rootControl = rootControl;
        this.safeAreaService = safeAreaService;

        this.control.SetOverlayController(this);
    }

    public UIElement Root => (UIElement)rootControl;
    public bool IsStandalone => false;
    public ISafeAreaService SafeAreaService => safeAreaService;

    public async Task ShowAsync(object param)
    {
        previousFocus = FocusManager.GetFocusedElement();

        await rootControl.ShowAsync(this, control, param);
    }

    public async Task<bool> TryHideAsync()
    {
        if (!await hideSemaphore.WaitAsync(100))
            return false;

        try
        {
            if (!await control.InvokeHidingAsync())
                return false;

            var retVal = await rootControl.HideAsync();
            if (retVal)
            {
                if (ApiInformation.IsMethodPresent(typeof(FocusManager).FullName, "TryFocusAsync")
                    && previousFocus is DependencyObject dep)
                {
                    await FocusManager.TryFocusAsync(dep, FocusState.Programmatic);
                }
                else if (previousFocus is Control control)
                {
                    control.Focus(FocusState.Programmatic);
                }
            }

            return retVal;
        }
        finally
        {
            hideSemaphore.Release();
        }
    }
}
