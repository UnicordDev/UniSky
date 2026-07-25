using FishyFlip;
using UniSky.Notifications.Data;
using UniSky.Notifications.Models;
using UniSky.Notifications.Services.Providers;

namespace UniSky.Notifications.Services;

public interface IPushService
{
    Task<bool> PushNotificationAsync(ATProtocol at, NotificationEvent notificationEvent, INotificationProvider service, NotificationRegistration registration);
}