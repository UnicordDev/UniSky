using System;
using Windows.UI.Xaml;

namespace UniSky.Services;

public interface ISafeAreaService
{
    SafeAreaInfo State { get; }

    event EventHandler<SafeAreaUpdatedEventArgs> SafeAreaUpdated;

    void SetTitleBar(UIElement uiElement);
    void SetTitlebarTheme(ElementTheme theme);
}