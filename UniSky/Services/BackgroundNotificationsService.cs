using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UniSky.Background;
using Windows.ApplicationModel.Background;

namespace UniSky.Services;

internal class BackgroundNotificationsService(ILogger<BackgroundNotificationsService> logger) : INotificationsService
{
    public const string BADGE_BACKGROUND_TASK_NAME = nameof(BadgeBackgroundTask);

    public async Task InitializeAsync()
    {
        await RegisterBadgeUpdateBackgroundTask();
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
