using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using UniSky.Services;

namespace UniSky.ViewModels.Bookmarks;

public partial class BookmarksPageViewModel : ViewModelBase
{
    private readonly IProtocolService protocolService;

    [ObservableProperty]
    private BookmarksCollection bookmarks;

    [ObservableProperty]
    private bool isEmpty;

    public BookmarksPageViewModel(IProtocolService protocolService)
    {
        this.protocolService = protocolService;
        this.bookmarks = new BookmarksCollection(this);
        this.isEmpty = false;
    }
}
