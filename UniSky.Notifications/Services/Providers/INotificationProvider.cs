using CommunityToolkit.WinUI.Notifications;
using FishyFlip;
using UniSky.Notifications.Models;

namespace UniSky.Notifications.Services.Providers;

public interface INotificationProvider
{
    Task<bool> PopulateNotification(
        ATProtocol at,
        NotificationEvent notification,
        ToastContentBuilder builder);
}
