using System;
using System.Collections;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UniSky.Services;
using Windows.ApplicationModel.Resources;

namespace UniSky.ViewModels.Search;

public partial class SearchFeedViewModel : ViewModelBase
{
    private readonly SearchPageViewModel parent;
    private readonly string type;
    private readonly IProtocolService protocolService;

    [ObservableProperty]
    private string name;
    [ObservableProperty]
    private ICollection items;
    [ObservableProperty]
    private bool selected;

    public SearchFeedViewModel(SearchPageViewModel parent, string type, IProtocolService protocolService)
    {
        this.parent = parent;
        this.parent.PropertyChanged += OnParentPropertyChanged;
        this.type = type;
        this.protocolService = protocolService;

        var resources = ResourceLoader.GetForCurrentView();
        this.Name = type switch
        {
            "latest" => resources.GetString("Search_New"),
            "top" => resources.GetString("Search_Hot"),
            "people" => resources.GetString("Search_People"),
            _ => throw new NotImplementedException(),
        };
    }

    private void OnParentPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(parent.SelectedFeed))
        {
            Selected = parent.SelectedFeed == this;
        }
    }

    partial void OnSelectedChanged(bool value)
    {
        if (value)
            parent.Select(this);
    }

    internal void DoQuery(string queryText)
    {
        if (type == "people")
        {
            Items = new SearchProfileCollection(this, queryText);
        }
        else
        {
            Items = new SearchPostsCollection(this, queryText, this.type);
        }
    }
}
