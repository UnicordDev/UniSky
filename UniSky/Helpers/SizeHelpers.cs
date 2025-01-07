using System;

namespace UniSky.Helpers;

internal class SizeHelpers
{
    public static void Scale(ref double width, ref double height, double maxWidth, double maxHeight, StretchMode mode = StretchMode.Uniform)
    {
        if (width <= maxWidth && height <= maxHeight)
            return;

        if (mode == StretchMode.None)
        {
            return;
        }

        if (mode == StretchMode.Fill)
        {
            width = maxWidth;
            height = maxHeight;
            return;
        }

        var ratioX = (double)maxWidth / width;
        var ratioY = (double)maxHeight / height;
        double ratio = 0;

        if (mode == StretchMode.Uniform)
        {
            ratio = Math.Min(ratioX, ratioY);
        }

        if (mode == StretchMode.UniformToFill)
        {
            ratio = Math.Max(ratioX, ratioY);
        }

        width = (int)(width * ratio);
        height = (int)(height * ratio);
    }

    public enum StretchMode
    {
        None, Fill, Uniform, UniformToFill
    }
}

