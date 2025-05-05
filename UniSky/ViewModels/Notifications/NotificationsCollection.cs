using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Notification;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Utilities.Collections;
using UniSky.Messages;
using UniSky.Moderation;
using UniSky.Services;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniSky.ViewModels.Notifications;

public class NotificationsCollection : ObservableCollection<NotificationViewModel>, ISupportIncrementalLoading
{
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;

    private readonly NotificationsPageViewModel parent;
    private readonly IProtocolService protocolService
        = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();
    private readonly IModerationService moderationService
        = ServiceContainer.Scoped.GetRequiredService<IModerationService>();
    private readonly ILogger<NotificationsCollection> logger
        = ServiceContainer.Scoped.GetRequiredService<ILogger<NotificationsCollection>>();

    private string cursor;
    private HashSet<string> contains;

    public NotificationsCollection(NotificationsPageViewModel parent)
    {
        this.parent = parent;
        this.contains = new HashSet<string>();
    }

    public bool HasMoreItems { get; private set; } = true;

    public async Task RefreshAsync()
    {
        var service = protocolService.Protocol;

        if (await semaphore.WaitAsync(500))
        {
            try
            {
                this.cursor = null;
                await InternalLoadMoreItemsAsync(Count);
            }
            finally
            {
                semaphore.Release();
            }
        }

        await service.Notification.UpdateSeenAsync(DateTime.Now);

        WeakReferenceMessenger.Default.Send<MarkAsReadNotification>();
    }


    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return Task.Run(async () =>
        {
            await semaphore.WaitAsync();

            try
            {
                return await InternalLoadMoreItemsAsync((int)count);
            }
            finally
            {
                semaphore.Release();
            }
        }).AsAsyncOperation();
    }

    private async Task<LoadMoreItemsResult> InternalLoadMoreItemsAsync(int count)
    {
        var service = protocolService.Protocol;
        var viewModel = parent;
        viewModel.Error = null;

        count = Math.Clamp(count, 5, 25);

        using var context = viewModel.GetLoadingContext();

        try
        {
            var moderator = new Moderator(moderationService.ModerationOptions);
            var notificationsResponse = (await service.ListNotificationsAsync(limit: count, cursor: cursor)
                .ConfigureAwait(false))
                .HandleResult();

            this.cursor = notificationsResponse.Cursor;

            var notifications = notificationsResponse.Notifications
                .Where(s => !moderator.ModerateNotification(s)
                                      .GetUI(ModerationContext.ContentList)
                                      .Filter)
                .Where(s => !contains.Contains(s.Cid))
                .ToList();

            foreach (var notif in notifications)
                contains.Add(notif.Cid);

            var hydratePostIds = notifications.Where(n =>
                n.Reason is (NotificationReason.Like or NotificationReason.Repost) &&
                n.ReasonSubject is not null)
                .Select(s => s.ReasonSubject)
                .Distinct();

            var posts = new Dictionary<string, PostView>();
            if (hydratePostIds.Any())
            {
                var p = (await service.GetPostsAsync(hydratePostIds.ToList())
                    .ConfigureAwait(false))
                    .HandleResult()
                    .Posts;

                foreach (var post in p)
                {
                    posts.TryAdd(post.Uri.ToString(), post);
                }
            }

            var initialCount = Count;

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var groups = notifications
                    .GroupBy(g => (g.Reason is (NotificationReason.Like or NotificationReason.Repost)) ? string.Join('-', g.Reason, g.ReasonSubject) : null);

                foreach (var group in groups)
                {
                    NotificationViewModel viewModel;
                    if (group.Key != null && (viewModel = this.FirstOrDefault(v => v.Subject != null && v.Key == group.Key)) != null)
                    {
                        viewModel.Add(group);
                        continue;
                    }

                    Notification notification = null;
                    PostView post = null;
                    if (group.Key == null)
                    {
                        foreach (var ungroupedNotification in group)
                        {
                            notification = ungroupedNotification;
                            if (notification.Reason is (NotificationReason.Like or NotificationReason.Repost))
                                _ = posts.TryGetValue(notification.ReasonSubject.ToString(), out post);

                            Add(new NotificationViewModel(notification, post));
                        }

                        continue;
                    }

                    notification = group.FirstOrDefault();
                    post = null;

                    if (notification.Reason is (NotificationReason.Like or NotificationReason.Repost))
                        _ = posts.TryGetValue(notification.ReasonSubject.ToString(), out post);

                    Add(new NotificationViewModel(group, post));
                }

                ArrayList.Adapter(this).Sort(); // ?????
            });

            if (notifications.Count == 0 || string.IsNullOrWhiteSpace(this.cursor))
                HasMoreItems = false;

            return new LoadMoreItemsResult() { Count = (uint)(Count - initialCount) };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Notifications");
            HasMoreItems = false;
            return new LoadMoreItemsResult() { Count = 0 };
        }
    }
}
