using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Moderation;
using UniSky.Services;
using UniSky.ViewModels.Profile;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniSky.ViewModels.Search;

public class SearchProfileCollection : ObservableCollection<ProfileViewModel>, ISupportIncrementalLoading
{
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;
    private readonly HashSet<string> ids = [];
    private readonly SearchFeedViewModel parent;

    private readonly IProtocolService protocolService
        = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();
    private readonly IModerationService moderationService
        = ServiceContainer.Scoped.GetRequiredService<IModerationService>();

    private readonly string query;
    private string cursor;

    public SearchProfileCollection(SearchFeedViewModel parent, string query)
    {
        this.parent = parent;
        this.query = query;
    }

    public bool HasMoreItems { get; private set; } = true;

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
            var results = (await protocolService.Protocol.SearchActorsAsync(query, count, cursor)
                .ConfigureAwait(false))
                .HandleResult();
            this.cursor = results.Cursor;

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var moderator = new Moderator(moderationService.ModerationOptions);
                foreach (var item in results.Actors)
                {
                    if (moderator.ModerateProfile(item)
                                 .GetUI(ModerationContext.ProfileList)
                                 .Filter)
                        continue;

                    if (!ids.Contains(item.Did.ToString()))
                        Add(new ProfileViewModel(item));
                }
            });

            if (results.Actors.Count == 0 || string.IsNullOrWhiteSpace(this.cursor))
                HasMoreItems = false;

            return new LoadMoreItemsResult() { Count = (uint)results.Actors.Count };
        }
        catch (Exception ex)
        {
            HasMoreItems = false;
            return new LoadMoreItemsResult() { Count = 0 };
        }
    }
}
