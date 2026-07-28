using System;
using System.Collections.Generic;
using FishyFlip.Lexicon.App.Bsky.Notification;
using FishyFlip.Models;

namespace UniSky.Models.Notifications;

public static class NotificationGrouper
{
    public static readonly TimeSpan GroupWindow = TimeSpan.FromHours(48);
    
    private static bool IsGroupable(NotificationKind kind) => kind is
        NotificationKind.Like or
        NotificationKind.Repost or
        NotificationKind.Follow or
        NotificationKind.LikeViaRepost or
        NotificationKind.RepostViaRepost or
        NotificationKind.SubscribedPost;
        
    public static List<NotificationGroup> Group(IReadOnlyList<Notification> notifications)
    {
        var groups = new List<NotificationGroup>(notifications.Count);

        for (var i = 0; i < notifications.Count; i++)
        {
            var notification = notifications[i];
            var kind = NotificationTypes.ToKnownKind(notification.Reason, notification.ReasonSubject);

            if (kind == NotificationKind.Unknown)
                continue;

            var merged = false;

            if (IsGroupable(kind))
            {
                for (var g = 0; g < groups.Count; g++)
                {
                    var group = groups[g];

                    if (group.Kind != kind)
                        continue;
                    if (!SameUri(group.Head.ReasonSubject, notification.ReasonSubject))
                        continue;
                    if (!WithinWindow(group.Head, notification))
                        continue;
                    if (!CanCoexist(group.Head, notification, kind))
                        continue;
                    if (IsFollowBack(group.Head) || IsFollowBack(notification))
                        continue;

                    group.Add(notification);
                    merged = true;
                    break;
                }
            }

            if (!merged)
                groups.Add(new NotificationGroup(kind, notification));
        }

        return groups;
    }

    private static bool WithinWindow(Notification head, Notification candidate)
    {
        var a = head.IndexedAt;
        var b = candidate.IndexedAt;
        if (a is null || b is null)
            return false; 
            
        var delta = a.Value - b.Value;
        if (delta < TimeSpan.Zero)
            delta = delta.Negate();

        return delta < GroupWindow;
    }
    
    private static bool CanCoexist(Notification head, Notification candidate, NotificationKind kind)
        => kind == NotificationKind.SubscribedPost
        || !string.Equals(head.Author?.Did?.ToString(),
                          candidate.Author?.Did?.ToString(),
                          StringComparison.Ordinal);

    private static bool IsFollowBack(Notification notification)
        => string.Equals(notification.Reason, NotificationReasons.Follow, StringComparison.Ordinal)
        && notification.Author?.Viewer?.Following is not null;

    private static bool SameUri(ATUri? a, ATUri? b)
        => string.Equals(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
}
