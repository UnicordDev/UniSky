using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using UniSky.Services;
using UniSky.ViewModels.Feeds;
using Windows.ApplicationModel.Resources;

namespace UniSky.ViewModels.Profile;

public partial class ProfileFeedViewModel : FeedViewModel
{
    private ProfilePageViewModel parent;

    [ObservableProperty]
    private bool selected;

    public ProfileFeedViewModel(ProfilePageViewModel parent, string filterType, ATObject profile, IProtocolService protocolService)
        : base(FeedType.Author, protocolService)
    {
        this.parent = parent;
        this.parent.PropertyChanged += OnParentPropertyChanged;

        var resources = ResourceLoader.GetForCurrentView();
        this.Name = filterType switch
        {
            "posts_no_replies" => resources.GetString("Feed_Posts"),
            "posts_with_replies" => resources.GetString("Feed_Replies"),
            "posts_with_media" => resources.GetString("Feed_Media"),
            "posts_and_author_threads" => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };

        var did = profile switch
        {
            ProfileViewBasic basic => basic.Did,
            ProfileViewDetailed detailed => detailed.Did,
            ProfileView view => view.Did,
            _ => throw new InvalidCastException()
        };
        this.Items = new FeedItemCollection(this, FeedType.Author, did, filterType);
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
}
