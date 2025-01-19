using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Moderation;
using UniSky.Services;
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
    private readonly HashSet<string> ids = [];

    private readonly IProtocolService protocolService = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();
    private readonly IModerationService moderationService = ServiceContainer.Scoped.GetRequiredService<IModerationService>();
    private readonly ILogger<FeedItemCollection> logger = ServiceContainer.Scoped.GetRequiredService<ILogger<FeedItemCollection>>();

    private string cursor;

    public FeedItemCollection(FeedViewModel parent, FeedType type, ATUri uri)
    {
        this.parent = parent;
        this.type = type;
        this.uri = uri;
    }

    public FeedItemCollection(ProfileFeedViewModel parent, FeedType type, ATDid did, string filterType)
    {
        this.parent = parent;
        this.type = type;
        this.did = did;
        this.filterType = filterType;
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
    public async Task LoadMoreAsync()
    {
        // already refreshing
        if (!await semaphore.WaitAsync(10)) return;

        try
        {
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
                    var vm = new PostViewModel(item);
                    var ui = vm.Moderation.GetUI(ModerationContext.ContentList);
                    if (ui.Filter)
                    {
                        logger.LogDebug("Filtering post {Cid} with cause {Cause}", item.Post.Cid, ui.Filters[0]);
                        continue;
                    }

                    if (item.Reply is { Parent: PostView } && item.Reason is not ReasonRepost)
                    {
                        var reply = item.Reply;

                        var moderation = new Moderator(moderationService.ModerationOptions);
                        if (reply.Root is PostView root)
                        {
                            var rootMod = moderation.ModeratePost(root);
                            if (rootMod.GetUI(ModerationContext.ContentList).Filter)
                                continue;
                        }

                        if (reply.Parent is PostView parent)
                        {
                            var parentMod = moderation.ModeratePost(parent);
                            if (parentMod.GetUI(ModerationContext.ContentList).Filter)
                                continue;

                            if (!ids.Contains(parent.Cid))
                            {
                                Add(new PostViewModel(parent, true));

                                vm.HasParent = true;
                                Add(vm);

                                ids.Add(parent.Cid);
                            }
                        }
                    }
                    else
                    {
                        if (!ids.Contains(item.Post.Cid))
                            Add(vm);
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
