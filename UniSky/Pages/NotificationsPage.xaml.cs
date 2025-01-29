using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Services;
using UniSky.ViewModels.Notifications;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Pages;

public sealed partial class NotificationsPage : Page, IScrollToTop
{
    public NotificationsPageViewModel ViewModel
    {
        get => (NotificationsPageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(NotificationsPageViewModel), typeof(NotificationsPage), new PropertyMetadata(null));

    public NotificationsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        if (this.ViewModel == null)
            this.DataContext = this.ViewModel = ActivatorUtilities.CreateInstance<NotificationsPageViewModel>(ServiceContainer.Scoped);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        var themeService = ServiceContainer.Scoped.GetRequiredService<IThemeService>();
        if (themeService.GetTheme() == AppTheme.SunValley)
            TitleBarPadding.Height = new GridLength(0);
        else
            TitleBarPadding.Height = new GridLength(e.SafeArea.Bounds.Top);
    }

    private void RootList_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private void RootList_Loaded(object sender, RoutedEventArgs e)
    {
        var themeService = ServiceContainer.Scoped.GetRequiredService<IThemeService>();
        if (themeService.GetTheme() == AppTheme.SunValley)
            return;

        if (ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 7))
        {
            var scrollViewer = RootList.FindDescendant<ScrollViewer>();
            scrollViewer.CanContentRenderOutsideBounds = true;
        }
    }

    public void ScrollToTop()
    {
        var scrollViewer = RootList.FindDescendant<ScrollViewer>();
        if (scrollViewer == null) 
            return;

        scrollViewer.ChangeView(0, 0, 1);
    }
}
