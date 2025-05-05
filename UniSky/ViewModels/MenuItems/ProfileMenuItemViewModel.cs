using System;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Extensions;
using UniSky.Pages;
using UniSky.Services;
using Windows.UI.Xaml.Media.Imaging;

namespace UniSky.ViewModels;

public partial class ProfileMenuItemViewModel : MenuItemViewModel
{
    private readonly IProtocolService protocolService
        = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();

    private ProfileViewDetailed profile;

    public ProfileMenuItemViewModel(HomeViewModel parent)
        : base(parent, HomePages.Profile, "\uE77B", typeof(ProfilePage))
    {

    }

    public override async Task LoadAsync()
    {
        var protocol = protocolService.Protocol;

        profile = (await protocol.GetProfileAsync(protocol.AuthSession.Session.Did)
             .ConfigureAwait(false))
             .HandleResult();

        Name = profile.DisplayName;
        if (profile.Avatar != null)
        {
            syncContext.Post(() =>
            {
                AvatarUrl = new BitmapImage(new Uri(profile.Avatar))
                {
                    DecodePixelWidth = 24,
                    DecodePixelHeight = 24,
                    DecodePixelType = DecodePixelType.Logical
                };
            });
        }
    }
}
