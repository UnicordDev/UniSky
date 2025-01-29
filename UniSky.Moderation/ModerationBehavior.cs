using System;
using System.Text.Json.Serialization;

namespace UniSky.Moderation;

public enum ModerationContext
{
    /// <summary>
    /// A profile being listed, e.g. search or follower list
    /// Can Blur, Alert & Inform
    /// </summary>
    ProfileList,

    /// <summary>
    /// A profile being viewed directly
    /// Can Blur, Alert & Inform
    /// </summary>
    ProfileView,

    /// <summary>
    /// The user's avatar in any context
    /// Can Blur & Alert 
    /// </summary>
    Avatar,

    /// <summary>
    /// The user's banner in any context
    /// Can Blur
    /// </summary>
    Banner,

    /// <summary>
    /// The user's display name in any context
    /// Can Blur
    /// </summary>
    DisplayName,

    /// <summary>
    /// Content being listed, e.g. posts in a feed/as replies, a list of user lists, a list of feed generators, etc.
    /// Can Blur, Alert & Inform
    /// </summary>
    ContentList,

    /// <summary>
    /// Conent being viewed directly, e.g. an open post, the user list page, the feed generator page, etc.
    /// Can Blur, Alert & Inform
    /// </summary>
    ContentView,

    /// <summary>
    /// Media inside content, e.g. images embedded in a post
    /// Can Blur
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

    [JsonInclude]
    public ModerationBehaviorType ProfileList;
    [JsonInclude]
    public ModerationBehaviorType ProfileView;
    [JsonInclude]
    public ModerationBehaviorType Avatar;
    [JsonInclude]
    public ModerationBehaviorType Banner;
    [JsonInclude]
    public ModerationBehaviorType DisplayName;
    [JsonInclude]
    public ModerationBehaviorType ContentList;
    [JsonInclude]
    public ModerationBehaviorType ContentView;
    [JsonInclude]
    public ModerationBehaviorType ContentMedia;
}
