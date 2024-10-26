using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Models;

namespace UniSky.ViewModels.Profiles;

public partial class ProfileViewModel : ViewModelBase
{
    [ObservableProperty]
    private string name;
    [ObservableProperty]
    private string handle;
    [ObservableProperty]
    private string avatarUrl;

    public ProfileViewModel(FeedProfile profile)
    {
        this.AvatarUrl = profile.Avatar;
        this.Name = profile.DisplayName;
        this.Handle = $"@{profile.Handle}";
    }

    public ProfileViewModel(ActorProfile profile)
    {
        this.AvatarUrl = profile.Avatar;
        this.Name = profile.DisplayName;
        this.Handle = $"@{profile.Handle}";
    }
}
