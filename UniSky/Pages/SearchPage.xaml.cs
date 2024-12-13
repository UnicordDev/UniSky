using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Services;
using UniSky.ViewModels.Profile;
using UniSky.ViewModels.Search;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Pages;

public sealed partial class SearchPage : Page
{
    public SearchPageViewModel ViewModel
    {
        get => (SearchPageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(SearchPageViewModel), typeof(SearchPage), new PropertyMetadata(null));

    public SearchPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        if (this.ViewModel == null)
            this.DataContext = this.ViewModel = ActivatorUtilities.CreateInstance<SearchPageViewModel>(ServiceContainer.Scoped);
        this.SearchBox.Focus(FocusState.Programmatic);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        TitleBarPadding.Height = new GridLength(e.SafeArea.Bounds.Top);
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            await ViewModel.UpdateSuggestionsAsync();
    }

    private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {

    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.DoQuery(args.QueryText);
    }

    private void RootList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ProfileViewModel profile)
        {
            var container = ((ListView)sender).ContainerFromItem(e.ClickedItem);
            if (container != null)
            {
                var child = container.FindDescendantByName("ProfileImage");
                profile.OpenProfileCommand.Execute(child);
            }
            else
            {
                profile.OpenProfileCommand.Execute(null);
            }
        }
    }

    private void RootList_Loaded(object sender, RoutedEventArgs e)
    {
        if (ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 7))
        {
            var scrollViewer = RootList.FindDescendant<ScrollViewer>();
            scrollViewer.CanContentRenderOutsideBounds = true;
        }
    }
}
