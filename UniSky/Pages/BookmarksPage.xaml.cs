using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Services;
using UniSky.ViewModels.Bookmarks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Pages;

public sealed partial class BookmarksPage : Page, IScrollToTop
{
    public BookmarksPageViewModel ViewModel
    {
        get => (BookmarksPageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(BookmarksPageViewModel), typeof(BookmarksPage), new PropertyMetadata(null));

    public BookmarksPage()
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
            this.DataContext = this.ViewModel = ActivatorUtilities.CreateInstance<BookmarksPageViewModel>(ServiceContainer.Scoped);
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

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await this.ViewModel.Bookmarks.RefreshAsync();
        }
        catch { }
    }
}
