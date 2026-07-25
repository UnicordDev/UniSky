using System.Net;
using System.Text;
using System.Web;
using FishyFlip;
using UniSky.Notifications.Data;
using UniSky.Notifications.Models;
using UniSky.Notifications.Services.Providers;

namespace UniSky.Notifications.Services;

public class PushServiceWinPhone7(
    ILogger<PushServiceWinPhone7> logger,
    IHttpClientFactory httpClientFactory) : IPushService
{
    private readonly Encoding UTF8NoBOM = new UTF8Encoding(false);
    private const string TOAST_TEMPLATE
        = "<?xml version=\"1.0\" encoding=\"utf-8\"?><wp:Notification xmlns:wp=\"WPNotification\"><wp:Toast><wp:Text1>{0}</wp:Text1><wp:Text2>{1}</wp:Text2></wp:Toast></wp:Notification>";

    public async Task<bool> PushNotificationAsync(ATProtocol at, NotificationEvent notificationEvent, INotificationProvider service, NotificationRegistration registration)
    {
        var notification = new ClassicNotificationBuilder();
        if (!await service.PopulateClassicNotification(at, notificationEvent with { Registration = registration }, notification))
            return true;

        if (!await SendNotificationAsync(notification, registration))
            return false;

        return true;
    }

    private async Task<bool> SendNotificationAsync(ClassicNotificationBuilder notification, NotificationRegistration registration)
    {
        var notificationXml = string.Format(
            TOAST_TEMPLATE,
            HttpUtility.HtmlEncode(notification.Title),
            HttpUtility.HtmlEncode(notification.Content));

        using var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, registration.ChannelUrl);
        request.Headers.Add("X-WindowsPhone-Target", "toast");
        request.Headers.Add("X-NotificationClass", "2");
        request.Content = new StringContent(notificationXml, UTF8NoBOM, "text/xml");

        try
        {
            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return true;

            logger.LogWarning("Failed to post notification! {StatusCode}", response.StatusCode);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Gone:
                case HttpStatusCode.NotFound:
                    return false;
                case HttpStatusCode.NotAcceptable:
                    break; // TODO: backoff
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push notification to client!");
        }

        return true;
    }
}