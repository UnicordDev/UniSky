using Microsoft.Toolkit.Uwp.Helpers;
using UniSky.Services;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Xaml;

namespace UniSky.ViewModels.Settings;

public class SettingsViewModel(ITypedSettings settingsService, IThemeService themeService) : ViewModelBase
{
    private readonly int _initialColour = (int)settingsService.RequestedColourScheme;
    private readonly bool _initialTwitterLocale = settingsService.UseTwitterLocale;
    private readonly int _initialTheme = (int)themeService.GetThemeForDisplay();

    public bool SunValleyThemeSupported
        => SystemInformation.OperatingSystemVersion.Build >= 17763;

    public int ColourScheme
    {
        get => (int)settingsService.RequestedColourScheme;
        set
        {
            settingsService.RequestedColourScheme = (ElementTheme)value;
            OnPropertyChanged(nameof(ColourScheme));
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    public int ApplicationTheme
    {
        get => (int)themeService.GetThemeForDisplay();
        set
        {
            themeService.SetThemeOnRelaunch((AppTheme)value);
            OnPropertyChanged(nameof(ApplicationTheme));
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    public bool UseMultipleWindows
    {
        get => settingsService.UseMultipleWindows;
        set => settingsService.UseMultipleWindows = value;
    }

    public bool AutoRefreshFeeds
    {
        get => settingsService.AutoRefreshFeeds;
        set => settingsService.AutoRefreshFeeds = value;
    }

    public bool UseTwitterLocale
    {
        get => settingsService.UseTwitterLocale;
        set
        {
            settingsService.UseTwitterLocale = value;
            ResourceContext.SetGlobalQualifierValue("Custom", value ? "Twitter" : "", ResourceQualifierPersistence.LocalMachine);
        }
    }

    public bool VideosInFeeds
    {
        get => settingsService.VideosInFeeds;
        set => settingsService.VideosInFeeds = value;
    }

    public bool IsDirty
        => ApplicationTheme != _initialTheme || ColourScheme != _initialColour || _initialTwitterLocale != UseTwitterLocale;
}