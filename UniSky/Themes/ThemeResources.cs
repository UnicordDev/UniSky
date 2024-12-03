using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using UniSky.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace UniSky.Themes
{
    internal class ThemeResources : ResourceDictionary
    {
        public ThemeResources()
        {
            var theme = Ioc.Default.GetRequiredService<IThemeService>()
                .GetTheme();

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
}
