using System;
using AngleSharp.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Services;
using UniSky.ViewModels;
using UniSky.ViewModels.Feeds;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using MUXC = Microsoft.UI.Xaml.Controls;

namespace UniSky.Pages;

public sealed partial class FeedsPage : Page, IScrollToTop
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

        this.ViewModel = ActivatorUtilities.CreateInstance<FeedsViewModel>(ServiceContainer.Scoped);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
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
        ScrollToTop();
    }

    private async void RefreshAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (FeedsPivot.SelectedItem is not FeedViewModel feedVm)
            return;

        await feedVm.RefreshAsync();
    }

    public void ScrollToTop()
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

    private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (!e.IsIntermediate)
        {
            var scroller = (ScrollViewer)sender;
            var dataContext = (FeedViewModel)scroller.DataContext;
            var distanceToEnd = scroller.ExtentHeight - (scroller.VerticalOffset + scroller.ViewportHeight);

            // trigger if within 2 viewports of the end
            if (distanceToEnd <= 0.5 * scroller.ViewportHeight
                    && dataContext.Items.HasMoreItems)
            {
                await dataContext.Items.LoadMoreAsync();
            }
        }
    }

    private async void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        var scroller = (ScrollViewer)sender;
        var dataContext = (FeedViewModel)scroller.DataContext;
        if (dataContext.Items.Count == 0)
            await dataContext.Items.LoadMoreAsync();
    }
}
