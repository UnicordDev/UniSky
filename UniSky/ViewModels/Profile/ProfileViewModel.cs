using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Models;
using UniSky.Pages;
using UniSky.Services;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.Resources;
using System.Globalization;
using System.Threading.Tasks;
using FishyFlip.Lexicon.App.Bsky.Graph;
using System;

namespace UniSky.ViewModels.Profile;

public partial class ProfileViewModel : ViewModelBase
{
    private static readonly IdnMapping mapper = new IdnMapping();
    private static readonly ResourceLoader strings = ResourceLoader.GetForViewIndependentUse();

    protected ATIdentifier id;
    protected ATObject source;

    [ObservableProperty]
    private string name;
    [ObservableProperty]
    private string handle;
    [ObservableProperty]
    private string avatarUrl;
    [ObservableProperty]
    private string bannerUrl;
    [ObservableProperty]
    private string bio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMutual))]
    [NotifyPropertyChangedFor(nameof(FollowButtonText))]
    private bool isFollowing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMutual))]
    private bool followsYou;

    [ObservableProperty]
    private bool isMe;

    public bool IsMutual
        => IsFollowing && FollowsYou;
    public string FollowButtonText
        => strings.GetString(IsFollowing ? "ProfileFollowing" : "ProfileFollow");

    public ProfileViewModel()
    {
        this.id = null;
        this.AvatarUrl = "ms-appx:///Assets/Default/Avatar.png";
        this.Name = "Example User";
        this.Handle = "@example.com";
    }

    public ProfileViewModel(ATObject obj)
    {
        this.source = obj;

        Populate(obj);
    }

    protected virtual void Populate(ATObject obj)
    {
        switch (obj)
        {
            case ProfileView view:
                SetData(view.Did,
                        view.Handle,
                        view.DisplayName,
                        view.Avatar,
                        view.Description,
                        view.Viewer);
                break;
            case ProfileViewBasic profile:
                SetData(profile.Did,
                        profile.Handle,
                        profile.DisplayName,
                        profile.Avatar,
                        viewerState: profile.Viewer);
                break;
            case ProfileViewDetailed detailed:
                SetData(detailed.Did,
                        detailed.Handle,
                        detailed.DisplayName,
                        detailed.Avatar,
                        detailed.Description,
                        detailed.Viewer,
                        detailed.Banner);
                break;
        }
    }

    [RelayCommand]
    private void OpenProfile(UIElement element)
    {
        var service = ServiceContainer.Scoped.GetRequiredService<INavigationServiceLocator>()
            .GetNavigationService("Home");

        service.Navigate<ProfilePage>(this.source, new ContinuumNavigationTransitionInfo() { ExitElement = element });
    }

    [RelayCommand]
    private async Task FollowAsync()
    {
        if (IsFollowing || this.id is not ATDid did)
            return;

        var protocol = ServiceContainer.Default.GetRequiredService<IProtocolService>()
            .Protocol;

        var follow = await protocol.CreateFollowAsync(new Follow(did, DateTime.UtcNow))
            .ConfigureAwait(false);

        follow.Switch(
            output => IsFollowing = true,
            SetErrored);
    }

    public virtual void SetData(ATDid id,
                                ATHandle handle,
                                string displayName,
                                string avatar,
                                string bio = "",
                                ViewerState viewerState = null,
                                string banner = "")
    {
        var protocol = ServiceContainer.Default.GetRequiredService<IProtocolService>()
            .Protocol;

        this.id = id;

        this.IsMe = protocol.Session.Did.ToString() == id.ToString();
        this.Handle = ConvertHandle(handle);

        if (viewerState is ViewerState viewer)
        {
            if (viewer.Blocking != null)
            {
                this.AvatarUrl = null;
                this.Name = strings.GetString("ProfileBlocked");
                this.Bio = strings.GetString("ProfileUserBlocked");
                return;
            }

            if (viewer.BlockedBy == true)
            {
                this.AvatarUrl = null;
                this.Name = strings.GetString("ProfileBlocked");
                this.Bio = strings.GetString("ProfileYouAreBlocked");
                return;
            }

            this.IsFollowing = viewer.Following != null;
            this.FollowsYou = viewer.FollowedBy != null;
        }

        this.AvatarUrl = avatar;
        this.Name = string.IsNullOrWhiteSpace(displayName) ? ConvertHandle(handle, true) : displayName;
        this.Bio = bio?.Trim() ?? "";
        this.BannerUrl = banner;
    }

    private static string ConvertHandle(ATHandle handle, bool forDisplayName = false)
    {
        if (string.IsNullOrWhiteSpace(handle.Handle) || handle.Handle == "handle.invalid")
            return strings.GetString("ProfileInvalidHandle");

        return forDisplayName ? ConvertHandleString(handle) : $"@{ConvertHandleString(handle)}";
    }

    private static string ConvertHandleString(ATHandle handle)
    {
        try
        {
            return mapper.GetUnicode(handle.Handle);
        }
        catch
        {
            return handle.Handle;
        }
    }
}
