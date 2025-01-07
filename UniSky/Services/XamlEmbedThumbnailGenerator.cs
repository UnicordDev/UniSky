using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Storage.Streams;
using UniSky.Controls.Compose.Renderer;
using UniSky.Models.Embed;
using Windows.Foundation;

namespace UniSky.Services;

public class XamlEmbedThumbnailGenerator(IElementCaptureService elementCaptureService) : IEmbedThumbnailGenerator
{
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;

    public async Task<SoftwareBitmap> GenerateThumbnailAsync(UriEmbedDetails embedDetails)
    {
        if (embedDetails.Image == null)
        {
            // TODO: in this case we can do an article renderer
            return null;
        }

        TaskCompletionSource<SoftwareBitmap> softwareBitmapCompletion = new TaskCompletionSource<SoftwareBitmap>();
        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        {
            try
            {
                var renderer = new ComposeImageRenderer();
                await renderer.LoadImage(RandomAccessStreamReference.CreateFromUri(new Uri(embedDetails.Image.Value.Url)));

                var bitmap = await elementCaptureService.CaptureElementAsync(() => renderer, new Size(640, 320));
                softwareBitmapCompletion.SetResult(bitmap);
            }
            catch (Exception ex)
            {
                softwareBitmapCompletion.TrySetException(ex);
            }
        });

        return await softwareBitmapCompletion.Task;
    }
}
