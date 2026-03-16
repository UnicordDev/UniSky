using System;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;

namespace UniSky.Utilities;

internal static class WebPHelpers
{
    private static readonly Lazy<bool> hasWebPSupport 
        = new Lazy<bool>(CheckWebPSupport);

    public static bool HasWebPCodec
        => hasWebPSupport.Value;

    private static bool CheckWebPSupport()
    {
        if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7, 0))
            return false;

        foreach (var item in BitmapDecoder.GetDecoderInformationEnumerator())
        {
            if (item.CodecId == BitmapDecoder.WebpDecoderId)
                return true;
        }

        return false;
    }
}