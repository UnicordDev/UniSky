using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using ColorThiefDotNet;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

//
// Ported from Unicord
// https://github.com/UnicordDev/Unicord/blob/redesign/Unicord.Universal/Interop/BitmapInterop.cs
//

namespace UniSky.Helpers.Interop;

public class BitmapInterop
{

    public enum BI_COMPRESSION : int
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CIEXYZ
    {
        public uint ciexyzX;
        public uint ciexyzY;
        public uint ciexyzZ;

        public CIEXYZ(uint FXPT2DOT30)
        {
            ciexyzX = FXPT2DOT30;
            ciexyzY = FXPT2DOT30;
            ciexyzZ = FXPT2DOT30;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CIEXYZTRIPLE
    {
        public CIEXYZ ciexyzRed;
        public CIEXYZ ciexyzGreen;
        public CIEXYZ ciexyzBlue;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct BITMAPINFOHEADER
    {
        [FieldOffset(0)]
        public uint biSize;
        [FieldOffset(4)]
        public int biWidth;
        [FieldOffset(8)]
        public int biHeight;
        [FieldOffset(12)]
        public ushort biPlanes;
        [FieldOffset(14)]
        public ushort biBitCount;
        [FieldOffset(16)]
        public BI_COMPRESSION biCompression;
        [FieldOffset(20)]
        public uint biSizeImage;
        [FieldOffset(24)]
        public int biXPelsPerMeter;
        [FieldOffset(28)]
        public int biYPelsPerMeter;
        [FieldOffset(32)]
        public uint biClrUsed;
        [FieldOffset(36)]
        public uint biClrImportant;
        [FieldOffset(40)]
        public uint bV5RedMask;
        [FieldOffset(44)]
        public uint bV5GreenMask;
        [FieldOffset(48)]
        public uint bV5BlueMask;
        [FieldOffset(52)]
        public uint bV5AlphaMask;
        [FieldOffset(56)]
        public uint bV5CSType;
        [FieldOffset(60)]
        public CIEXYZTRIPLE bV5Endpoints;
        [FieldOffset(96)]
        public uint bV5GammaRed;
        [FieldOffset(100)]
        public uint bV5GammaGreen;
        [FieldOffset(104)]
        public uint bV5GammaBlue;
        [FieldOffset(108)]
        public uint bV5Intent;
        [FieldOffset(112)]
        public uint bV5ProfileData;
        [FieldOffset(116)]
        public uint bV5ProfileSize;
        [FieldOffset(120)]
        public uint bV5Reserved;

        public const int DIB_RGB_COLORS = 0;

        public BITMAPINFOHEADER(int width, int height, ushort bpp)
        {
            biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            biPlanes = 1;
            biCompression = BI_COMPRESSION.BI_RGB;
            biWidth = width;
            biHeight = height;
            biBitCount = bpp;
            biSizeImage = (uint)(width * height * (bpp >> 3));
            biXPelsPerMeter = 0;
            biYPelsPerMeter = 0;
            biClrUsed = 0;
            biClrImportant = 0;
            bV5RedMask = (uint)255 << 16;
            bV5GreenMask = (uint)255 << 8;
            bV5BlueMask = (uint)255;
            bV5AlphaMask = (uint)255 << 24;
            bV5CSType = 1934772034;
            bV5Endpoints = new CIEXYZTRIPLE();
            bV5Endpoints.ciexyzBlue = new CIEXYZ(0);
            bV5Endpoints.ciexyzGreen = new CIEXYZ(0);
            bV5Endpoints.ciexyzRed = new CIEXYZ(0);
            bV5GammaRed = 0;
            bV5GammaGreen = 0;
            bV5GammaBlue = 0;
            bV5Intent = 4;
            bV5ProfileData = 0;
            bV5ProfileSize = 0;
            bV5Reserved = 0;
        }

        public uint OffsetToPixels
        {
            get
            {
                if (biCompression == BI_COMPRESSION.BI_BITFIELDS)
                {
                    return biSize + (3 * 4);
                }

                return biSize;
            }
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public static async Task<float> GetImageAverageBrightnessAsync(IRandomAccessStream stream)
    {
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var transform = new BitmapTransform() { ScaledWidth = 1, ScaledHeight = 1, InterpolationMode = BitmapInterpolationMode.Fant };

        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
        using var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read);
        using var reference = buffer.CreateReference();

        unsafe
        {
            ((IMemoryBufferByteAccess)reference).GetBuffer(out var raw, out var capacity);

            var quantisedColor = new QuantizedColor(new Color()
            {
                B = raw[0],
                G = raw[1],
                R = raw[2],
                A = raw[3],
            }, 1);

            return quantisedColor.CalculateYiqLuma(quantisedColor.Color) / 255.0f;
        }
    }

    public static async Task<QuantizedColor> GetColor(IRandomAccessStream stream, int quality = 10)
    {
        var decoder = await BitmapDecoder.CreateAsync(stream);

        var thief = new ColorThief();
        return await thief.GetColor(decoder, quality);
    }

    public static async Task<IList<QuantizedColor>> GetColors(IRandomAccessStream stream, int colorCount = 5, int quality = 10)
    {
        var decoder = await BitmapDecoder.CreateAsync(stream);

        var thief = new ColorThief();
        return await thief.GetPalette(decoder, colorCount, quality);
    }

    public static async Task<StorageFile> SaveBitmapToFileAsync(RandomAccessStreamReference streamReference)
    {
        var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Guid.NewGuid()}.png");

        using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
        using (var bmp = await streamReference.OpenReadAsync())
        {
            var decoder = await BitmapDecoder.CreateAsync(bmp);

            using var softwareBmp = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBmp);
            await encoder.FlushAsync();
        }

        return file;
    }

    public static async Task<StorageFile> SaveBitmapToFileAsync(IRandomAccessStream stream)
    {
        var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Guid.NewGuid()}.png");

        var array = new byte[stream.Size];
        await stream.AsStream().ReadAsync(array, 0, array.Length);

        using var softwareBitmap = CreateSoftwareBitmap(array);
        using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
        {
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputStream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();
        }

        return file;
    }

    private static unsafe SoftwareBitmap CreateSoftwareBitmap(byte[] data)
    {
        fixed (byte* dataPtr = data)
        {
            var info = *(BITMAPINFOHEADER*)dataPtr;
            var bytesPerPixel = info.biBitCount >> 3;

            if (info.biSizeImage == 0)
                info.biSizeImage = (uint)(info.biWidth * info.biHeight * bytesPerPixel);

            var stride = -(int)(info.biSizeImage / info.biHeight);
            var scanOffset = info.OffsetToPixels + ((info.biHeight - 1) * (int)(info.biSizeImage / info.biHeight));

            var desiredStride = info.biWidth * 4;
            var bmp = new SoftwareBitmap(BitmapPixelFormat.Bgra8, info.biWidth, info.biHeight, BitmapAlphaMode.Straight);

            using (var buff = bmp.LockBuffer(BitmapBufferAccessMode.Write))
            using (var reference = buff.CreateReference())
            {
                (reference as IMemoryBufferByteAccess).GetBuffer(out var raw, out var capacity);

                if (bytesPerPixel == 3)
                {
                    // UWP doesn't support 24bpp bitmaps so we have to copy this manually
                    for (var row = 0; row < info.biHeight; row++)
                    {
                        var beginOffset = scanOffset + (row * stride);
                        for (var x = 0; x < info.biWidth; x++)
                        {
                            var root = (row * desiredStride) + x;
                            raw[root] = data[beginOffset];
                            raw[root + 1] = data[beginOffset + 1];
                            raw[root + 2] = data[beginOffset + 2];
                            raw[root + 3] = 255;

                            beginOffset += 3;
                        }
                    }
                }
                else
                {
                    // remove padding copying row by row
                    var scan0 = dataPtr + scanOffset;
                    for (int row = 0; row < info.biHeight; row++)
                    {
                        var dataBeginPointer = scan0 + (row * stride);
                        var targetPointer = raw + (row * desiredStride);
                        System.Buffer.MemoryCopy(dataBeginPointer, targetPointer, capacity - (row * desiredStride), info.biWidth * bytesPerPixel);
                    }
                }
            }

            return bmp;
        }
    }
}