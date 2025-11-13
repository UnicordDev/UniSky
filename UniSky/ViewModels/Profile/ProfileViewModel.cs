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
using UniSky.Moderation;
using System.Collections.ObjectModel;
using UniSky.ViewModels.Moderation;

namespace UniSky.ViewModels.Profile;

public partial class ProfileViewModel : ViewModelBase
{
    private static readonly IdnMapping mapper = new IdnMapping();
    private static readonly ResourceLoader strings = ResourceLoader.GetForViewIndependentUse();

    private readonly IModerationService moderationService
        = ServiceContainer.Default.GetRequiredService<IModerationService>();

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
    private string pronouns;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMutual))]
    [NotifyPropertyChangedFor(nameof(FollowButtonText))]
    private bool isFollowing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMutual))]
    private bool followsYou;

    [ObservableProperty]
    private bool isMe;

    public ObservableCollection<LabelViewModel> Labels { get; }
    public ModerationDecision ModDecision { get; private set; }

    public bool IsMutual
        => IsFollowing && FollowsYou;
    public string FollowButtonText
        => strings.GetString(IsFollowing ? "Profile_Following" : "Profile_Follow");

    public ProfileViewModel()
    {
        this.id = null;
        this.AvatarUrl = "ms-appx:///Assets/Default/Avatar.png";
        this.Name = "Example User";
        this.Handle = "@example.com";
        this.Pronouns = "it/its";
        this.Labels = [];
    }

    public ProfileViewModel(ATObject obj)
    {
        this.source = obj;
        this.Labels = [];

        Populate(obj);
    }

    protected virtual void Populate(ATObject obj)
    {
        Labels.Clear();

        switch (obj)
        {
            case ProfileView view:
                SetData(view.Did,
                        view.Handle,
                        view.DisplayName,
                        view.Avatar,
                        view.Description,
                        view.Viewer,
                        pronouns: view.Pronouns);

                if (moderationService.ModerationOptions != null)
                    ModDecision = new Moderator(moderationService.ModerationOptions).ModerateProfile(view);
                break;
            case ProfileViewBasic profile:
                SetData(profile.Did,
                        profile.Handle,
                        profile.DisplayName,
                        profile.Avatar,
                        viewerState: profile.Viewer,
                        pronouns: profile.Pronouns);

                if (moderationService.ModerationOptions != null)
                    ModDecision = new Moderator(moderationService.ModerationOptions).ModerateProfile(profile);
                break;
            case ProfileViewDetailed detailed:
                SetData(detailed.Did,
                        detailed.Handle,
                        detailed.DisplayName,
                        detailed.Avatar,
                        detailed.Description,
                        detailed.Viewer,
                        detailed.Banner,
                        detailed.Pronouns);

                if (moderationService.ModerationOptions != null)
                    ModDecision = new Moderator(moderationService.ModerationOptions).ModerateProfile(detailed);
                break;
        }

        if (ModDecision != null)
        {
            DoModeration();
        }
    }

    private void DoModeration()
    {
        var ui = ModDecision.GetUI(ModerationContext.ProfileList);
        foreach (var cause in ui.Informs)
        {
            if (cause is LabelModerationCause label)
                Labels.Add(new LabelViewModel(label));
        }

        var avatar = ModDecision.GetUI(ModerationContext.Avatar);
        if (avatar.Blur || avatar.Alert)
            AvatarUrl = null;

        var banner = ModDecision.GetUI(ModerationContext.Banner);
        if (banner.Blur)
            BannerUrl = null;

        var displayName = ModDecision.GetUI(ModerationContext.DisplayName);
        if (displayName.Blur)
            this.Name = this.Handle;

        var blockCause = ModDecision.BlockCause;
        if (blockCause != null)
        {
            if (blockCause.Type is (ModerationCauseType.Blocking or ModerationCauseType.BlockOther))
            {
                this.Name = strings.GetString("Profile_Blocked");
                this.Bio = strings.GetString("Profile_BlockedUser");
                return;
            }

            if (blockCause.Type is (ModerationCauseType.BlockedBy))
            {
                this.Name = strings.GetString("Profile_Blocked");
                this.Bio = strings.GetString("Profile_BlockedByUser");
                return;
            }
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
                                string banner = "",
                                string pronouns = "")
    {
        var protocol = ServiceContainer.Default.GetRequiredService<IProtocolService>()
            .Protocol;

        this.id = id;

        this.IsMe = protocol.Session.Did.ToString() == id.ToString();
        this.Handle = ConvertHandle(handle);

        if (viewerState is ViewerState viewer)
        {
            this.IsFollowing = viewer.Following != null;
            this.FollowsYou = viewer.FollowedBy != null;
        }

        this.AvatarUrl = avatar;
        this.Name = string.IsNullOrWhiteSpace(displayName) ? ConvertHandle(handle, true) : displayName;
        this.Bio = bio?.Trim() ?? "";
        this.BannerUrl = banner;
        this.Pronouns = pronouns?.Trim() ?? "";
    }

    private static string ConvertHandle(ATHandle handle, bool forDisplayName = false)
    {
        if (string.IsNullOrWhiteSpace(handle.Handle) || handle.Handle == "handle.invalid")
            return strings.GetString("Profile_InvalidHandle");

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
