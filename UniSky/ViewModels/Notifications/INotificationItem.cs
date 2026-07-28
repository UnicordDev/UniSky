using System;
using FishyFlip.Lexicon.App.Bsky.Feed;
using UniSky.Models.Notifications;
using UniSky.Services;
using UniSky.Services.Navigation;

namespace UniSky.ViewModels.Notifications;

public interface INotificationItem
{
    NotificationKind Kind { get; }    
    string Key { get; }
    DateTime IndexedAt { get; }
    bool IsRead { get; }
    bool ShowUnreadHighlight { get; }
}

public static class NotificationItem
{
    public static INotificationItem Create(INavigationContext navigation,
                                           NotificationGroup group,
                                           NotificationFeedPage page,
                                           DateTime? seenAt,
                                           bool allowUnreadHighlight)
    {
        var subject = GetSubjectPost(group, page);

        if (NotificationTypes.IsFullPost(group.Kind))
        {
            if (subject is null)
                return null;

            return new PostNotificationViewModel(navigation, group, subject, seenAt, allowUnreadHighlight);
        }

        return new AggregateNotificationViewModel(navigation, group, page, subject, seenAt, allowUnreadHighlight);
    }
    
    internal static bool ComputeIsRead(NotificationGroup group, DateTime? seenAt)
        => seenAt is null || group.IndexedAt <= seenAt.Value;

    private static PostView GetSubjectPost(NotificationGroup group, NotificationFeedPage page)
    {
        if (group.SubjectUri is null || page?.Posts is null)
            return null;

        return page.Posts.TryGetValue(group.SubjectUri.ToString(), out var post) ? post : null;
    }
}
