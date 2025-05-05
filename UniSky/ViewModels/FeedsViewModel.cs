using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Controls.Compose;
using UniSky.Extensions;
using UniSky.Services;
using UniSky.ViewModels.Feeds;

namespace UniSky.ViewModels;

public partial class FeedsViewModel : ViewModelBase
{
    private readonly IProtocolService protocolService;
    private readonly ILogger<FeedsViewModel> logger;

    public FeedsViewModel(
        IProtocolService protocolService,
        ILogger<FeedsViewModel> logger)
    {
        this.protocolService = protocolService;
        this.logger = logger;

        Feeds = [];

        Task.Run(LoadAsync);
    }

    public ObservableCollection<FeedViewModel> Feeds { get; }

    [RelayCommand]
    public async Task Post()
    {
        var sheetsService = ServiceContainer.Scoped.GetRequiredService<ISheetService>();
        await sheetsService.ShowAsync<ComposeSheet>();
    }

    private async Task LoadAsync()
    {
        try
        {
            var protocol = protocolService.Protocol;
            var prefs = (await protocol.GetPreferencesAsync()
                .ConfigureAwait(false))
                .HandleResult();

            var feeds = prefs.Preferences
                .OfType<SavedFeedsPrefV2>()
                .FirstOrDefault()?.Items
                .Where(p => p.Pinned == true);

            var generatedFeeds = feeds.Where(s => s.TypeValue == "feed")
                .Select(s => new ATUri(s.Value))
                .ToList();
            var generators = (await protocol.GetFeedGeneratorsAsync(generatedFeeds)
                .ConfigureAwait(false))
                .HandleResult();

            syncContext.Post(() =>
            {
                // TODO: this _all_ sucks
                foreach (var feed in feeds)
                {
                    if (feed.TypeValue == "feed")
                    {
                        var generatedFeed = generators.Feeds.FirstOrDefault(f => f.Uri.ToString() == feed.Value);
                        Feeds.Add(new FeedViewModel(FeedType.Custom, generatedFeed.Uri, generatedFeed, this.protocolService));
                    }

                    if (feed.TypeValue == "timeline")
                    {
                        if (feed.Value == "following")
                        {
                            Feeds.Add(new FeedViewModel(FeedType.Following, null, null, this.protocolService));
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch feeds!");
            this.SetErrored(ex);
        }
    }
}
