using System;
using CommunityToolkit.Mvvm.DependencyInjection;
#if WINDOWS_UWP
using UniSky.Helpers.Composition;
#endif
using UniSky.Pages;
using UniSky.Services;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UniSky;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RootPage : Page
{
    public RootPage()
    {
        this.InitializeComponent();
        Loaded += RootPage_Loaded;
    }

    private void RootPage_Loaded(object sender, RoutedEventArgs e)
    {
        var serviceLocator = Ioc.Default.GetRequiredService<INavigationServiceLocator>();
        var service = serviceLocator.GetNavigationService("Root");
        service.Frame = RootFrame;

        var sessionService = Ioc.Default.GetRequiredService<SessionService>();
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue("LastUsedUser", out var userObj) &&
            userObj is string user &&
            sessionService.TryFindSession(user, out var session))
        {
            service.Navigate<HomePage>(user);
        }
        else
        {
            service.Navigate<LoginPage>();
        }

#if WINDOWS_UWP
        BirdAnimation.RunBirdAnimation(RootFrame);
#endif
    }
}
