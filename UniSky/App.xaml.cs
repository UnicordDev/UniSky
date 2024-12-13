using System;
using Humanizer.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Extensions;
using UniSky.Helpers.Localisation;
using UniSky.Services;
using UniSky.Services.Overlay;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

namespace UniSky;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
sealed partial class App : Application
{
    private ILogger<App> _logger;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.ConfigureServices();

        this.InitializeComponent();
        this.Suspending += OnSuspending;
        this.UnhandledException += OnUnhandledException;

        _logger = ServiceContainer.Default.GetRequiredService<ILoggerFactory>()
            .CreateLogger<App>();

        // ResourceContext.SetGlobalQualifierValue("Custom", "Twitter", ResourceQualifierPersistence.LocalMachine);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unhandled exception!!");

        // hate this
        e.Handled = true;
    }

    private void ConfigureServices()
    {
        var collection = new ServiceCollection();
        collection.AddLogging(c => c.AddDebug()
            .SetMinimumLevel(LogLevel.Trace));

        collection.AddSingleton<IProtocolService, ProtocolService>();
        collection.AddSingleton<ISettingsService, SettingsService>();
        collection.AddSingleton<ITypedSettings, SettingsService>();
        collection.AddSingleton<IThemeService, ThemeService>();
        collection.AddSingleton<INavigationServiceLocator, NavigationServiceLocator>();
        collection.AddScoped<ISafeAreaService, ApplicationViewSafeAreaService>();
        collection.AddScoped<ISheetService, SheetService>();
        collection.AddScoped<IStandardOverlayService, StandardOverlayService>();

        collection.AddTransient<LoginService>();
        collection.AddTransient<SessionService>();

        ServiceContainer.Default.ConfigureServices(collection.BuildServiceProvider());
        
        Configurator.Formatters.Register("en", (locale) => new ShortTimespanFormatter("en"));
        Configurator.Formatters.Register("en-GB", (locale) => new ShortTimespanFormatter("en"));
        Configurator.Formatters.Register("en-US", (locale) => new ShortTimespanFormatter("en"));
    }

    protected override void OnActivated(IActivatedEventArgs args)
    {
        if (args is ProtocolActivatedEventArgs e)
        {
            this.OnProtocolActivated(e);
        }
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        Hairline.Initialize();

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (Window.Current.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            rootFrame.NavigationFailed += OnNavigationFailed;

            if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                //TODO: Load state from previously suspended application
            }

            // Place the frame in the current Window
            Window.Current.Content = rootFrame;
        }

        if (e.PrelaunchActivated == false)
        {
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(RootPage), e.Arguments);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }
    }

    private void OnProtocolActivated(ProtocolActivatedEventArgs e)
    {
        Hairline.Initialize();
        if (Window.Current.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            rootFrame.Navigate(typeof(RootPage));
            Window.Current.Content = rootFrame;
        }

        // Ensure the current window is active
        Window.Current.Activate();
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }

    /// <summary>
    /// Invoked when application execution is being suspended.  Application state is saved
    /// without knowing whether the application will be terminated or resumed with the contents
    /// of memory still intact.
    /// </summary>
    /// <param name="sender">The source of the suspend request.</param>
    /// <param name="e">Details about the suspend request.</param>
    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
        var deferral = e.SuspendingOperation.GetDeferral();
        //TODO: Save application state and stop any background activity
        deferral.Complete();
    }
}
