using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using UniSky.Services;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;

namespace UniSky.ViewModels.Feeds;

#nullable enable

public enum FeedType
{
    Following,
    Custom,
    Author
}

public partial class FeedViewModel : ViewModelBase
{
    private readonly FeedType type;
    private readonly ATUri? id;
    private readonly GeneratorView? generator;
    private readonly IProtocolService protocolService;

    [ObservableProperty]
    private string name = null!;
    [ObservableProperty]
    private FeedItemCollection items = null!;

    protected FeedViewModel(FeedType type, IProtocolService protocolService)
    {
        this.type = type;
        this.protocolService = protocolService;
    }

    public FeedViewModel(FeedType type, ATUri? id, GeneratorView? record, IProtocolService protocolService)
        : this(type, protocolService)
    {
        this.id = id;
        this.generator = record;

        this.Name = record?.DisplayName ?? ResourceLoader.GetForCurrentView().GetString("Feed_Following");
        this.Items = new FeedItemCollection(this, type, id);
    }

    public async Task RefreshAsync(Deferral? deferral = null)
    {
        this.Error = null!;
        await this.Items.RefreshAsync();
        deferral?.Complete();
    }

    internal void OnFeedLoadError(Exception ex)
    {
        base.SetErrored(ex);
    }
}
