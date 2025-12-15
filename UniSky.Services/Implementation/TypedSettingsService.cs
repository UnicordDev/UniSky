using System;
using System.ComponentModel;
using Windows.System.Profile;
using Windows.UI.Xaml;

using static UniSky.Constants.Settings;

namespace UniSky.Services;

public class TypedSettingsService : ITypedSettings
{
    private readonly ISettingsService settings;

    public TypedSettingsService(ISettingsService settings)
    {
        this.settings = settings;
        this.settings.SettingChanged += OnSettingChanged;
    }

    private void OnSettingChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case REQUESTED_COLOUR_SCHEME:
                SettingChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RequestedColourScheme)));
                break;
            case USE_MULTIPLE_WINDOWS:
                SettingChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseMultipleWindows)));
                break;
            case AUTO_FEED_REFRESH:
                SettingChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoRefreshFeeds)));
                break;
            case USE_TWITTER_LOCALE:
                SettingChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseTwitterLocale)));
                break;
            case VIDEOS_IN_FEEDS:
                SettingChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VideosInFeeds)));
                break;
            case SHOW_FEED_CONTEXT:
                SettingChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowFeedContext)));
                break;
        }
    }

    public event PropertyChangedEventHandler SettingChanged;

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

    public string InstallId
    {
        get
        {
            if (!settings.TryRead(INSTALL_ID, out string installId))
            {
                installId = Guid.NewGuid().ToString();
                settings.Save(INSTALL_ID, installId);
            }

            return installId;
        }
    }
}
