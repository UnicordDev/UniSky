using System;
using System.Linq;
using System.Threading.Tasks;
using UniSky.Helpers;
using UniSky.Models;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UniSky.Services;

public class ImageCompressionService : IImageCompressionService
{
    private static bool isHeifEncoderMissing = false;

    public async Task<CompressedImageFile> CompressStorageFileAsync(IStorageFile input, int size = 4096)
    {
        return await CompressImageAsync(input, CheckHeifSupport(), size);
    }

    public async Task<CompressedImageStream> CompressSoftwareBitmapAsync(
        SoftwareBitmap softwareBitmap,
        IRandomAccessStream outputStream,
        int size = 4096)
    {
        return await CompressSoftwareBitmapAsync(softwareBitmap,
                                                 outputStream,
                                                 CheckHeifSupport(),
                                                 size,
                                                 (int)softwareBitmap.PixelWidth,
                                                 (int)softwareBitmap.PixelHeight);
    }

    private async Task<CompressedImageFile> CompressImageAsync(IStorageFile input, bool useHeif, int size)
    {
        var output = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Guid.NewGuid()}");

        using var inputStream = await input.OpenAsync(FileAccessMode.Read);
        using var outputStream = await output.OpenAsync(FileAccessMode.ReadWrite);

        var decoder = await BitmapDecoder.CreateAsync(inputStream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
        var compressedStream = await CompressSoftwareBitmapAsync(
            softwareBitmap,
            outputStream,
            useHeif,
            size,
            (int)decoder.OrientedPixelWidth,
            (int)decoder.OrientedPixelHeight);

        // dodgy logic but sure it'll probably work
        if (compressedStream.ContentType.Contains("jpeg") ||
            compressedStream.ContentType.Contains("jpg"))
        {
            await output.RenameAsync($"{Guid.NewGuid()}.jpeg");
        }
        else
        {
            await output.RenameAsync($"{Guid.NewGuid()}.heic");
        }

        return new CompressedImageFile(compressedStream.Width, compressedStream.Height, compressedStream.ContentType, output);
    }

    private async Task<CompressedImageStream> CompressSoftwareBitmapAsync(
        SoftwareBitmap softwareBitmap,
        IRandomAccessStream outputStream,
        bool useHeif,
        int size,
        int? rawWidth,
        int? rawHeight)
    {
        double width = rawWidth ?? (double)softwareBitmap.PixelWidth;
        double height = rawHeight ?? (double)softwareBitmap.PixelHeight;

        var contentType = useHeif ? "image/heic" : "image/jpeg";
        var codec = useHeif ? BitmapEncoder.HeifEncoderId : BitmapEncoder.JpegEncoderId;

        // for reasons that dont entirely make sense, this needs to run on a separate thread
        return await Task.Run(async () =>
        {
            try
            {
                do
                {
                    outputStream.Size = 0;
                    SizeHelpers.Scale(ref width, ref height, size, size);

                    var propertySet = new BitmapPropertySet()
                    {
                        ["ImageQuality"] = new BitmapTypedValue(0.9, PropertyType.Single),
                    };

                    var encoder = await BitmapEncoder.CreateAsync(codec, outputStream, propertySet);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    encoder.BitmapTransform.ScaledWidth = (uint)Math.Ceiling(width);
                    encoder.BitmapTransform.ScaledHeight = (uint)Math.Ceiling(height);
                    encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;

                    await encoder.FlushAsync();

                    contentType = encoder.EncoderInformation.MimeTypes.FirstOrDefault()
                            ?? contentType;
                    size = (int)Math.Floor(size * 0.75);
                }
                while (outputStream.Size > 1_000_000);

                return new CompressedImageStream((int)Math.Ceiling(width), (int)Math.Ceiling(height), contentType, outputStream);
            }
            catch (Exception ex) when ((uint)ex.HResult == 0xc00d5212) // missing heif codec
            {
                isHeifEncoderMissing = true;
                return await CompressSoftwareBitmapAsync(softwareBitmap, outputStream, false, size, rawWidth, rawHeight);
            }
        });
    }

    private static bool CheckHeifSupport()
    {
        if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7, 0))
            return false;

        if (isHeifEncoderMissing)
            return false;

        foreach (var item in BitmapEncoder.GetEncoderInformationEnumerator())
        {
            if (item.CodecId == BitmapEncoder.HeifEncoderId)
                return true;
        }

        return false;
    }
}
