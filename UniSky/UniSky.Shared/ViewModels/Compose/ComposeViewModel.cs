using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using UniSky.Services;

namespace UniSky.ViewModels.Compose;

public partial class ComposeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _text;
    [ObservableProperty]
    private string _avatarUrl;

    private IProtocolService protocolService;
    private ILogger<ComposeViewModel> logger;

    public ComposeViewModel(
        IProtocolService protocolService,
        ILogger<ComposeViewModel> logger)
    {
        this.protocolService = protocolService;
        this.logger = logger;

        Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        using var loading = this.GetLoadingContext();

        var protocol = protocolService.Protocol;

        try
        {
            var profile = (await protocol.Actor.GetProfileAsync(protocol.Session.Did)
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
