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

        AppTitleSuffixTextBlock.Text = Constants.Version;
        AppTitleSuffixTextBlock.Visibility = Visibility.Visible;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        Window.Current.SetTitleBar(TitleBarDrag);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        if (e.Parameter is not HomeViewModel)
            return;

        DataContext = ViewModel = (HomeViewModel)e.Parameter;
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
