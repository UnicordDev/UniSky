using System.Threading.Tasks;
using UniSky.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UniSky.Services;

public interface IImageCompressionService
{
    Task<CompressedImageStream> CompressSoftwareBitmapAsync(SoftwareBitmap softwareBitmap, IRandomAccessStream outputStream, int size = 4096);
    Task<CompressedImageFile> CompressStorageFileAsync(IStorageFile input, int size = 4096);
}