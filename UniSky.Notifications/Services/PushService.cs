
using System.Net;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Notifications;
using FishyFlip;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using UniSky.Notifications.Data;
using UniSky.Notifications.Messages;
using UniSky.Notifications.Models;
using UniSky.Notifications.Models.WNS;
using UniSky.Notifications.Services.Providers;

namespace UniSky.Notifications.Services;

public class PushService(
    ILogger<PushService> logger,
    ILogger<ATProtocol> protocolLogger,
    IServiceProvider services) : IHostedService, IRecipient<NotificationEventMessage>
{
    private readonly ATProtocol at = new ATProtocolBuilder()
        .WithLogger(protocolLogger)
        .EnableBlueskyModerationService()
        .Build();

    public async Task OnNotificationEvent(NotificationEvent notificationEvent)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            logger.LogInformation("Got event {Event}", notificationEvent.SourceType);
            var service = services.GetKeyedService<INotificationProvider>(notificationEvent.SourceType);
            if (service == null)
                return;

            var subject = notificationEvent.SubjectDid.ToString();
            var registrations = await db.Registrations.Where(r => r.Did == subject)
                .ToListAsync();

            logger.LogInformation("Got {N} registrations for DID {DID}", registrations.Count, notificationEvent.SubjectDid);

            var failed = new List<NotificationRegistration>();
            for (int i = 0; i < registrations.Count; i++)
            {
                var registration = registrations[i];

                logger.LogInformation("Pushing to {Url}, {Version}", registration.ChannelUrl, registration.PlatformVersion);

                var pusher = services.GetKeyedService<IPushService>("v" + (registration.PlatformVersion ?? "10.0"));
                var succeeded = pusher != null && await pusher.PushNotificationAsync(at, notificationEvent, service, registration);
                if (!succeeded)
                    failed.Add(registration);
            }

            foreach(var reg in failed)
                db.Remove(reg);

            await db.SaveChangesAsync();

            if (failed.Count > 0)
                await Task.WhenAll(WeakReferenceMessenger.Default.Send(new RegistrationsUpdatedMessage()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in event handler!");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Unregister<NotificationEventMessage>(this);

        return Task.CompletedTask;
    }

    public void Receive(NotificationEventMessage message)
    {
        message.Reply(OnNotificationEvent(message.Event));
    }
}
