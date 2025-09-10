using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Bookmark;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Moderation;
using UniSky.Services;
using UniSky.ViewModels.Posts;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniSky.ViewModels.Bookmarks;

public class BookmarksCollection : ObservableCollection<PostViewModel>, ISupportIncrementalLoading
{
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;
    private readonly HashSet<string> ids = [];
    private readonly BookmarksPageViewModel parent;

    private readonly IProtocolService protocolService
        = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();
    private readonly IModerationService moderationService
        = ServiceContainer.Scoped.GetRequiredService<IModerationService>();

    private string cursor;

    public BookmarksCollection(BookmarksPageViewModel parent)
    {
        this.parent = parent;
    }

    public bool HasMoreItems { get; private set; } = true;

    public async Task RefreshAsync()
    {
        // already refreshing
        if (!await semaphore.WaitAsync(10)) return;

        try
        {
            this.cursor = null;
            this.ids.Clear();
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.Clear());
            await InternalLoadMoreItemsAsync(25);
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
            try
            {
                await semaphore.WaitAsync();
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
        try
        {
            var service = protocolService.Protocol;
            var viewModel = parent;
            viewModel.Error = null;

            count = Math.Clamp(count, 5, 100);

            using var context = viewModel.GetLoadingContext();

            try
            {
                var results = (await protocolService.Protocol.GetBookmarksAsync(limit: count, cursor: this.cursor)
                    .ConfigureAwait(false))
                    .HandleResult();

                this.cursor = results.Cursor;

                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var moderator = new Moderator(moderationService.ModerationOptions);
                    foreach (var bookmark in results.Bookmarks)
                    {
                        if (bookmark.Item is not PostView post) continue;
                        if (moderator.ModeratePost(post)
                                     .GetUI(ModerationContext.ContentList)
                                     .Filter)
                            continue;

                        if (ids.Contains(post.Cid)) continue;

                        Add(new PostViewModel(post));
                        ids.Add(post.Cid);
                    }
                });

                if (results.Bookmarks.Count == 0 || string.IsNullOrWhiteSpace(this.cursor))
                    HasMoreItems = false;

                return new LoadMoreItemsResult() { Count = (uint)results.Bookmarks.Count };
            }
            catch (Exception ex)
            {
                HasMoreItems = false;
                return new LoadMoreItemsResult() { Count = 0 };
            }
        }
        finally
        {
            this.parent.IsEmpty = Count == 0;
        }
    }
}
