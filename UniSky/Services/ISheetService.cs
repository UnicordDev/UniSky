using System.Threading.Tasks;
using UniSky.Controls.Sheet;

namespace UniSky.Services
{
    public interface ISheetService
    {
        Task ShowAsync<T>() where T : SheetControl, new();
        Task<bool> TryCloseAsync();
    }
}