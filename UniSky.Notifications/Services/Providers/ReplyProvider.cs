using CommunityToolkit.WinUI.Notifications;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using Microsoft.Extensions.Caching.Memory;
using UniSky.Models;
using UniSky.Notifications.Models;

namespace UniSky.Notifications.Services.Providers;

public class ReplyProvider(IMemoryCache cache) : INotificationProvider
{
    public async Task<bool> PopulateNotification(
        ATProtocol at,
        NotificationEvent notification,
        ToastContentBuilder builder)
    {
        if (notification.Registration.Options.HasFlag(NotificationOptions.ExcludeReplies))
            return false;

        var postUri = notification.SourceRecordUri!;
        var post = await cache.GetOrCreateAsync(postUri!.ToString() + "PostView",
            Helpers.CacheForTime(TimeSpan.FromHours(1), async (entry) =>
            (await at.GetPostsAsync([postUri])).HandleResult()?.Posts[0]));

        if (post == null || post.Record is not Post postRecord)
            return false;

        var actor = post.Author;
        var displayName = !string.IsNullOrWhiteSpace(actor!.DisplayName) ? actor.DisplayName : $"@{actor.Handle}";

        builder
            .AddText($"{displayName} replied to you", AdaptiveTextStyle.Title, hintMaxLines: 1)
            .AddText(postRecord.Text, AdaptiveTextStyle.Body);

        switch (post.Embed)
        {
            case ViewImages images:
                {
                    var image = images.Images.FirstOrDefault();
                    if (image == null || !Uri.TryCreate(image.Thumb, UriKind.Absolute, out var uri))
                        break;

                    builder.AddHeroImage(uri, image.Alt);
                    break;
                }
            case ViewVideo video:
                {
                    if (!Uri.TryCreate(video.Thumbnail, UriKind.Absolute, out var uri))
                        break;

                    builder.AddHeroImage(uri, video.Thumbnail);
                    break;
                }
        }

        if (actor.Avatar != null)
            builder.AddAppLogoOverride(new Uri(actor.Avatar), ToastGenericAppLogoCrop.Circle);

        return true;
    }
}
