using FishyFlip.Models;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Services;
using UniSky.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

using MUXC = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UniSky.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel
    {
        get => (HomeViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(HomeViewModel), typeof(HomePage), new PropertyMetadata(null));

    public HomePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        Window.Current.SetTitleBar(TitleBarDrag);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        var serviceLocator = ServiceContainer.Scoped.GetRequiredService<INavigationServiceLocator>();
        var service = serviceLocator.GetNavigationService("Home");
        service.Frame = NavigationFrame;

        if (e.Parameter is string session || e.Parameter is ATDid did && (session = did.Handler) != null)
        {
            ViewModel = ActivatorUtilities.CreateInstance<HomeViewModel>(ServiceContainer.Scoped, session);
        }
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        if (e.SafeArea.HasTitleBar)
        {
            AppTitleBar.Visibility = Visibility.Visible;
            AppTitleBar.Height = e.SafeArea.Bounds.Top;
            PaneHeader.Margin = new Thickness();
        }
        else
        {
            AppTitleBar.Visibility = Visibility.Collapsed;
            PaneHeader.Margin = new Thickness(0, e.SafeArea.Bounds.Top, 0, 0);
        }

        if (e.SafeArea.IsActive)
        {
            VisualStateManager.GoToState(AppTitleBarContainer, "Active", true);
        }
        else
        {
            VisualStateManager.GoToState(AppTitleBarContainer, "Inactive", true);
        }

        AppTitleBarContainer.RequestedTheme = e.SafeArea.Theme;
        Margin = new Thickness(e.SafeArea.Bounds.Left, 0, e.SafeArea.Bounds.Right, e.SafeArea.Bounds.Bottom);
    }

    private async void NavigationView_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is HomePages page)
            ViewModel.Page = page;

        if (args.IsSettingsInvoked)
        {
            ViewModel.Page = ViewModel.Page;
            await ViewModel.OpenSettingsCommand.ExecuteAsync(null);
        }
    }

    private void FooterToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        var button = (ToggleButton)sender;
        var page = (HomePages)button.Tag;

        ViewModel.Page = page;
    }

    private void FooterToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateChecked();
    }

    private void NavView_PaneOpening(MUXC.NavigationView sender, object args)
    {
        if (sender.PaneDisplayMode == MUXC.NavigationViewPaneDisplayMode.LeftCompact)
            PaneOpenStoryboard.Begin();
    }

    private void NavView_PaneClosing(MUXC.NavigationView sender, MUXC.NavigationViewPaneClosingEventArgs args)
    {
        if (sender.PaneDisplayMode == MUXC.NavigationViewPaneDisplayMode.LeftCompact)
            PaneCloseStoryboard.Begin();
    }
}
