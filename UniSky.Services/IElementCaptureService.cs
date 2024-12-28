using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;

namespace UniSky.Services;

public interface IElementCaptureService
{
    Task<SoftwareBitmap> CaptureElementAsync(Func<UIElement> element, Size targetSize);
}