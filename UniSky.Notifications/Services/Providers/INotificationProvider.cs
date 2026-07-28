using CommunityToolkit.WinUI.Notifications;
using FishyFlip;
using UniSky.Notifications.Models;

namespace UniSky.Notifications.Services.Providers;

public interface INotificationProvider
{
    Task<bool> PopulateModernNotification(
        ATProtocol at,
        NotificationEvent notification,
        ToastContentBuilder builder);

    public virtual async Task<bool> PopulateClassicNotification(ATProtocol at, NotificationEvent notification, ClassicNotificationBuilder builder)
    {
        var contentBuilder = new ToastContentBuilder();
        if (!await PopulateModernNotification(at, notification, contentBuilder))
            return false;

        var title = contentBuilder.Content.Visual.BindingGeneric.Children
            .OfType<AdaptiveText>()
            .FirstOrDefault(t => t.HintStyle == AdaptiveTextStyle.Title);
        var body = contentBuilder.Content.Visual.BindingGeneric.Children
            .OfType<AdaptiveText>()
            .FirstOrDefault(t => t.HintStyle == AdaptiveTextStyle.Body);

        if (title != null)
            builder.SetTitle(title.Text);
        if (body != null)
            builder.SetContent(body.Text);

        return title != null && body != null;
    }
}
