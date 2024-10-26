using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FishyFlip.Models;
using FishyFlip.Tools;
using Google.Protobuf.Collections;
using UniSky.Services;
using UniSky.ViewModels.Error;
using UniSky.ViewModels.Posts;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniSky.ViewModels.Feeds;

public class FeedItemCollection(FeedViewModel parent, ATUri uri, IProtocolService protocolService)
    : ObservableCollection<PostViewModel>, ISupportIncrementalLoading
{
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;

    private string cursor;

    public bool HasMoreItems { get; private set; } = true;

    public async Task RefreshAsync()
    {
        await semaphore.WaitAsync();

        try
        {
            this.cursor = null;
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
        var service = protocolService.Protocol;
        var viewModel = parent;
        viewModel.Error = null;

        count = Math.Max(count, 5);

        using var context = viewModel.GetLoadingContext();

        try
        {
            FeedViewPost[] posts;
            if (uri == null)
            {
                var list = (await service.Feed.GetTimelineAsync(count, this.cursor)
                    .ConfigureAwait(false))
                    .HandleResult();

                // BUGBUG: seems FishyFlip doesn't do this for me?
                this.cursor = HttpUtility.UrlEncode(list.Cursor);
                posts = list.Feed;
            }
            else
            {
                var list = (await service.Feed.GetFeedAsync(uri, count, this.cursor)
                    .ConfigureAwait(false))
                    .HandleResult();

                // BUGBUG: ^^
                this.cursor = HttpUtility.UrlEncode(list.Cursor);
                posts = list.Feed;
            }

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var item in posts)
                    this.Add(new PostViewModel(item));
            });

            if (posts.Length == 0)
                HasMoreItems = false;

            return new LoadMoreItemsResult() { Count = (uint)posts.Length };
        }
        catch (Exception ex)
        {
            viewModel.OnFeedLoadError(ex);
            HasMoreItems = false;
            return new LoadMoreItemsResult() { Count = 0 };
        }
    }
}
