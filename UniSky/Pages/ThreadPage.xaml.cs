using Microsoft.Extensions.DependencyInjection;
using UniSky.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using FishyFlip.Models;
using UniSky.ViewModels.Thread;
using Microsoft.Toolkit.Uwp.UI.Extensions;

namespace UniSky.Pages;

public sealed partial class ThreadPage : Page
{
    public ThreadViewModel ViewModel
    {
        get => (ThreadViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(ThreadViewModel), typeof(ThreadPage), new PropertyMetadata(null));

    public ThreadPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        if (e.Parameter is not (ATUri))
            return;

        if (e.Parameter is ATUri uri)
            this.DataContext = ViewModel = ActivatorUtilities.CreateInstance<ThreadViewModel>(ServiceContainer.Default, uri);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        HeaderGrid.Padding = e.SafeArea.Bounds with { Bottom = 0, Left = 0, Right = 0 };
        HandleScrolling();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        HandleScrolling();
    }

    private void HandleScrolling()
    {
        var stackPanel = RootList.FindDescendant<ItemsStackPanel>();
        if (stackPanel == null)
            return;

        stackPanel.Margin = new Thickness(0, HeaderContainer.ActualHeight, 0, ActualHeight - HeaderContainer.ActualHeight);
    }
}
