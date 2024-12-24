using System;

namespace UniSky.Moderation;

public enum ModerationContext
{
    /// <summary>
    /// A profile being listed, e.g. search or follower list
    /// </summary>
    ProfileList,

    /// <summary>
    /// A profile being viewed directly
    /// </summary>
    ProfileView,

    /// <summary>
    /// The user's avatar in any context
    /// </summary>
    Avatar,

    /// <summary>
    /// The user's banner in any context
    /// </summary>
    Banner,

    /// <summary>
    /// The user's display name in any context
    /// </summary>
    DisplayName,

    /// <summary>
    /// Content being listed, e.g. posts in a feed/as replies, a list of user lists, a list of feed generators, etc.
    /// </summary>
    ContentList,

    /// <summary>
    /// Conent being viewed directly, e.g. an open post, the user list page, the feed generator page, etc.
    /// </summary>
    ContentView,

    /// <summary>
    /// Media inside content, e.g. images embedded in a post
    /// </summary>
    ContentMedia
}

public struct ModerationBehavior
{
    public static readonly ModerationBehavior BlockBehaviour = new()
    {
        ProfileList = ModerationBehaviorType.Blur,
        ProfileView = ModerationBehaviorType.Alert,
        Avatar = ModerationBehaviorType.Blur,
        Banner = ModerationBehaviorType.Blur,
        ContentList = ModerationBehaviorType.Blur,
        ContentView = ModerationBehaviorType.Blur
    };

    public static readonly ModerationBehavior MuteBehaviour = new()
    {
        ProfileList = ModerationBehaviorType.Inform,
        ProfileView = ModerationBehaviorType.Alert,
        ContentList = ModerationBehaviorType.Blur,
        ContentView = ModerationBehaviorType.Inform
    };

    public static readonly ModerationBehavior MuteWordBehavour = new()
    {
        ContentList = ModerationBehaviorType.Blur,
        ContentView = ModerationBehaviorType.Blur
    };

    public static readonly ModerationBehavior HideBehaviour
        = MuteWordBehavour;

    public readonly ModerationBehaviorType this[ModerationContext target]
        => target switch
        {
            ModerationContext.ProfileList => ProfileList,
            ModerationContext.ProfileView => ProfileView,
            ModerationContext.Avatar => Avatar,
            ModerationContext.Banner => Banner,
            ModerationContext.DisplayName => DisplayName,
            ModerationContext.ContentList => ContentList,
            ModerationContext.ContentView => ContentView,
            ModerationContext.ContentMedia => ContentMedia,
            _ => throw new InvalidOperationException()
        };

    public ModerationBehaviorType ProfileList;
    public ModerationBehaviorType ProfileView;
    public ModerationBehaviorType Avatar;
    public ModerationBehaviorType Banner;
    public ModerationBehaviorType DisplayName;
    public ModerationBehaviorType ContentList;
    public ModerationBehaviorType ContentView;
    public ModerationBehaviorType ContentMedia;
}
