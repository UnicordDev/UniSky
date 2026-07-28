using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Feed;
using UniSky.Services.Navigation;
using UniSky.ViewModels.Posts;
using Windows.Globalization.DateTimeFormatting;

namespace UniSky.ViewModels.Thread;

public partial class ThreadPostViewModel : PostViewModel
{
    private static readonly DateTimeFormatter dateTimeFormatter
        = new DateTimeFormatter("shorttime longdate");

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string longDate;

    public ThreadPostViewModel(INavigationContext navigation, ThreadViewPost threadPost, bool isSelected = false)
        : this(navigation, threadPost.Post, isSelected)
    {
        this.HasParent = threadPost.Parent != null;
    }
    
    public ThreadPostViewModel(INavigationContext navigation, PostView post, bool isSelected = false)
        : base(navigation, post, false)
    {
        this.IsSelected = isSelected;

        var date = post.IndexedAt.GetValueOrDefault();
        this.LongDate = dateTimeFormatter.Format(new DateTimeOffset(date));
    }
}
