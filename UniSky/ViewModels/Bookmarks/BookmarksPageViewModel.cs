using CommunityToolkit.Mvvm.ComponentModel;
using UniSky.Services;
using UniSky.Services.Navigation;

namespace UniSky.ViewModels.Bookmarks;

public partial class BookmarksPageViewModel : ViewModelBase
{
    private readonly IProtocolService protocolService;

    [ObservableProperty]
    private BookmarksCollection bookmarks;

    [ObservableProperty]
    private bool isEmpty;

    public BookmarksPageViewModel(INavigationContext navigation, IProtocolService protocolService)
        : base(navigation)
    {
        this.protocolService = protocolService;
        this.bookmarks = new BookmarksCollection(this);
        this.isEmpty = false;
    }
}
