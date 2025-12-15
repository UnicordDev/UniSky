using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UniSky.Background;
using UniSky.Helpers;
using UniSky.Models;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Windows.System.UserProfile;

namespace UniSky.Services;

internal class BackgroundNotificationsService(ILogger<BackgroundNotificationsService> logger, ITypedSettings settings, IProtocolService protocolService) : INotificationsService
{
    public const string BADGE_BACKGROUND_TASK_NAME = nameof(BadgeBackgroundTask);

    public async Task InitializeAsync()
    {
        await RegisterPushNotificationsAsync();
        await RegisterBadgeUpdateBackgroundTask();
    }

    private async Task<bool> RegisterPushNotificationsAsync()
    {
        try
        {
            var protocol = protocolService.Protocol;
            if (protocol == null || protocol.Session?.Did == null)
                return false;

            var notificationRegistration = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            var uri = notificationRegistration.Uri;
            var installId = settings.InstallId;

            var languages = new List<string>();
            if (settings.UseTwitterLocale)
                languages.Add("twitter");
            languages.AddRange(GlobalizationPreferences.Languages);

            var model = new RegistrationModel(protocol.Session.Did.ToString(), installId, uri, 0, [..languages]);
            var content = JsonSerializer.Serialize(model, JsonContext.Default.RegistrationModel);

            using var client = new HttpClient();
            using var message = new HttpRequestMessage(HttpMethod.Post, "https://wamwoowam.co.uk/unisky/push/register");
            message.Content = new StringContent(content, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(message);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to register push notifications!");
            return false;
        }
    }


    private async Task<bool> RegisterBadgeUpdateBackgroundTask()
    {
        try
        {
            if (BackgroundTaskRegistration.AllTasks.Values.Any(i => i.Name.Equals(BADGE_BACKGROUND_TASK_NAME)))
                return true;

            var status = await BackgroundExecutionManager.RequestAccessAsync();
#pragma warning disable CS0618 // Type or member is obsolete (why is this disabled)
            if (status is BackgroundAccessStatus.Denied or BackgroundAccessStatus.DeniedBySystemPolicy or BackgroundAccessStatus.DeniedByUser)
                return false;
#pragma warning restore CS0618 // Type or member is obsolete

            var builder = new BackgroundTaskBuilder()
            {
                Name = BADGE_BACKGROUND_TASK_NAME,
                TaskEntryPoint = typeof(BadgeBackgroundTask).FullName,
                IsNetworkRequested = true
            };

            builder.SetTrigger(new TimeTrigger(15, false));

            var registration = builder.Register();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to register {nameof(BadgeBackgroundTask)}");
            return false;
        }
    }
}
