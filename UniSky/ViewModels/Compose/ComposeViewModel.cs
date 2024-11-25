using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using UniSky.Extensions;
using UniSky.Services;
using UniSky.ViewModels.Error;

namespace UniSky.ViewModels.Compose;

public partial class ComposeViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPost))]
    private string _text;
    [ObservableProperty]
    private string _avatarUrl;

    private readonly IProtocolService protocolService;
    private readonly ILogger<ComposeViewModel> logger;

    // TODO: this but better
    public bool IsDirty
        => !string.IsNullOrEmpty(Text);

    // TODO: ditto
    public bool CanPost
        => !string.IsNullOrEmpty(Text);

    public ComposeViewModel(
        IProtocolService protocolService,
        ILogger<ComposeViewModel> logger)
    {
        this.protocolService = protocolService;
        this.logger = logger;

        Task.Run(LoadAsync);
    }

    [RelayCommand]
    private async Task Post()
    {
        Error = null;
        using var ctx = this.GetLoadingContext();

        try
        {
            var text = Text;

            var post = (await protocolService.Protocol.CreatePostAsync(new Post(text))
                .ConfigureAwait(false))
                .HandleResult();

            Text = null;
            syncContext.Post(async () => { await Hide(); });
        }
        catch (Exception ex)
        {
            syncContext.Post(() => Error = new ExceptionViewModel(ex));
        }
    }

    [RelayCommand]
    private async Task Hide()
    {
        var sheetService = Ioc.Default.GetRequiredService<ISheetService>();
        await sheetService.TryCloseAsync();
    }

    private async Task LoadAsync()
    {
        using var loading = this.GetLoadingContext();

        var protocol = protocolService.Protocol;

        try
        {
            var profile = (await protocol.GetProfileAsync(protocol.AuthSession.Session.Did)
                .ConfigureAwait(false))
                .HandleResult();

            AvatarUrl = profile.Avatar;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch user info!");
            this.SetErrored(ex);
        }
    }
}
