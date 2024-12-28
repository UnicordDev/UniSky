using System.Threading.Tasks;
using UniSky.Models.Embed;
using Windows.Graphics.Imaging;

namespace UniSky.Services;

public interface IEmbedThumbnailGenerator
{
    Task<SoftwareBitmap> GenerateThumbnailAsync(UriEmbedDetails embedDetails);
}