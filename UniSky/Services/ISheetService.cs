using System;
using System.Threading.Tasks;
using UniSky.Controls.Sheet;

namespace UniSky.Services
{
    public interface ISheetService
    {
        Task<ISheetController> ShowAsync<T>(object parameter = null) where T : SheetControl, new();
        Task<ISheetController> ShowAsync<T>(Func<SheetControl> factory, object parameter = null) where T : SheetControl;
    }
}