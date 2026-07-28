using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using Humanizer;
using UniSky.Helpers;
using UniSky.Models.Notifications;
using UniSky.Navigation;
using UniSky.Services;
using UniSky.Services.Navigation;
using UniSky.ViewModels.Posts;
using UniSky.ViewModels.Profile;
using Windows.ApplicationModel.Resources;

namespace UniSky.ViewModels.Notifications;

public sealed partial class AggregateNotificationViewModel : ViewModelBase, INotificationItem
{
    private const int MaxAuthors = 5;

    private static readonly ResourceLoader Strings = ResourceLoader.GetForViewIndependentUse();

    public AggregateNotificationViewModel(INavigationContext navigation,
                                          NotificationGroup group,
                                          NotificationFeedPage page,
                                          PostView subject,
                                          DateTime? seenAt,
                                          bool allowUnreadHighlight)
        : base(navigation)
    {
        Group = group;
        Kind = group.Kind;
        Key = group.Key;
        IndexedAt = group.IndexedAt;

        this.isRead = NotificationItem.ComputeIsRead(group, seenAt);
        this.allowUnreadHighlight = allowUnreadHighlight;

        var authors = BuildAuthors(navigation, group);
        Authors = authors;
        TotalCount = group.Count;
        OverflowCount = Math.Max(0, DistinctAuthorCount(group) - authors.Count);

        Title = BuildTitle(authors.Count > 0 ? authors[0].Name : null);
        Timestamp = (DateTime.UtcNow - group.IndexedAt)
            .Humanize(1, minUnit: Humanizer.Localisation.TimeUnit.Second);
            
        var subjectPost = subject?.Record as Post ?? group.Head.Record as Post;
        SubjectText = subjectPost?.Text ?? StarterPackName(group, page);
        SubjectEmbed = BuildEmbed(subjectPost, subject?.Author?.Did ?? group.Head.Author?.Did);
    }

    public NotificationGroup Group { get; }
    public NotificationKind Kind { get; }
    public string Key { get; }
    public DateTime IndexedAt { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnreadHighlight))]
    private bool isRead;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnreadHighlight))]
    private bool allowUnreadHighlight;

    public bool ShowUnreadHighlight
        => AllowUnreadHighlight && !IsRead;

    public IReadOnlyList<ProfileViewModel> Authors { get; }

    public int TotalCount { get; }

    public int OverflowCount { get; }

    public bool HasOverflow
        => OverflowCount > 0;

    public string OverflowLabel
        => HasOverflow ? string.Format(Strings.GetString("Notification_AuthorOverflow"), OverflowCount) : null;

    public string Title { get; }

    public string SubjectText { get; }

    public string Timestamp { get; }

    public PostEmbedViewModel SubjectEmbed { get; }

    private static List<ProfileViewModel> BuildAuthors(INavigationContext navigation, NotificationGroup group)
    {
        var authors = new List<ProfileViewModel>(MaxAuthors);
        var seen = new HashSet<string>();

        foreach (var notification in group.All())
        {
            var did = notification.Author?.Did?.ToString();
            if (did is null || !seen.Add(did))
                continue;

            authors.Add(new ProfileViewModel(navigation, notification.Author));
            if (authors.Count == MaxAuthors)
                break;
        }

        return authors;
    }

    private static int DistinctAuthorCount(NotificationGroup group)
    {
        var seen = new HashSet<string>();
        foreach (var notification in group.All())
        {
            var did = notification.Author?.Did?.ToString();
            if (did is not null)
                seen.Add(did);
        }

        return seen.Count;
    }

    private string BuildTitle(string name) => Kind switch
    {
        NotificationKind.Like
            => Aggregated("Notification_LikedTweetSingle", "Notification_LikedTweetMultiple", name),
        NotificationKind.Repost
            => Aggregated("Notification_RetweetSingle", "Notification_RetweetMultiple", name),
        NotificationKind.LikeViaRepost
            => Aggregated("Notification_LikeViaRepostSingle", "Notification_LikeViaRepostMultiple", name),
        NotificationKind.RepostViaRepost
            => Aggregated("Notification_RepostViaRepostSingle", "Notification_RepostViaRepostMultiple", name),
        NotificationKind.FeedGenLike
            => Aggregated("Notification_FeedGenLikeSingle", "Notification_FeedGenLikeMultiple", name),
        NotificationKind.Follow
            => Aggregated("Notification_Follow", "Notification_FollowMultiple", name),
        NotificationKind.SubscribedPost
            => Aggregated("Notification_SubscribedPost", "Notification_SubscribedPostMultiple", name),
        NotificationKind.StarterPackJoined
            => Format("Notification_StarterPackJoined", name),
        NotificationKind.Verified
            => Format("Notification_Verified", name),
        NotificationKind.Unverified
            => Format("Notification_Unverified", name),
        _ => Format("Notification_Unknown", name),
    };

    private string Format(string key, string name)
        => string.Format(Strings.GetString(key), name);

    private string Aggregated(string singleKey, string multipleKey, string name)
    {
        var count = Kind == NotificationKind.SubscribedPost ? TotalCount : Authors.Count + OverflowCount;
        if (count <= 1)
            return Format(singleKey, name);

        var other = Strings.GetString("Notification_Other");
        return string.Format(Strings.GetString(multipleKey), name, other.ToQuantity(count - 1));
    }

    private static string StarterPackName(NotificationGroup group, NotificationFeedPage page)
    {
        if (group.Kind != NotificationKind.StarterPackJoined ||
            group.SubjectUri is null ||
            page?.StarterPacks is null)
            return null;

        if (!page.StarterPacks.TryGetValue(group.SubjectUri.ToString(), out var pack))
            return null;

        return (pack.Record as FishyFlip.Lexicon.App.Bsky.Graph.Starterpack)?.Name;
    }

    private static PostEmbedViewModel BuildEmbed(Post subjectPost, ATIdentifier author)
    {
        if (subjectPost is { Embed: EmbedImages and { } images })
            return new PostEmbedImagesViewModel(author, images);

        // FishyFlip has no lexicon type for app.bsky.embed.gallery, so it arrives unparsed
        if (subjectPost is { Embed: UnknownATObject { Type: GalleryEmbed.RecordType } gallery } &&
            GalleryEmbed.TryCreateViewImages(gallery, author, out var galleryImages))
            return new PostEmbedImagesViewModel(galleryImages, isCarousel: true);

        return null;
    }

    [RelayCommand]
    private void OpenSubject()
    {
        var request = Kind switch
        {
            NotificationKind.FeedGenLike => Routes.Feed(Group.SubjectUri),
            NotificationKind.StarterPackJoined or
            NotificationKind.Follow or
            NotificationKind.Verified or
            NotificationKind.Unverified => Routes.Profile(Group.Head.Author?.Did),
            _ => NotificationTypes.IsPostSubject(Group.SubjectUri) ? Routes.Thread(Group.SubjectUri) : null,
        };

        Navigate(request);
    }
}
