using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Http.HttpResults;
using UniSky.Models;
using UniSky.Notifications.Data;
using UniSky.Notifications.Messages;

namespace UniSky.Notifications;

public static class NotificationsApi
{
    public static RouteGroupBuilder MapNotifications(this IEndpointRouteBuilder routes)
    {
        var notificationsApi = routes.MapGroup("/push");

        notificationsApi.MapPost("/register", Register);

        return notificationsApi;
    }

    public static async Task<Results<BadRequest<string>, Accepted>> Register(RegistrationModel reg, NotificationDbContext db)
    {
        if (!Uri.TryCreate(reg.ChannelUrl, UriKind.Absolute, out var channelUrl))
        {
            return TypedResults.BadRequest("Invalid channel url.");
        }

        // TODO: validate the host better than this
        if (!channelUrl.Host.EndsWith(".windows.com", StringComparison.InvariantCultureIgnoreCase))
        {
            return TypedResults.BadRequest("Invalid channel url.");
        }

        var dbRegistration = await db.Registrations.FindAsync(reg.Did, reg.InstallId);
        if (dbRegistration == null)
        {
            dbRegistration = new NotificationRegistration()
            {
                Did = reg.Did,
                InstallId = reg.InstallId,
                ChannelUrl = reg.ChannelUrl,
                Options = reg.Options,
            };

            await db.Registrations.AddAsync(dbRegistration);
        }
        else 
        {
            dbRegistration.ChannelUrl = reg.ChannelUrl;
            dbRegistration.Options = reg.Options;

            db.Registrations.Update(dbRegistration);
        }

        await db.SaveChangesAsync();

        await Task.WhenAll(WeakReferenceMessenger.Default.Send(new RegistrationsUpdatedMessage()));

        return TypedResults.Accepted((string?)null);
    }
}
