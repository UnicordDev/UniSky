using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using UniSky.Helpers.Composition;
using UniSky.Pages;
using UniSky.Services;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

        BirdAnimation.RunBirdAnimation(SheetRoot);
    }
}
