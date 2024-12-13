using System;
using System.Threading.Tasks;
using UniSky.Controls.Sheet;

namespace UniSky.Services
{
    public interface ISheetService
    {
        Task<IOverlayController> ShowAsync<T>(object parameter = null) where T : SheetControl, new();
        Task<IOverlayController> ShowAsync<T>(Func<SheetControl> factory, object parameter = null) where T : SheetControl;
    }
}