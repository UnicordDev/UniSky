using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using FishyFlip.Models;
using UniSky.Extensions;
using UniSky.Helpers;
using UniSky.Services;
using UniSky.ViewModels.Feeds;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using UniSky.ViewModels.Error;
using CommunityToolkit.Mvvm.Input;
using UniSky.Controls.Compose;

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
        Feeds.Add(new FeedViewModel(null, null, protocolService));

        Task.Run(LoadAsync);
    }
    public ObservableCollection<FeedViewModel> Feeds { get; }

    [RelayCommand]
    public async Task Post()
    {
        var dialog = new ComposeDialog();
        await dialog.ShowAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var protocol = protocolService.Protocol;
            var prefs = (await protocol.Actor.GetPreferencesAsync()
                .ConfigureAwait(false))
                .HandleResult();

            var feeds = prefs.Preferences
            .OfType<SavedFeedsPref>()
                .FirstOrDefault();

            var generators = (await protocol.Feed.GetFeedGeneratorsAsync(feeds.Pinned)
                .ConfigureAwait(false))
                .HandleResult();

            syncContext.Post(() =>
            {
                foreach (var feed in generators.Feeds)
                {
                    Feeds.Add(new FeedViewModel(feed.Uri, feed, this.protocolService));
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
