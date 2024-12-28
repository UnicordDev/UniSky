using Windows.UI.Xaml;

namespace UniSky.Services;

public interface ITypedSettings
{
    ElementTheme RequestedColourScheme { get; set; }
    bool UseMultipleWindows { get; set; }
    bool AutoRefreshFeeds { get; set; }
    bool UseTwitterLocale { get; set; }
    bool VideosInFeeds { get; set; }
}
