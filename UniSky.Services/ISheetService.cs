using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace UniSky.Services;

public interface ISheetService
{
    Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : FrameworkElement, ISheetControl, new();
    Task<IOverlayController> ShowAsync<T>(Func<T> factory, object parameter = null) where T : FrameworkElement, ISheetControl;
}