using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Graph;
using FishyFlip.Lexicon.App.Bsky.Notification;
using FishyFlip.Models;
using Microsoft.Extensions.Logging;
using UniSky.Models.Notifications;
using UniSky.Moderation;

namespace UniSky.Services;

public class NotificationFeedService(
    IProtocolService protocolService,
    IModerationService moderationService,
    ILogger<NotificationFeedService> logger) : INotificationFeedService
{
    private const int HydrationChunkSize = 25;

    private DateTime? seenAt;

    public DateTime? SeenAt => seenAt;

    public event EventHandler SeenAtChanged;

    public async Task<NotificationFeedPage> FetchPageAsync(NotificationFeedFilter filter,
                                                           string cursor,
                                                           int limit,
                                                           DateTime? seenAt = null,
                                                           CancellationToken cancellationToken = default)
    {
        var protocol = protocolService.Protocol;
        var moderator = new Moderator(moderationService.ModerationOptions);

        var reasons = filter == NotificationFeedFilter.Mentions
            ? NotificationReasons.MentionsOnly.ToList()
            : null;

        // even though we have plumbing for seenAt, it doesn't currently work becausae fishyflip marshals it incorrectly,
        // so we dont actually pass it down along
        var response = (await protocol.Notification
            .ListNotificationsAsync(reasons: reasons, limit: limit, cursor: cursor, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
            .HandleResult();

        var usable = Sanitise(response.Notifications, moderator);
        var groups = NotificationGrouper.Group(usable);

        var posts = await HydratePostsAsync(protocol, groups, cancellationToken).ConfigureAwait(false);
        var starterPacks = await HydrateStarterPacksAsync(protocol, groups, cancellationToken).ConfigureAwait(false);

        UpdateSeenAt(response.SeenAt);

        return new NotificationFeedPage()
        {
            Cursor = response.Cursor,
            SeenAt = response.SeenAt,
            Items = groups,
            Posts = posts,
            StarterPacks = starterPacks,
        };
    }
    
    private List<Notification> Sanitise(IReadOnlyList<Notification> notifications, Moderator moderator)
    {
        if (notifications == null)
            return [];

        var usable = new List<Notification>(notifications.Count);
        foreach (var notification in notifications)
        {
            try
            {
                if (notification?.Cid is null || notification.IndexedAt is null || notification.Author?.Did is null)
                {
                    logger.LogDebug("Skipping malformed notification {Uri}", notification?.Uri);
                    continue;
                }

                if (NotificationFilter.ShouldFilter(notification, moderator))
                    continue;

                usable.Add(notification);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Skipping notification {Uri}, moderation threw", notification?.Uri);
            }
        }

        return usable;
    }

    private async Task<IReadOnlyDictionary<string, PostView>> HydratePostsAsync(
        ATProtocol protocol, IReadOnlyList<NotificationGroup> groups, CancellationToken cancellationToken)
    {
        var uris = new Dictionary<string, ATUri>();
        foreach (var group in groups)
        {
            if (NotificationTypes.IsPostSubject(group.SubjectUri))
                uris[group.SubjectUri.ToString()] = group.SubjectUri;
        }

        var posts = new Dictionary<string, PostView>();
        if (uris.Count == 0)
            return posts;

        var chunks = Chunk(uris.Values.ToList(), HydrationChunkSize)
            .Select(async chunk =>
            {
                try
                {
                    return (await protocol.Feed.GetPostsAsync(chunk, cancellationToken).ConfigureAwait(false))
                        .HandleResult()?.Posts;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to hydrate {Count} notification subject posts", chunk.Count);
                    return null;
                }
            });

        foreach (var chunk in await Task.WhenAll(chunks).ConfigureAwait(false))
        {
            foreach (var post in chunk ?? [])
            {
                if (post?.Uri is not null && post.Record is Post)
                    posts[post.Uri.ToString()] = post;
            }
        }

        return posts;
    }

    private async Task<IReadOnlyDictionary<string, StarterPackViewBasic>> HydrateStarterPacksAsync(
        ATProtocol protocol, IReadOnlyList<NotificationGroup> groups, CancellationToken cancellationToken)
    {
        var uris = new Dictionary<string, ATUri>();
        foreach (var group in groups)
        {
            if (group.Kind == NotificationKind.StarterPackJoined && group.SubjectUri is not null)
                uris[group.SubjectUri.ToString()] = group.SubjectUri;
        }

        var packs = new Dictionary<string, StarterPackViewBasic>();
        if (uris.Count == 0)
            return packs;

        var chunks = Chunk(uris.Values.ToList(), HydrationChunkSize)
            .Select(async chunk =>
            {
                try
                {
                    return (await protocol.Graph.GetStarterPacksAsync(chunk, cancellationToken).ConfigureAwait(false))
                        .HandleResult()?.StarterPacks;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to hydrate {Count} starter packs", chunk.Count);
                    return null;
                }
            });

        foreach (var chunk in await Task.WhenAll(chunks).ConfigureAwait(false))
        {
            foreach (var pack in chunk ?? [])
            {
                if (pack?.Uri is not null)
                    packs[pack.Uri.ToString()] = pack;
            }
        }

        return packs;
    }

    public async Task<int> GetUnreadCountAsync(DateTime? seenAt, CancellationToken cancellationToken = default)
    {
        var protocol = protocolService.Protocol;
        
        // ditto about seenAt
        var response = (await protocol.Notification
            .GetUnreadCountAsync(priority: null, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
            .HandleResult();

        return (int)Math.Min(response.Count, int.MaxValue);
    }

    public async Task MarkAllReadAsync(DateTime seenAtUtc, CancellationToken cancellationToken = default)
    {
        var protocol = protocolService.Protocol;
        _ = (await protocol.Notification
            .UpdateSeenAsync(seenAtUtc, cancellationToken)
            .ConfigureAwait(false))
            .HandleResult();

        UpdateSeenAt(seenAtUtc);
    }

    private void UpdateSeenAt(DateTime? value)
    {
        if (value is null || (seenAt is not null && value <= seenAt))
            return;

        seenAt = value;
        SeenAtChanged?.Invoke(this, EventArgs.Empty);
    }
    
    private static IEnumerable<List<T>> Chunk<T>(IReadOnlyList<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
        {
            var chunk = new List<T>(Math.Min(size, source.Count - i));
            for (var j = i; j < Math.Min(i + size, source.Count); j++)
                chunk.Add(source[j]);

            yield return chunk;
        }
    }
}
