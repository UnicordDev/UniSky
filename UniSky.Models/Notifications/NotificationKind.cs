namespace UniSky.Models.Notifications;

public enum NotificationKind
{
    Unknown = 0,
    Like,
    FeedGenLike,
    Repost,
    Follow,
    Mention,
    Reply,
    Quote,
    Verified,
    Unverified,
    LikeViaRepost,
    RepostViaRepost,
    SubscribedPost,
    StarterPackJoined,
}
