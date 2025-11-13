using System;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Services;
using Windows.ApplicationModel.Background;

namespace UniSky.Background;

public sealed class BadgeBackgroundTask : IBackgroundTask
{
    public BadgeBackgroundTask()
    {
    }

    public async void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();
        var logger = ServiceContainer.Default.GetRequiredService<ILogger<BadgeBackgroundTask>>();
        try
        {
            var badgeCount = 0;
            var badgeService = ServiceContainer.Default.GetRequiredService<IBadgeService>();
            var sessionService = ServiceContainer.Default.GetRequiredService<ISessionService>();
            var protocolService = ServiceContainer.Default.GetRequiredService<IProtocolService>();
            var atLogger = ServiceContainer.Default.GetRequiredService<ILogger<ATProtocol>>();

            foreach (var session in sessionService.EnumerateAllSessions())
            {
                try
                {
                    protocolService.SetProtocol(new ATProtocolBuilder()
                        .WithLogger(atLogger)
                        .EnableAutoRenewSession(true)
                        .WithUserAgent(Constants.UserAgent)
                        .Build());

                    await protocolService.RefreshSessionAsync(session);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to configure session for {Did}", session.DID);
                    continue;
                }

                try
                {
                    var protocol = protocolService.Protocol;
                    var notifications = (await protocol.GetUnreadCountAsync()
                        .ConfigureAwait(false))
                        .HandleResult();

                    badgeCount += (int)notifications.Count;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch badge count for {Did}", session.DID);
                    continue;
                }
            }

            badgeService.UpdateBadge(badgeCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to run {nameof(BadgeBackgroundTask)}");
        }
        finally
        {
            deferral.Complete();
        }
    }
}
