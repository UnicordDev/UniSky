using System;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Notification;
using FishyFlip.Models;

namespace UniSky.Models.Notifications;

/// <summary>
/// Maps the wire form of a notification onto <see cref="NotificationKind"/> and works out what
/// it points at. Mirrors social-app's <c>toKnownType</c> / <c>getSubjectUri</c>.
/// </summary>
public static class NotificationTypes
{
    public const string PostCollection = "app.bsky.feed.post";
    private const string FeedGeneratorCollection = "app.bsky.feed.generator";
    
    public static NotificationKind ToKnownKind(string? reason, ATUri? reasonSubject) => reason switch
    {
        // a like on a feed generator and a like on a post arrive with the same reason
        NotificationReasons.Like => Contains(reasonSubject, FeedGeneratorCollection)
            ? NotificationKind.FeedGenLike
            : NotificationKind.Like,
        NotificationReasons.Repost => NotificationKind.Repost,
        NotificationReasons.Follow => NotificationKind.Follow,
        NotificationReasons.Mention => NotificationKind.Mention,
        NotificationReasons.Reply => NotificationKind.Reply,
        NotificationReasons.Quote => NotificationKind.Quote,
        NotificationReasons.Verified => NotificationKind.Verified,
        NotificationReasons.Unverified => NotificationKind.Unverified,
        NotificationReasons.LikeViaRepost => NotificationKind.LikeViaRepost,
        NotificationReasons.RepostViaRepost => NotificationKind.RepostViaRepost,
        NotificationReasons.SubscribedPost => NotificationKind.SubscribedPost,
        NotificationReasons.StarterPackJoined => NotificationKind.StarterPackJoined,
        _ => NotificationKind.Unknown
    };
    
    public static bool IsFullPost(NotificationKind kind)
        => kind is NotificationKind.Reply or NotificationKind.Mention or NotificationKind.Quote;
        
    public static ATUri? GetSubjectUri(NotificationKind kind, Notification notification) => kind switch
    {
        NotificationKind.Reply or
        NotificationKind.Quote or
        NotificationKind.Mention or
        NotificationKind.SubscribedPost
            => notification.Uri,

        NotificationKind.Like or
        NotificationKind.Repost or
        NotificationKind.LikeViaRepost or
        NotificationKind.RepostViaRepost
            => (notification.Record as Like)?.Subject?.Uri
            ?? (notification.Record as Repost)?.Subject?.Uri
            ?? notification.ReasonSubject,

        NotificationKind.FeedGenLike => notification.ReasonSubject,
        NotificationKind.StarterPackJoined => notification.ReasonSubject,
        _ => null
    };
    
    public static bool IsPostSubject(ATUri? uri)
        => Contains(uri, PostCollection);

    private static bool Contains(ATUri? uri, string collection)
        => uri?.ToString()?.IndexOf(collection, StringComparison.Ordinal) >= 0;
}
