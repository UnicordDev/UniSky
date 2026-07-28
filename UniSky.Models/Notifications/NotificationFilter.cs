using System;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Notification;
using UniSky.Moderation;

namespace UniSky.Models.Notifications;

public static class NotificationFilter
{
    private static readonly string[] HideableOffenses = ["!hide", "!takedown"];

    public static bool ShouldFilter(Notification notification, Moderator moderator)
    {
        if (notification?.Author?.Did is null)
            return true;
            
        var labels = notification.Author.Labels;
        if (labels is not null)
        {
            for (var i = 0; i < labels.Count; i++)
            {
                var value = labels[i].Val;
                if (value is not null && Array.IndexOf(HideableOffenses, value) >= 0)
                    return true;
            }
        }

        var isFollowed = notification.Author.Viewer?.Following is not null;
        if (notification.Reason == NotificationReasons.SubscribedPost &&
            notification.Record is Post post &&
            moderator.HasMutedWord(post.Text ?? "",
                                   post.Facets,
                                   post.Tags,
                                   post.Langs,
                                   actorIsFollowed: isFollowed))
            return true;

        if (isFollowed)
            return false;

        return moderator.ModerateNotification(notification)
                        .GetUI(ModerationContext.ContentList)
                        .Filter;
    }
}
