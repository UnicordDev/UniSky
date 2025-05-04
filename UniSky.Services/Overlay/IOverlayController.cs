using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace UniSky.Services;


public interface IOverlayController
{
    UIElement Root { get; } 
    bool IsStandalone { get; }
    ISafeAreaService SafeAreaService { get; }
    Task ShowAsync(object parameter);
    Task<bool> TryHideAsync();
}
