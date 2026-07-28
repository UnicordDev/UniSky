using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace UniSky.Extensions;

internal static class FrameworkElementExtensions
{
    private static readonly bool SupportsIsLoaded =
        ApiInformation.IsPropertyPresent(typeof(FrameworkElement).FullName, nameof(FrameworkElement.IsLoaded));
        
    public static bool IsLive(this FrameworkElement element)
    {
        if (element == null)
            return false;

        return SupportsIsLoaded
            ? element.IsLoaded
            : VisualTreeHelper.GetParent(element) != null;
    }
}
