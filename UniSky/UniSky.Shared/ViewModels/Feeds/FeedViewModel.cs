using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Models;
using UniSky.Helpers;
using UniSky.Services;
using Windows.Foundation;

namespace UniSky.ViewModels.Feeds;

#nullable enable

public partial class FeedViewModel : ViewModelBase
{
    private readonly ATUri? uri;
    private readonly FeedRecord? generator;
    private readonly IProtocolService protocolService;

    [ObservableProperty]
    private string name = null!;
    [ObservableProperty]
    private FeedItemCollection items = null!;

    public FeedViewModel(ATUri? uri, FeedRecord? record, IProtocolService protocolService)
    {
        this.uri = uri;
        this.generator = record;
        this.protocolService = protocolService;

        this.Name = record?.DisplayName ?? "Following";
        this.Items = new FeedItemCollection(this, uri, protocolService);
    }

    public async Task RefreshAsync(Deferral? deferral = null)
    {
        await this.Items.RefreshAsync();
        deferral?.Complete();
    }

    internal void OnFeedLoadError(Exception ex)
    {
        base.SetErrored(ex);
    }
}
