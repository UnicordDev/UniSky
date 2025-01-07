using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    Frame Frame { set; }

    event NavigatedEventHandler Navigated;
    event NavigationFailedEventHandler NavigationFailed;

    bool GoBack();
    void GoForward();
    bool Navigate(Type pageType, object parameter = null, NavigationTransitionInfo infoOverride = null);
    bool Navigate<T>(object parameter = null, NavigationTransitionInfo infoOverride = null) where T : Page;
}