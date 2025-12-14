using System.Reflection;
using UniSky.Models;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace UniSky;

public static class Constants
{
    public const string CLIENT_ID = "https://u.hasthebig.gay/~wam/unisky/client-metadata.json";
    public const string OAUTH_CALLBACK = "https://u.hasthebig.gay/~wam/unisky/oauth-callback.html";

    public static string Version
    {
        get
        {
            var gitSha = "";
            var versionedAssembly = typeof(LoginModel).Assembly;
            var attribute = versionedAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            int idx;
            if (attribute != null && (idx = attribute.InformationalVersion.IndexOf('+')) != -1)
            {
                gitSha = "-" + attribute.InformationalVersion.Substring(idx + 1);
            }

            var v = Package.Current.Id.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}{gitSha}";
        }
    }

    public static string UserAgent
        => $"UniSky/{Version} (https://github.com/UnicordDev/UniSky)";

    public static string CrawlerUserAgent
        => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 UniSky/{Version} (https://github.com/UnicordDev/UniSky)";

    public static class Settings
    {
        public const string REQUESTED_COLOUR_SCHEME = "RequestedColourScheme_v1";
        public const int REQUESTED_COLOUR_SCHEME_DEFAULT = (int)ElementTheme.Default;

        public const string USE_MULTIPLE_WINDOWS = "UseMultipleWindows_v1";
        // default: calculated

        public const string AUTO_FEED_REFRESH = "AutoRefreshFeeds_v1";
        public const bool AUTO_FEED_REFRESH_DEFAULT = true;

        public const string USE_TWITTER_LOCALE = "UseTwitterLocale_v1";
        public const bool USE_TWITTER_LOCALE_DEFAULT = false;

        public const string VIDEOS_IN_FEEDS = "VideosInFeeds_v1";
        public const bool VIDEOS_IN_FEEDS_DEFAULT = true;

        public const string SHOW_FEED_CONTEXT = "ShowFeedContext_v1";
        public const bool SHOW_FEED_CONTEXT_DEFAULT = false;
    }
}
