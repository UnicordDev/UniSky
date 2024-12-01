using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using FishyFlip.Tools;
using UniSky.Services;
using UniSky.ViewModels.Error;
using UniSky.ViewModels.Posts;
using UniSky.ViewModels.Profile;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniSky.ViewModels.Feeds;

public class FeedItemCollection : ObservableCollection<PostViewModel>, ISupportIncrementalLoading
{
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;
    private readonly FeedViewModel parent;
    private readonly FeedType type;
    private readonly ATUri uri;
    private readonly ATDid did;
    private readonly string filterType;
    private readonly IProtocolService protocolService;
    private readonly HashSet<string> ids = [];

    private string cursor;

    public FeedItemCollection(FeedViewModel parent, FeedType type, ATUri uri, IProtocolService protocolService)
    {
        this.parent = parent;
        this.type = type;
        this.uri = uri;
        this.protocolService = protocolService;
    }

    public FeedItemCollection(ProfileFeedViewModel parent, FeedType type, ATDid did, string filterType, IProtocolService protocolService)
    {
        this.parent = parent;
        this.type = type;
        this.did = did;
        this.filterType = filterType;
        this.protocolService = protocolService;
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
        var service = protocolService.Protocol;
        var viewModel = parent;
        viewModel.Error = null;

        count = Math.Clamp(count, 5, 100);

        using var context = viewModel.GetLoadingContext();

        try
        {
            List<FeedViewPost> posts;
            switch (type)
            {
                case FeedType.Following:
                    {
                        var list = (await service.GetTimelineAsync(limit: count, cursor: this.cursor)
                            .ConfigureAwait(false))
                            .HandleResult();

                        // BUGBUG: seems FishyFlip doesn't do this for me?
                        this.cursor = HttpUtility.UrlEncode(list.Cursor);
                        posts = list.Feed;
                        break;
                    }
                case FeedType.Custom:
                    {
                        var list = (await service.GetFeedAsync(uri, limit: count, cursor: this.cursor)
                            .ConfigureAwait(false))
                            .HandleResult();

                        // BUGBUG: ^^
                        this.cursor = HttpUtility.UrlEncode(list.Cursor);
                        posts = list.Feed;
                        break;
                    }
                case FeedType.Author:
                    {
                        var list = (await service.GetAuthorFeedAsync(did, filter: filterType, limit: count, cursor: this.cursor)
                            .ConfigureAwait(false))
                            .HandleResult();

                        // BUGBUG: ^^
                        this.cursor = HttpUtility.UrlEncode(list.Cursor);
                        posts = list.Feed;
                        break;
                    }
                default:
                    throw new InvalidOperationException();
            }

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var item in posts)
                {
                    if (item.Reply is { Parent: PostView } && item.Reason is not ReasonRepost)
                    {
                        var reply = item.Reply;
                        //var root = (PostView)reply.Root;
                        var parent = (PostView)reply.Parent;

                        if (!ids.Contains(parent.Cid))
                        {
                            Add(new PostViewModel(parent, true));
                            Add(new PostViewModel(item, true));

                            ids.Add(parent.Cid);
                        }
                    }
                    else
                    {
                        if (!ids.Contains(item.Post.Cid))
                            Add(new PostViewModel(item));
                    }
                }
            });

            if (posts.Count == 0 || string.IsNullOrWhiteSpace(this.cursor))
                HasMoreItems = false;

            return new LoadMoreItemsResult() { Count = (uint)posts.Count };
        }
        catch (Exception ex)
        {
            viewModel.OnFeedLoadError(ex);
            HasMoreItems = false;
            return new LoadMoreItemsResult() { Count = 0 };
        }
    }
}
