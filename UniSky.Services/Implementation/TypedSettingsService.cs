using Windows.System.Profile;
using Windows.UI.Xaml;

using static UniSky.Constants.Settings;

namespace UniSky.Services;

public class TypedSettingsService(ISettingsService settings) : ITypedSettings
{
    // typed settings
    public ElementTheme RequestedColourScheme
    {
        get => (ElementTheme)settings.Read<int>(REQUESTED_COLOUR_SCHEME, REQUESTED_COLOUR_SCHEME_DEFAULT);
        set => settings.Save(REQUESTED_COLOUR_SCHEME, (int)value);
    }

    public bool UseMultipleWindows
    {
        get => settings.Read(USE_MULTIPLE_WINDOWS, AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop");
        set => settings.Save(USE_MULTIPLE_WINDOWS, value);
    }

    public bool AutoRefreshFeeds
    {
        get => settings.Read(AUTO_FEED_REFRESH, AUTO_FEED_REFRESH_DEFAULT);
        set => settings.Save(AUTO_FEED_REFRESH, value);
    }

    public bool UseTwitterLocale
    {
        get => settings.Read(USE_TWITTER_LOCALE, USE_TWITTER_LOCALE_DEFAULT);
        set => settings.Save(USE_TWITTER_LOCALE, value);
    }

    public bool VideosInFeeds
    {
        get => settings.Read(VIDEOS_IN_FEEDS, VIDEOS_IN_FEEDS_DEFAULT);
        set => settings.Save(VIDEOS_IN_FEEDS, value);
    }
    public bool ShowFeedContext
    {
        get => settings.Read(SHOW_FEED_CONTEXT, SHOW_FEED_CONTEXT_DEFAULT);
        set => settings.Save(SHOW_FEED_CONTEXT, value);
    }
}
