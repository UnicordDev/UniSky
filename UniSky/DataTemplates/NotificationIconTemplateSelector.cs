using UniSky.Models.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.DataTemplates;

public class NotificationIconTemplateSelector : DataTemplateSelector
{
    public DataTemplate LikeTemplate { get; set; }
    public DataTemplate RepostTemplate { get; set; }
    public DataTemplate FollowTemplate { get; set; }
    public DataTemplate SubscribedPostTemplate { get; set; }
    public DataTemplate StarterPackTemplate { get; set; }
    public DataTemplate VerifiedTemplate { get; set; }
    public DataTemplate UnverifiedTemplate { get; set; }
    public DataTemplate DefaultTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not NotificationKind kind)
            return DefaultTemplate;

        return kind switch
        {
            NotificationKind.Like or
            NotificationKind.LikeViaRepost or
            NotificationKind.FeedGenLike => LikeTemplate,

            NotificationKind.Repost or
            NotificationKind.RepostViaRepost => RepostTemplate,

            NotificationKind.Follow => FollowTemplate,
            NotificationKind.SubscribedPost => SubscribedPostTemplate,
            NotificationKind.StarterPackJoined => StarterPackTemplate,
            NotificationKind.Verified => VerifiedTemplate,
            NotificationKind.Unverified => UnverifiedTemplate,

            _ => DefaultTemplate,
        };
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateCore(item, null);
    }
}
