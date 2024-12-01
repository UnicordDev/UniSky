using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Sheet;
using UniSky.Services;
using UniSky.ViewModels;
using UniSky.ViewModels.Feeds;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using MUXC = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UniSky.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FeedsPage : Page
{
    public FeedsViewModel ViewModel
    {
        get => (FeedsViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(FeedsViewModel), typeof(FeedsPage), new PropertyMetadata(null));

    public FeedsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        this.ViewModel = ActivatorUtilities.CreateInstance<FeedsViewModel>(Ioc.Default);

        var safeAreaService = Ioc.Default.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        RootGrid.Padding = new Thickness(0, e.SafeArea.Bounds.Top, 0, 0);
    }

    private async void OnRefreshRequested(MUXC.RefreshContainer sender, MUXC.RefreshRequestedEventArgs args)
    {
        var feed = (FeedViewModel)sender.DataContext;

        var deferral = args.GetDeferral();
        await feed.RefreshAsync(deferral);
    }

    private void PivotHeaderText_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var feedsList = FeedsPivot.ContainerFromItem(FeedsPivot.SelectedItem)
            .FindDescendantByName("PART_FeedList");

        if (feedsList is not ListView lv)
            return;

        var scrollView = lv.FindDescendant<ScrollViewer>();
        if (scrollView is null)
            return;

        scrollView.ChangeView(0, 0, null);
    }

    private async void RefreshAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (FeedsPivot.SelectedItem is not FeedViewModel feedVm)
            return;

        await feedVm.RefreshAsync();
    }
}
