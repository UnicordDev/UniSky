using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using UniSky.Services;

namespace UniSky.ViewModels.Search;

public partial class SearchPageViewModel : ViewModelBase
{
    private readonly IProtocolService protocolService;

    [ObservableProperty]
    private string searchQuery;
    [ObservableProperty]
    private SearchFeedViewModel selectedFeed;

    public ObservableCollection<SearchFeedViewModel> SearchFeeds { get; }

    public SearchPageViewModel(IProtocolService protocolService)
    {
        this.protocolService = protocolService;
        this.SearchFeeds = [
            new SearchFeedViewModel(this, "latest", protocolService),
            new SearchFeedViewModel(this, "top", protocolService),
            new SearchFeedViewModel(this, "people", protocolService),
        ];

        SelectedFeed = SearchFeeds[0];
    }

    public async Task UpdateSuggestionsAsync()
    {

    }

    public void DoQuery(string queryText)
    {
        foreach (var feed in SearchFeeds)
        {
            feed.DoQuery(queryText);
        }
    }

    internal void Select(SearchFeedViewModel searchFeedViewModel)
    {
        SelectedFeed = searchFeedViewModel;
    }
}
