using System.Threading.Tasks;
using UniSky.Controls.Overlay;
using Windows.Foundation;

namespace UniSky.Services.Overlay;

public interface IOverlaySizeProvider
{
    Size? GetDesiredSize();
}

public interface IStandardOverlayService
{
    Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : StandardOverlayControl, new();
}

internal class StandardOverlayService : OverlayService, IStandardOverlayService
{
    public Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : StandardOverlayControl, new()
    {
        return base.ShowOverlayForWindow<T>(() => new T(), parameter);
    }
}
