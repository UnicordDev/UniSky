namespace UniSky.Notifications.Services.Providers;

public static class NotificationProviders
{
    public static IServiceCollection AddNotificationProviders(this IServiceCollection services)
    {
        services.AddKeyedSingleton<INotificationProvider, LikeProvider>("app.bsky.feed.like:subject.uri");
        services.AddKeyedSingleton<INotificationProvider, ReplyProvider>("app.bsky.feed.post:reply.parent.uri");

        return services;
    }
}
