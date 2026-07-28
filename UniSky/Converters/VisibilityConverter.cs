using System;
using System.Collections;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniSky.Converters;

public class VisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var visibility = ConvertCore(value);

        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            return visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        return visibility;
    }

    private static Visibility ConvertCore(object value)
    {
        if (value is null)
        {
            return Visibility.Collapsed;
        }

        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        if (value is int i)
        {
            return i > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        if (value is string str)
        {
            return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        if (value is IEnumerable e)
        {
            return e.OfType<object>().Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
