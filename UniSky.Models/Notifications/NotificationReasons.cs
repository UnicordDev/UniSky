namespace UniSky.Models.Notifications;

public static class NotificationReasons
{
    public const string Like = "like";
    public const string Repost = "repost";
    public const string Follow = "follow";
    public const string Mention = "mention";
    public const string Reply = "reply";
    public const string Quote = "quote";
    public const string StarterPackJoined = "starterpack-joined";
    public const string Verified = "verified";
    public const string Unverified = "unverified";
    public const string LikeViaRepost = "like-via-repost";
    public const string RepostViaRepost = "repost-via-repost";
    public const string SubscribedPost = "subscribed-post";
    
    public static readonly string[] MentionsOnly = [Mention, Reply, Quote];
}
