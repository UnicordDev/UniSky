using System.ComponentModel;
using Windows.UI.Xaml;

namespace UniSky.Services;

public interface ITypedSettings
{
    event PropertyChangedEventHandler SettingChanged;
    ElementTheme RequestedColourScheme { get; set; }
    bool UseMultipleWindows { get; set; }
    bool AutoRefreshFeeds { get; set; }
    bool UseTwitterLocale { get; set; }
    bool VideosInFeeds { get; set; }
    bool ShowFeedContext { get; set; }
    string InstallId { get; }
}
