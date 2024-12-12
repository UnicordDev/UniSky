using Microsoft.Toolkit.Uwp.Helpers;

namespace UniSky.Services;

public enum AppTheme
{
    Performance,
    Fluent,
    SunValley,
    OLED
}

internal class ThemeService(ISettingsService settings) : IThemeService
{
    private const string Themes_AppTheme = "AppTheme";
    private const string Themes_AppThemeSetOnLaunch = "AppThemeSet";

    public AppTheme GetTheme()
    {
        if (settings.TryRead<int>(Themes_AppThemeSetOnLaunch, out var value))
        {
            settings.Save(Themes_AppTheme, value);
            settings.TryDelete(Themes_AppThemeSetOnLaunch);
        }

        if (settings.TryRead<int>(Themes_AppTheme, out var theme))
            return (AppTheme)theme;

        var defaultTheme = GetDefaultAppTheme();
        settings.Save(Themes_AppTheme, (int)defaultTheme);

        return defaultTheme;
    }

    public AppTheme GetThemeForDisplay()
    {
        if (settings.TryRead<int>(Themes_AppThemeSetOnLaunch, out var value))
        {
            return (AppTheme)value;
        }

        if (settings.TryRead<int>(Themes_AppTheme, out var theme))
            return (AppTheme)theme;

        return GetDefaultAppTheme();
    }

    public void SetThemeOnRelaunch(AppTheme theme)
    {
        settings.Save(Themes_AppThemeSetOnLaunch, (int)theme);
    }

    public AppTheme GetDefaultAppTheme()
    {
        //var osBuild = SystemInformation.OperatingSystemVersion.Build;
        //if (osBuild >= 22000)
        //    return AppTheme.SunValley;

        if (SystemInformation.DeviceFamily == "Windows.Mobile")
            return AppTheme.Performance;

        return AppTheme.Fluent;
    }
}
