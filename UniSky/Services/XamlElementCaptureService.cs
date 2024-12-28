using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UniSky.Services;

public class XamlElementCaptureService : IElementCaptureService
{
    private readonly CoreDispatcher dispatcher;
    private readonly Canvas rootElement;

    public XamlElementCaptureService()
    {
        this.dispatcher = Window.Current.Dispatcher;
        this.rootElement = (Canvas)Window.Current.Content.FindDescendantByName("RenderTargetRoot");
    }

    public async Task<SoftwareBitmap> CaptureElementAsync(Func<UIElement> elementFactory, Size targetSize)
    {
        TaskCompletionSource<SoftwareBitmap> softwareBitmapCompletion = new TaskCompletionSource<SoftwareBitmap>();
        await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
        {
            var element = elementFactory();

            try
            {
                if (VisualTreeHelper.GetParent(element) == null)
                    rootElement.Children.Add(element);

                element.Measure(targetSize);
                element.Arrange(new Rect(new Point(), targetSize));

                var rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(element);

                var pixels = await rtb.GetPixelsAsync();
                var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                    pixels, 
                    BitmapPixelFormat.Bgra8, 
                    rtb.PixelWidth,
                    rtb.PixelHeight, 
                    BitmapAlphaMode.Premultiplied);

                softwareBitmapCompletion.SetResult(softwareBitmap);
            }
            catch (Exception ex)
            {
                softwareBitmapCompletion.TrySetException(ex);
            }
            finally
            {
                rootElement.Children.Remove(element);
            }
        });

        return await softwareBitmapCompletion.Task;
    }
}
