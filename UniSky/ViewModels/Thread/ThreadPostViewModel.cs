using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Feed;
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

    public ThreadPostViewModel(ThreadViewPost threadPost, bool isSelected = false) : base(threadPost.Post, false)
    {
        this.IsSelected = isSelected;
        this.HasParent = threadPost.Parent != null;

        var date = threadPost.Post.IndexedAt.GetValueOrDefault();
        this.LongDate = dateTimeFormatter.Format(new DateTimeOffset(date));
    }
}
