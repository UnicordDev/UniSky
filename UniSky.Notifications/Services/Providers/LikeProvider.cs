using CommunityToolkit.WinUI.Notifications;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using Microsoft.Extensions.Caching.Memory;
using UniSky.Models;
using UniSky.Notifications.Models;

namespace UniSky.Notifications.Services.Providers;

public class LikeProvider(IMemoryCache cache) : INotificationProvider
{
    public async Task<bool> PopulateNotification(
        ATProtocol at,
        NotificationEvent notification,
        ToastContentBuilder builder)
    {
        if (notification.Registration.Options.HasFlag(NotificationOptions.ExcludeLikes))
            return false;

        var postUri = notification.SubjectRecordUri!;
        var likerUri = notification.SourceRecordUri!;

        var collectionId = postUri.ToString();

        //var actor = GetProfileAsync(at, notification.SourceDid);
        var actor = await cache.GetOrCreateAsync(likerUri!.ToString(),
            Helpers.CacheForTime(TimeSpan.FromHours(1), async (entry) =>
                (await at.GetProfileAsync(notification.SourceDid)).HandleResult()));

        if (actor == null)
            return false;

        var post = await cache.GetOrCreateAsync(postUri!.ToString(),
            Helpers.CacheForTime(TimeSpan.FromHours(1), async (entry) =>
                (Post?)(await at.GetPostAsync(postUri!.Did!, postUri.Rkey)).HandleResult()?.Value));

        if (post == null)
            return false;

        var displayName = !string.IsNullOrWhiteSpace(actor!.DisplayName) ? actor.DisplayName : $"@{actor.Handle}";

        builder
            .AddText($"{displayName} liked your post", AdaptiveTextStyle.Title, hintMaxLines: 1)
            .AddText(post.Text, AdaptiveTextStyle.Body);

        if (actor.Avatar != null)
            builder.AddAppLogoOverride(new Uri(actor.Avatar), ToastGenericAppLogoCrop.Circle);

        return true;
    }
}
