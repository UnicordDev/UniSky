using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Graph;
using UniSky.Models.Notifications;

namespace UniSky.Services;

public enum NotificationFeedFilter
{
    All,
    Mentions
}

public sealed class NotificationFeedPage
{
    public string Cursor { get; set; }
    public DateTime? SeenAt { get; set; }
    public IReadOnlyList<NotificationGroup> Items { get; set; }
    public IReadOnlyDictionary<string, PostView> Posts { get; set; }
    public IReadOnlyDictionary<string, StarterPackViewBasic> StarterPacks { get; set; }
}

public interface INotificationFeedService
{
    Task<NotificationFeedPage> FetchPageAsync(NotificationFeedFilter filter,
                                              string cursor,
                                              int limit,
                                              DateTime? seenAt = null,
                                              CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(DateTime? seenAt, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(DateTime seenAtUtc, CancellationToken cancellationToken = default);
    DateTime? SeenAt { get; }
    event EventHandler SeenAtChanged;
}
