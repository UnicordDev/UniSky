using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSky.Controls.Overlay;
using UniSky.Controls.Sheet;
using Windows.Foundation;

namespace UniSky.Services.Overlay;

public interface IOverlaySizeProvider
{
    Size? GetDesiredSize();
}

public interface IGenericOverlayService
{
    Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : GenericOverlayControl, new();
}

internal class GenericOverlayService : OverlayService, IGenericOverlayService
{
    public Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : GenericOverlayControl, new()
    {
        return base.ShowOverlayForWindow<T>(() => new T(), parameter);
    }
}
