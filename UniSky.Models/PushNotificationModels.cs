using System;
using System.Text.Json.Serialization;

namespace UniSky.Models;

[Flags]
public enum NotificationOptions
{
    ExcludeLikes = 1,
    ExcludeNewFollowers = 2,
    ExcludeReplies = 4,
    ExcludeMentions = 8,
    ExcludeQuotes = 16,
    ExcludeReposts = 32,
    ExcludeRepostLikes = 64,
    ExcludeRepostReposts = 128,

    ExcludeEverything = ExcludeLikes | ExcludeNewFollowers | ExcludeReplies | ExcludeMentions | ExcludeQuotes | ExcludeReposts,

    FollowingOnly = 65536
}

public record class RegistrationModel(string Did, string InstallId, string ChannelUrl, NotificationOptions Options, string[]? Locales = null)
{
}
