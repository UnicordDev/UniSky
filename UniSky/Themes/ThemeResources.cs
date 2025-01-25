using System;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Services;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace UniSky.Themes;

internal class ThemeResources : ResourceDictionary
{
    public ThemeResources()
    {
        AppTheme theme;
        if (!DesignMode.DesignModeEnabled)
        {
            theme = ServiceContainer.Scoped.GetRequiredService<IThemeService>()
                .GetTheme();
        }
        else
        {
            theme = AppTheme.Fluent;
        }

        Uri uri = theme switch
        {
            AppTheme.OLED => new Uri("ms-appx:///Themes/OLED.xaml"),
            AppTheme.Fluent => new Uri("ms-appx:///Themes/Fluent.xaml"),
            AppTheme.Performance => new Uri("ms-appx:///Themes/Performance.xaml"),
            AppTheme.SunValley => new Uri("ms-appx:///Themes/SunValley.xaml"),
            _ => throw new InvalidOperationException("Unknown theme"),
        };

        Application.LoadComponent(this, uri, ComponentResourceLocation.Application);
    }
}
