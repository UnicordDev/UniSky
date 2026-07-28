using Microsoft.Extensions.DependencyInjection;
using UniSky.Navigation;
using UniSky.Services;
using UniSky.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using MUXC = Microsoft.UI.Xaml.Controls;

namespace UniSky.Pages;

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

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        var region = NavigationScopeHost.EnsureScope(FrameContent);

        if (e.Parameter is not HomeViewModel)
            return;

        DataContext = ViewModel = (HomeViewModel)e.Parameter;
        ViewModel.AttachToRegion(region);
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        if (e.SafeArea.HasTitleBar)
        {
            PaneHeader.Margin = new Thickness();

            var themeService = ServiceContainer.Scoped.GetRequiredService<IThemeService>();
            if (themeService.GetTheme() == AppTheme.SunValley)
            {
                FrameContainerContainer.Margin = new Thickness(0, e.SafeArea.Bounds.Top + 1, 0, 0);
            }
        }
        else
        {
            PaneHeader.Margin = new Thickness(0, e.SafeArea.Bounds.Top, 0, 0);
        }

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
