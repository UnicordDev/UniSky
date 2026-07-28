using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Messages;
using UniSky.Services;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniSky.ViewModels.Notifications;

public class NotificationsCollection(NotificationsPageViewModel parent, NotificationFeedFilter filter = NotificationFeedFilter.All) : ObservableCollection<INotificationItem>, ISupportIncrementalLoading
{
    private const int PageSize = 30;
    private const int MaxAutoPageAttempts = 10;

    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;
    private readonly HashSet<string> seen = [];

    private readonly INotificationFeedService feedService
        = ServiceContainer.Scoped.GetRequiredService<INotificationFeedService>();
    private readonly ILogger<NotificationsCollection> logger
        = ServiceContainer.Scoped.GetRequiredService<ILogger<NotificationsCollection>>();

    private string cursor;
    private bool endOfFeed;
    private bool isFirstPage = true;
    private DateTime? pageSeenAt;

    public bool HasMoreItems => !endOfFeed;

    public async Task RefreshAsync()
    {
        if (!await semaphore.WaitAsync(10))
            return;

        try
        {
            cursor = null;
            endOfFeed = false;
            isFirstPage = true;
            pageSeenAt = null;
            seen.Clear();

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Clear());
            await InternalLoadMoreItemsAsync(PageSize);
        }
        finally
        {
            semaphore.Release();
        }
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
        var viewModel = parent;
        viewModel.ClearError();

        count = Math.Clamp(count, 5, 100); // listNotifications caps out at 100

        using var context = viewModel.GetLoadingContext();

        var added = 0;
        var attempts = 0;

        try
        {
            while (added == 0 && !endOfFeed && attempts++ < MaxAutoPageAttempts)
            {
                var page = await feedService
                    .FetchPageAsync(filter, cursor, count, pageSeenAt)
                    .ConfigureAwait(false);

                cursor = page.Cursor;
                
                if (string.IsNullOrWhiteSpace(cursor))
                    endOfFeed = true;

                if (isFirstPage)
                {
                    pageSeenAt = page.SeenAt;
                    isFirstPage = false;

                    if (filter == NotificationFeedFilter.All)
                        await MarkAllReadAsync(page).ConfigureAwait(false);
                }

                added += await MaterialiseAsync(page).ConfigureAwait(false);
            }

            viewModel.UpdateIsEmpty(Count == 0);

            return new LoadMoreItemsResult() { Count = (uint)added };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load notifications");
            viewModel.OnLoadError(ex);
            return new LoadMoreItemsResult() { Count = 0 };
        }
    }

    private async Task<int> MaterialiseAsync(NotificationFeedPage page)
    {
        var added = 0;

        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            foreach (var group in page.Items)
            {
                if (group.Key is null || !seen.Add(group.Key))
                    continue;

                try
                {
                    var item = NotificationItem.Create(
                        parent.Navigation, group, page, pageSeenAt,
                        allowUnreadHighlight: filter == NotificationFeedFilter.All);

                    if (item is null)
                        continue;

                    Add(item);
                    added++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Dropping notification {Key} ({Kind})", group.Key, group.Kind);
                }
            }
        });

        return added;
    }

    private async Task MarkAllReadAsync(NotificationFeedPage page)
    {
        try
        {
            var newest = page.Items.Count > 0 ? page.Items[0].IndexedAt : DateTime.MinValue;
            var now = DateTime.UtcNow;
            var syncedAt = newest > now ? newest : now;

            await feedService.MarkAllReadAsync(syncedAt).ConfigureAwait(false);

            WeakReferenceMessenger.Default.Send<MarkAsReadNotification>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark notifications as read");
        }
    }
}
