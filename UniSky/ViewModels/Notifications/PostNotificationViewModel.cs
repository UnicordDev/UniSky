using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Feed;
using UniSky.Models.Notifications;
using UniSky.Services.Navigation;
using UniSky.ViewModels.Posts;
using UniSky.ViewModels.Profile;
using Windows.ApplicationModel.Resources;

namespace UniSky.ViewModels.Notifications;
public sealed partial class PostNotificationViewModel : PostViewModel, INotificationItem
{
    private static readonly ResourceLoader strings = ResourceLoader.GetForViewIndependentUse();

    public PostNotificationViewModel(INavigationContext navigation,
                                     NotificationGroup group,
                                     PostView subject,
                                     DateTime? seenAt,
                                     bool allowUnreadHighlight)
        : base(navigation, subject)
    {
        Group = group;
        Kind = group.Kind;
        Key = group.Key;
        IndexedAt = group.IndexedAt;

        this.isRead = NotificationItem.ComputeIsRead(group, seenAt);
        this.allowUnreadHighlight = allowUnreadHighlight;

        var author = new ProfileViewModel(navigation, group.Head.Author);
        NotificationTitle = string.Format(strings.GetString(TitleKey(group.Kind)), author.Name);
        NotificationGlyph = Glyph(group.Kind);
    }

    public NotificationGroup Group { get; }
    public NotificationKind Kind { get; }
    public string Key { get; }
    public DateTime IndexedAt { get; }

    public string NotificationTitle { get; }

    public string NotificationGlyph { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnreadHighlight))]
    private bool isRead;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnreadHighlight))]
    private bool allowUnreadHighlight;

    public bool ShowUnreadHighlight
        => AllowUnreadHighlight && !IsRead;

    private static string TitleKey(NotificationKind kind) => kind switch
    {
        NotificationKind.Reply => "Notification_Reply",
        NotificationKind.Mention => "Notification_Mention",
        NotificationKind.Quote => "Notification_Quote",
        _ => "Notification_Unknown",
    };

    private static string Glyph(NotificationKind kind) => kind switch
    {
        NotificationKind.Reply => "\uE97A",     // reply arrow, as the feed's reply strip uses
        NotificationKind.Mention => "\uE77B",   // contact
        NotificationKind.Quote => "\uE8EB",     // repost
        _ => "\uE946",                          // info
    };
}
