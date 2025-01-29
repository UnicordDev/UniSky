using System.ComponentModel;
using Microsoft.Toolkit.Uwp.Helpers;
using UniSky.Services;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Xaml;

namespace UniSky.ViewModels.Settings;

public class SettingsViewModel : ViewModelBase, ITypedSettings
{
    private readonly ITypedSettings settingsService;
    private readonly IThemeService themeService;
    private readonly int _initialColour;
    private readonly bool _initialTwitterLocale;
    private readonly int _initialTheme;

    public SettingsViewModel(ITypedSettings settingsService, IThemeService themeService)
    {
        this.settingsService = settingsService;
        this.themeService = themeService;
        this.settingsService.SettingChanged += OnSettingChanged;

        _initialColour = (int)settingsService.RequestedColourScheme;
        _initialTwitterLocale = settingsService.UseTwitterLocale;
        _initialTheme = (int)themeService.GetThemeForDisplay();
    }

    private void OnSettingChanged(object sender, PropertyChangedEventArgs e)
    {
        this.OnPropertyChanged(e.PropertyName);
    }

    public bool SunValleyThemeSupported
        => SystemInformation.OperatingSystemVersion.Build >= 17763;

    public ElementTheme RequestedColourScheme
    {
        get => settingsService.RequestedColourScheme;
        set => settingsService.RequestedColourScheme = value;
    }

    public int ColourScheme
    {
        get => (int)RequestedColourScheme;
        set
        {
            RequestedColourScheme = (ElementTheme)value;
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

    public bool ShowFeedContext
    {
        get => settingsService.ShowFeedContext;
        set => settingsService.ShowFeedContext = value;
    }

    public bool IsDirty
        => ApplicationTheme != _initialTheme || ColourScheme != _initialColour || _initialTwitterLocale != UseTwitterLocale;

    public event PropertyChangedEventHandler SettingChanged
    {
        add
        {
            settingsService.SettingChanged += value;
        }

        remove
        {
            settingsService.SettingChanged -= value;
        }
    }
}